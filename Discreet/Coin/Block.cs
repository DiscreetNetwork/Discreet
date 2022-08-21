using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;
using Discreet.Network.Peerbloom;
using System.IO;
using System.Linq;

namespace Discreet.Coin
{
    /**
     * This block class is intended for use in testnet.
     * Blocks will, in the future, also specify position in the DAG.
     * Global indices will be deterministically generated for the sake of DAG consistency.
     * The current consensus among developers for amount_output_index and tx_index is generation during the addition of the head block in the DAG.
     * All transactions in blocks in a round of consensus are added in order of timestamp, ensuring all blocks are processed.
     * For a blockchain this simplifies to a single block addition.
     */

    public class Block: ICoin
    {
        public BlockHeader Header;

        public FullTransaction[] Transactions;

        public byte[] Serialize()
        {
            using MemoryStream _ms = new MemoryStream();

            Serialize(_ms);

            return _ms.ToArray();
        }

        public void Serialize(Stream s)
        {
            Header.Serialize(s);

            for (int i = 0; i < Transactions.Length; i++)
            {
                Transactions[i].Serialize(s);
            }
        }

        public void Serialize(byte[] bytes, uint offset)
        {
            Array.Copy(Serialize(), 0, bytes, offset, Size());
        }

        public string Readable()
        {
            return Discreet.Readable.Block.ToReadable(this);
        }

        public object ToReadable()
        {
            return new Discreet.Readable.Block(this);
        }

        public static Block FromReadable(string json)
        {
            return Discreet.Readable.Block.FromReadable(json);
        }

        public void Deserialize(byte[] bytes)
        {
            Deserialize(bytes, 0);
        }

        public uint Deserialize(byte[] bytes, uint offset)
        {
            Header = new BlockHeader();
            offset = Header.Deserialize(bytes, offset);

            Transactions = new FullTransaction[Header.NumTXs];
            for (int i = 0; i < Transactions.Length; i++)
            {
                Transactions[i] = new FullTransaction();
                offset = Transactions[i].Deserialize(bytes, offset);
            }

            return offset;
        }

        public void Deserialize(Stream s)
        {
            Header = new BlockHeader();
            Header.Deserialize(s);

            Transactions = new FullTransaction[Header.NumTXs];
            for (int i = 0; i < Transactions.Length; i++)
            {
                Transactions[i] = new FullTransaction();
                Transactions[i].Deserialize(s);
            }
        }

        public uint Size()
        {
            uint size = Header.Size();

            for (int i = 0; i < Transactions.Length; i++)
            {
                size += Transactions[i].Size();
            }

            return size;
        }

        /* for testing purposes only */
        public static Block BuildRandom(StealthAddress[] addresses, int[] numOutputs)
        {
            List<FullTransaction> txs = new();

            for (int i = 0; i < addresses.Length; i++)
            {
                for (int j = 0; j < numOutputs[i] / 16; j++)
                {
                    txs.Add(Transaction.GenerateRandomNoSpend(addresses[i], 16).ToFull());
                }

                if (numOutputs[i] % 16 != 0)
                {
                    txs.Add(Transaction.GenerateRandomNoSpend(addresses[i], numOutputs[i] % 16).ToFull());
                }
            }

            return Build(txs, null, default);
        }

        public static Block BuildRandomPlus(StealthAddress[] addresses, int[] numOutputs, List<FullTransaction> txExtras)
        {
            List<FullTransaction> txs = txExtras;

            for (int i = 0; i < addresses.Length; i++)
            {
                for (int j = 0; j < numOutputs[i] / 16; j++)
                {
                    txs.Add(Transaction.GenerateRandomNoSpend(addresses[i], 16).ToFull());
                }

                if (numOutputs[i] % 16 != 0)
                {
                    txs.Add(Transaction.GenerateRandomNoSpend(addresses[i], numOutputs[i] % 16).ToFull());
                }
            }

            return Build(txs, null, default);
        }

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
                block.Header.BlockSize += txs[i].Size();
            }

            DB.DataView dataView = DB.DataView.GetView();

            block.Header.Height = dataView.GetChainHeight() + 1;

            if (block.Header.Height > 0)
            {
                block.Header.PreviousBlock = dataView.GetBlockHeader(block.Header.Height - 1).BlockHash;
            }
            else
            {
                block.Header.PreviousBlock = new SHA256(new byte[32], false);
            }

            if (block.Header.Fee > 0 && miner != null)
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

                /* the mask is always the identity (scalar identity is zero) for miner tx */
                Key mask = Key.Z;
                KeyOps.GenCommitment(ref minerOutput.Commitment, ref mask, block.Header.Fee);

                minerOutput.UXKey = KeyOps.DKSAP(ref r, miner.view, miner.spend, 0);
                minerOutput.Amount = block.Header.Fee;

                minertx.Outputs = new TXOutput[1] { minerOutput };

                minertx.TransactionKey = R;

                txs.Insert(0, minertx.ToFull());

                block.Header.BlockSize += minertx.Size();
            }

            if (signingKey != default)
            {
                if (block.Header.Fee > 0 && miner != null)
                {
                    block.Header.Version = 2;
                }
                else
                {
                    block.Header.Version = 1;
                }

                block.Header.ExtraLen = 96;
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

                Key mask = KeyOps.GenCommitmentMask(ref r, ref addresses[i].view, 0);
                KeyOps.GenCommitment(ref tx.Outputs[0].Commitment, ref mask, values[i]);
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

        public VerifyException Verify()
        {
            /* Verify does the following:
             *   - ensures Height is equal to db.GetChainHeight() + 1
             *   - ensures MerkleRoot is proper
             *   - ensures PreviousBlock is the result of db.GetBlock(block.Height - 1).BlockHash
             *   - ensures BlockHash is proper
             *   - ensures BlockSize, NumOutputs, NumTXs, and Fee are proper
             *   - verifies all transactions in the block
             *   
             * Verify() should be used for full blocks only. Validating previous blocks 
             * is not needed, as blocks are always processed in order.
             */

            DB.DataView dataView = DB.DataView.GetView();

            if (Header.Version != 1 && Header.Version != 2)
            {
                return new VerifyException("Block", $"Unsupported version (blocks are either version 1 or 2); got version {Header.Version}");
            }

            if (Header.Height != dataView.GetChainHeight() + 1)
            {
                return new VerifyException("Block", $"Block is not next in sequence (expected {dataView.GetChainHeight() + 1}, but got {Header.Height})");
            }

            if (Transactions == null || Transactions.Length == 0)
            {
                return new VerifyException("Block", $"Block does not contain full transaction information (malformed)");
            }

            if (Header.NumTXs != Transactions.Length)
            {
                return new VerifyException("Block", $"Transaction count mismatch: expected {Header.NumTXs}, but got {Transactions.Length})");
            }

            SHA256 merkleRoot = GetMerkleRoot();

            if(!merkleRoot.Equals(Header.MerkleRoot))
            {
                return new VerifyException("Block", $"Merkle root mismatch: expected {Header.MerkleRoot.ToHexShort()}, but got {merkleRoot.ToHexShort()}");
            }

            if (Header.Height == 0)
            {
                if (!Header.PreviousBlock.Equals(new SHA256(new byte[32], false)))
                {
                    return new VerifyException("Block", $"genesis block should point to zero hash, but got {Header.PreviousBlock.ToHexShort()}");
                }
            }
            else
            {
                SHA256 prevBlockHash = dataView.GetBlockHeader(Header.Height - 1).BlockHash;

                if (!prevBlockHash.Equals(Header.PreviousBlock))
                {
                    return new VerifyException("Block", $"previous block mismatch: expected {prevBlockHash.ToHexShort()} (previous block in database), but got {Header.PreviousBlock.ToHexShort()}");
                }
            }

            if (Header.ExtraLen != (Header.Extra?.Length ?? 0))
            {
                return new VerifyException("Block", $"Block extra mismatch: expected length {Header.ExtraLen}, but got {Header.Extra?.Length ?? 0}");
            }

            SHA256 blockHash = Hash();

            if (!blockHash.Equals(Header.BlockHash))
            {
                return new VerifyException("Block", $"block hash mismatch: expected {Header.BlockHash.ToHexShort()}, but got {blockHash.ToHexShort()}");
            }

            ulong fee = 0;
            uint numOutputs = 0;
            uint blockSize = 137 + (uint)(Header.Extra?.Length ?? 0);

            for (int i = 0; i < Transactions.Length; i++)
            {
                fee += Transactions[i].Fee;
                numOutputs += Transactions[i].NumPOutputs;
                blockSize += Transactions[i].Size();
            }

            if (fee != Header.Fee)
            {
                return new VerifyException("Block", $"block fee mismatch: expected {Header.Fee} as included in block, but got {fee} from calculations");
            }

            if (numOutputs != Header.NumOutputs)
            {
                return new VerifyException("Block", $"block output count mismatch: expected {Header.NumOutputs} as included in block, but got {numOutputs} from calculations");
            }

            if (blockSize != Header.BlockSize)
            {
                return new VerifyException("Block", $"block size (in bytes) mismatch: expected {Header.BlockSize} as included in block, but got {blockSize} from calculations");
            }

            /* verify coinbase */
            if (Header.Version == 2)
            {
                var _coinbase = Transactions[0];

                if (_coinbase == null)
                {
                    return new VerifyException("Block", "No coinbase transaction detected");
                }

                if (_coinbase.Version != 0)
                {
                    return new VerifyException("Block", "Miner tx not present or invalid");
                }

                var coinbase = _coinbase.ToPrivate();

                if (coinbase.Outputs == null || coinbase.Outputs.Length != 1)
                {
                    return new VerifyException("Block", "Miner tx has invalid outputs");
                }

                var minerexc = coinbase.Verify();

                if (minerexc != null)
                {
                    return minerexc;
                }

                /* now verify output amount matches commitment */
                Key feeComm = new(new byte[32]);
                Key _I = Key.Copy(Key.I);
                KeyOps.GenCommitment(ref feeComm, ref _I, Header.Fee);

                if (!feeComm.Equals(coinbase.Outputs[0].Commitment))
                {
                    return new VerifyException("Block", "Coinbase transaction in block does not balance with fee commitment!");
                }
            }

            for (int i = Header.Version == 1 ? 0 : 1; i < Transactions.Length; i++)
            {
                if (Header.Height > 0 && Transactions[i].Version == 0)
                {
                    return new VerifyException("Block", "block contains coinbase transaction outside of miner tx");
                }

                var txexc = Transactions[i].Verify(inBlock: true);

                if (txexc != null)
                {
                    return txexc;
                }
            }

            if ((Header.Version == 1 || Header.Version == 2) && !CheckSignature())
            {
                return new VerifyException("Block", "block signature is invalid and/or does not come from a masternode!");
            }

            return null;
        }

        public bool CheckSignature()
        {
            if (Header.Extra == null || Header.Extra.Length != 96) return true;

            var sig = new Signature(Header.Extra);
            return sig.Verify(Header.BlockHash) && IsMasternode(sig.y);
        }

        public static bool IsMasternode(Key k)
        {
            //TODO: Implement hardcoded masternode IDs
            return k == Key.FromHex(Constants.TEMPORARY_MASTERNODE_PUBLIC_KEY);
            //return true;
        }
    }
}
