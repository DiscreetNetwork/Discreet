using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;
using System.IO;
using System.Linq;
using Discreet.Common.Exceptions;
using Discreet.Common.Serialize;
using System.Text.Json;
using Discreet.Daemon;
using Discreet.DB;

namespace Discreet.Coin.Models
{
    /**
     * This block class is intended for use in testnet.
     * Blocks will, in the future, also specify position in the DAG.
     * Global indices will be deterministically generated for the sake of DAG consistency.
     * The current consensus among developers for amount_output_index and tx_index is generation during the addition of the head block in the DAG.
     * All transactions in blocks in a round of consensus are added in order of timestamp, ensuring all blocks are processed.
     * For a blockchain this simplifies to a single block addition.
     */

    public class Block : IHashable
    {
        public BlockHeader Header { get; set; }

        public FullTransaction[] Transactions { get; set; }

        public void Serialize(BEBinaryWriter writer)
        {
            Header.Serialize(writer);
            writer.WriteSerializableArray(Transactions, false);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Header = reader.ReadSerializable<BlockHeader>();
            Transactions = reader.ReadSerializableArray<FullTransaction>((int)Header.NumTXs);
        }

        public string Readable()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new Converters.BlockConverter());

            return JsonSerializer.Serialize(this, typeof(Block), options);
        }

        public uint GetSize()
        {
            int size = Header.Size;

            for (int i = 0; i < Transactions.Length; i++)
            {
                size += Transactions[i].Size;
            }

            return (uint)size;
        }

        public int Size => Header.Size + Transactions.Select(x => x.Size).Aggregate(0, (x, y) => x + y);


        public static Block Build(List<FullTransaction> txs, StealthAddress miner, Key signingKey)
        {
            Block block = new Block
            {
                Header = new BlockHeader
                {
                    Timestamp = (ulong)DateTime.UtcNow.Ticks,
                    NumTXs = (uint)txs.Count,
                    Version = 0,
                    Fee = 0,
                    NumOutputs = 0,
                    BlockSize = 137
                },
            };

            for (int i = 0; i < txs.Count; i++)
            {
                block.Header.Fee += txs[i].Fee;
                block.Header.NumOutputs += txs[i].NumPOutputs;
                block.Header.BlockSize += txs[i].GetSize();
            }

            // because of block buffer, we need to use that instead
            IView dataView = BlockBuffer.Instance;
            
            block.Header.Height = dataView.GetChainHeight() + 1;

            if (block.Header.Height > 0)
            {
                block.Header.PreviousBlock = dataView.GetBlockHeader(block.Header.Height - 1).BlockHash;
            }
            else
            {
                block.Header.PreviousBlock = new SHA256(new byte[32], false);
            }

            if ((block.Header.Fee > 0 || GetEmissions(block.Header.Height) > 0) && miner != null)
            {
                /* Construct miner TX */
                Transaction minertx = new();
                minertx.NumInputs = 0;
                minertx.NumOutputs = 1;
                minertx.NumSigs = 0;

                Key R = new(new byte[32]);
                Key r = new(new byte[32]);

                KeyOps.GenerateKeypair(ref r, ref R);

                TXOutput minerOutput = new();
                minerOutput.Commitment = new Key(new byte[32]);

                /* the mask is always 1 for miner tx */
                Key mask = Key.I;
                var minerComm = minerOutput.Commitment;
                KeyOps.GenCommitment(ref minerComm, ref mask, block.Header.Fee + GetEmissions(block.Header.Height));
                minerOutput.Commitment = minerComm;

                minerOutput.UXKey = KeyOps.DKSAP(ref r, miner.view, miner.spend, 0);
                minerOutput.Amount = block.Header.Fee + GetEmissions(block.Header.Height);

                minertx.Outputs = new TXOutput[1] { minerOutput };

                minertx.TransactionKey = R;

                txs.Insert(0, minertx.ToFull());

                block.Header.BlockSize += minertx.GetSize();

                block.Header.NumTXs += 1;
                block.Header.NumOutputs += 1;
            }

            if (signingKey != default)
            {
                if ((block.Header.Fee > 0 || GetEmissions(block.Header.Height) > 0) && miner != null)
                {
                    block.Header.Version = 2;
                }
                else
                {
                    block.Header.Version = 1;
                }

                //block.Header.ExtraLen = 96;
                block.Header.BlockSize += 96;
            }

            block.Header.MerkleRoot = GetMerkleRoot(txs);

            /* Block hash is just the header hash, i.e. Hash(Version, Timestamp, Height, BlockSize, NumTXs, NumOutputs, PreviousBlock, MerkleRoot) */
            block.Header.BlockHash = block.Hash();

            if (signingKey != default)
            {
                block.Header.Extra = KeyOps.Sign(ref signingKey, block.Header.BlockHash).ToBytes();
            }

            block.Transactions = txs.ToArray();

            return block;
        }

        public static Block BuildGenesis(StealthAddress[] addresses, ulong[] values, int numDummy, Key signingKey)
        {
            List<FullTransaction> txs = new();

            for (int i = 0; i < numDummy / 16; i++)
            {
                txs.Add(Transaction.GenerateRandomNoSpend(new StealthAddress(KeyOps.GeneratePubkey(), KeyOps.GeneratePubkey()), 16).ToFull());
            }

            if (numDummy % 16 != 0)
            {
                txs.Add(Transaction.GenerateRandomNoSpend(new StealthAddress(KeyOps.GeneratePubkey(), KeyOps.GeneratePubkey()), numDummy % 16).ToFull());
            }

            for (int i = 0; i < addresses.Length; i++)
            {
                Transaction tx = new()
                {
                    Version = 0,
                    NumInputs = 0,
                    NumOutputs = 1,
                    NumSigs = 0,
                };

                Key r = new(new byte[32]);
                Key R = new(new byte[32]);

                KeyOps.GenerateKeypair(ref r, ref R);

                tx.Outputs = new TXOutput[1];


                tx.Outputs[0] = new TXOutput
                {
                    Commitment = new Key(new byte[32])
                };

                var comm = tx.Outputs[0].Commitment;
                Key mask = KeyOps.GenCommitmentMask(ref r, ref addresses[i].view, 0);
                KeyOps.GenCommitment(ref comm, ref mask, values[i]);
                tx.Outputs[0].Commitment = comm;
                tx.Outputs[0].UXKey = KeyOps.DKSAP(ref r, addresses[i].view, addresses[i].spend, 0);
                tx.Outputs[0].Amount = KeyOps.GenAmountMask(ref r, ref addresses[i].view, 0, values[i]);


                tx.TransactionKey = R;

                txs.Add(tx.ToFull());
            }

            return Build(txs, null, signingKey);
        }

        public static SHA256 GetMerkleRoot(List<FullTransaction> txs)
        {
            List<SHA256> hashes = new();

            for (int k = 0; k < txs.Count; k++)
            {
                hashes.Add(txs[k].Hash());
            }

            while (hashes.Count > 1)
            {
                hashes = GetMerkleRoot(hashes);
            }

            return hashes[0];
        }

        public SHA256 GetMerkleRoot()
        {

            var _transactions = new SHA256[Transactions.Length];

            for (int k = 0; k < Transactions.Length; k++)
            {
                _transactions[k] = Transactions[k].Hash();
            }

            List<SHA256> hashes = new(_transactions);

            while (hashes.Count > 1)
            {
                hashes = GetMerkleRoot(hashes);
            }

            return hashes[0];
        }

        private static List<SHA256> GetMerkleRoot(List<SHA256> hashes)
        {
            List<SHA256> newHashes = new();

            for (int i = 0; i < hashes.Count / 2; i++)
            {
                byte[] data = new byte[64];
                Array.Copy(hashes[2 * i].Bytes, data, 32);
                Array.Copy(hashes[2 * i + 1].Bytes, 0, data, 32, 32);
                newHashes.Add(SHA256.HashData(data));
            }

            if (hashes.Count % 2 != 0)
            {
                newHashes.Add(SHA256.HashData(hashes[^1].Bytes));
            }

            return newHashes;
        }

        public SHA256 Hash()
        {
            return Header.Hash();
        }

        public bool CheckSignature()
        {
            if (Header.Extra == null || Header.Extra.Length != 96) return false;

            var sig = new Signature(Header.Extra);
            return sig.Verify(Header.BlockHash) && IsBlockAuthority(sig.y);
        }

        public static bool IsBlockAuthority(Key k)
        {
            return Daemon.BlockAuth.DefaultBlockAuth.Instance.Keyring.Keys.Any(x => k == x);
            //return true;
        }

        public static ulong GetEmissions(long height)
        {
            if (height > 200_000_000L)
            {
                return 0_15000_00000UL;
            }
            else if (height > 160_000_000L)
            {
                return 0_25000_00000UL;
            }
            else if (height > 140_000_000L)
            {
                return 0_40000_00000UL;
            }
            else if (height > 120_000_000L)
            {
                return 0_60000_00000UL;
            }
            else if (height > 100_000_000L)
            {
                return 0_80000_00000UL;
            }
            else if (height > 70_000_000L)
            {
                return 1_00000_00000UL;
            }
            else if (height > 50_000_000L)
            {
                return 1_20000_00000UL;
            }
            else if (height > 45_000_000L)
            {
                return 1_40000_00000UL;
            }
            else if (height > 37_000_000L)
            {
                return 1_50000_00000UL;
            }
            else if (height > 27_000_000L)
            {
                return 1_60000_00000UL;
            }
            else if (height > 18_000_000L)
            {
                return 2_00000_00000UL;
            }
            else if (height > 9_000_000L)
            {
                return 3_00000_00000UL;
            }
            else if (height > 0)
            {
                return 5_00000_00000UL;
            }
            else
            {
                return 0UL;
            }
        }
    }
}
