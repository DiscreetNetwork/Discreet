using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;
using System.IO;
using System.Linq;

/// <summary>
/// Discreet.Coin defines the components of the Discreet blockchain.
/// </summary>
namespace Discreet.Coin
{
    /// <summary>
    /// This block class is intended for use in testnet. Blocks will, in the
    /// future, also specify position in the DAG. Global indices will be
    /// deterministically generated for the sake of DAG consistency. The current
    /// consensus among developers for amount_output_index and tx_index is
    /// generation during the addition of the head block in the DAG. All
    /// transactions in blocks in a round of consensus are added in order of
    /// timestamp, ensuring all blocks are processed. For a blockchain this
    /// simplifies to a single block addition.
    /// </summary>
    public class Block: ICoin
    {
        public BlockHeader Header;

        public FullTransaction[] Transactions;

        /// <summary>
        /// Serialize returns an array that contains all the data in a Block
        /// transformed to bytes.
        /// </summary>
        /// <returns>The byte array containing the Block data.</returns>
        public byte[] Serialize()
        {
            using MemoryStream _ms = new MemoryStream();

            Serialize(_ms);

            return _ms.ToArray();
        }

        /// <summary>
        /// Serialize populates a byte stream that contains all the data in a Block
        /// transformed to bytes.
        /// </summary>
        /// <param name="s">A byte stream.</param>
        public void Serialize(Stream s)
        {
            Header.Serialize(s);

            for (int i = 0; i < Transactions.Length; i++)
            {
                Transactions[i].Serialize(s);
            }
        }

        /// <summary>
        /// Serialize copies this Block's serialized representation to a given
        /// byte array, starting at an offset.
        /// </summary>
        /// <param name="bytes">A byte array that will hold a copy of the block serialization.</param>
        /// <param name="offset">The offset at which we will copy the block serialization.</param>
        public void Serialize(byte[] bytes, uint offset)
        {
            Array.Copy(Serialize(), 0, bytes, offset, Size());
        }

        /// <summary>
        /// Readable encodes this block to a string containing a JSON.
        /// </summary>
        public string Readable()
        {
            return Discreet.Readable.Block.ToReadable(this);
        }

        /// <summary>
        /// ToReadable converts the block to an object containing the block
        /// header and transactions in JSON format.
        /// </summary>
        /// <returns>A readable block.</returns>
        public object ToReadable()
        {
            return new Discreet.Readable.Block(this);
        }

        /// <summary>
        /// FromReadable returns a Block that is created from a stringified
        /// readable block.
        /// </summary>
        /// <returns>A Block.</returns>
        public static Block FromReadable(string json)
        {
            return Discreet.Readable.Block.FromReadable(json);
        }

        /// <summary>
        /// Deserialize initializes the block instance from the deserialization
        /// of a byte array contaning the data of a block.
        /// </summary>
        /// <param name="bytes">A byte array that will hold a copy of the block
        /// serialization.</param>
        public void Deserialize(byte[] bytes)
        {
            Deserialize(bytes, 0);
        }

        /// <summary>
        /// Deserialize initializes the block instance from the deserialization
        /// of a byte array, starting from an offset, which contains the data of
        /// a block.
        /// </summary>
        /// <param name="bytes">A byte array that will hold a copy of the block
        /// serialization.</param>
        /// <param name="offset">An offset that works as the starting index for
        /// the byte array to start the deserialization.</param>
        /// <returns>An offset equal to the original offset, plus the length of
        /// the deserialized block.</returns>
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

        /// <summary>
        /// Deserialize initializes the block instance from the deserialization
        /// of bytes contained in a stream, which represent the data of a block.
        /// </summary>
        /// <param name="s">A byte stream.</param>
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

        /// <summary>
        /// Size calculates the size of the block, which considers the block's
        /// header and its transactions sizes.
        /// </summary>
        /// <returns>The size of the block.</returns>
        public uint Size()
        {
            uint size = Header.Size();

            for (int i = 0; i < Transactions.Length; i++)
            {
                size += Transactions[i].Size();
            }

            return size;
        }

        /// <summary>
        /// BuildRandom creates a block with randomly created transactions. It's
        /// intended for testing purposes only.
        /// </summary>
        /// <param name="addresses">An array of stealth addresses to use for creating the transactions.</param>
        /// <param name="numOutputs">How many random transactions to add per stealth address.</param>
        /// <returns>A block with random transactions.</returns>
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

        /// <summary>
        /// BuildRandomPlus creates a block with randomly created transactions,
        /// plus some already created transactions. It's intended for testing
        /// purposes only.
        /// </summary>
        /// <param name="addresses">An array of stealth addresses to use for
        /// creating the transactions.</param>
        /// <param name="numOutputs">How many random transactions to add per
        /// stealth address.</param>
        /// <param name="txExtras">A list of initial transactions for the
        /// block.</param>
        /// <returns>A block with random transactions.</returns>
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

        /// <summary>
        /// Build mints a new block, given a list of transactions, a miner and a
        /// signing key.
        /// </summary>
        /// <param name="txs">A list of transactions to be added to the block.</param>
        /// <param name="miner">The address responsible for minting the new block.</param>
        /// <param name="signingKey">A key used for signing the new block.</param>
        /// <returns>A new block.</returns>
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

            if ((block.Header.Fee > 0 || Config.STANDARD_BLOCK_REWARD > 0) && miner != null)
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
                KeyOps.GenCommitment(ref minerOutput.Commitment, ref mask, block.Header.Fee + Config.STANDARD_BLOCK_REWARD);

                minerOutput.UXKey = KeyOps.DKSAP(ref r, miner.view, miner.spend, 0);
                minerOutput.Amount = block.Header.Fee + Config.STANDARD_BLOCK_REWARD;

                minertx.Outputs = new TXOutput[1] { minerOutput };

                minertx.TransactionKey = R;

                txs.Insert(0, minertx.ToFull());

                block.Header.BlockSize += minertx.Size();

                block.Header.NumTXs += 1;
                block.Header.NumOutputs += 1;
            }

            if (signingKey != default)
            {
                if ((block.Header.Fee > 0 || Config.STANDARD_BLOCK_REWARD > 0) && miner != null)
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

        /// <summary>
        /// BuildGenesis mints the genesis block of the blockchain.
        /// </summary>
        /// <param name="addresses">An array of stealth addresses that will hold the initial coins.</param>
        /// <param name="values">The amount of coins to set for each address.</param>
        /// <param name="numDummy">Number of dummy outputs created for obfuscating real outputs.</param>
        /// <param name="signingKey">The genesis block signing key.</param>
        /// <returns>The genesis block.</returns>
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

        /// <summary>
        /// GetMerkleRoot constructs a Merkle root using a list of transactions
        /// for the leafs of the Merkle tree.
        /// </summary>
        /// <param name="txs">A list of transactions to be hashed to construct
        /// the Merkle tree.</param>
        /// <returns>The Merkle root of the transactions given.</returns>
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

        /// <summary>
        /// GetMerkleRoot constructs a Merkle root using the block's
        /// transactions for the leafs of the Merkle tree.
        /// </summary>
        /// <returns>The Merkle root of the block's transactions.</returns>
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

        /// <summary>
        /// GetMerkleRoot constructs a Merkle root using a list of hashes as the
        /// leafs of the Merkle tree.
        /// </summary>
        /// <returns>The Merkle root of the given hashes.</returns>
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

        /// <summary>
        /// Hash hashes the block's header.
        /// </summary>
        /// <returns>The hash of the block's header.</returns>
        public SHA256 Hash()
        {
            return Header.Hash();
        }

        /// <summary>
        /// Verify does the following:
        /// - ensures Height is equal to db.GetChainHeight() + 1
        /// - ensures MerkleRoot is proper
        /// - ensures PreviousBlock is the result of db.GetBlock(block.Height - 1).BlockHash
        /// - ensures BlockHash is proper
        /// - ensures BlockSize, NumOutputs, NumTXs, and Fee are proper
        /// - verifies all transactions in the block
        ///
        /// Verify() should be used for full blocks only. Validating previous blocks
        /// is not needed, as blocks are always processed in order.
        /// </summary>
        /// <returns>An exception, in case of an error, null otherwise.</returns>
        public VerifyException Verify()
        {
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

        /// <summary>
        /// CheckSignature checks that the block signature is valid and that
        /// it's coming from a master node.
        /// </summary>
        /// <returns>True if the block signature is valid, false otherwise.</returns>
        public bool CheckSignature()
        {
            if (Header.Extra == null || Header.Extra.Length != 96) return false;

            var sig = new Signature(Header.Extra);
            return sig.Verify(Header.BlockHash) && IsMasternode(sig.y);
        }

        /// <summary>
        /// IsMasternode checks if the provided signing key belongs to the
        /// masternode or not.
        /// </summary>
        /// <param name="k">The signing key to check.</param>
        /// <returns>True if the signing key is from the master node, false
        /// otherwise.</returns>
        public static bool IsMasternode(Key k)
        {
            //TODO: Implement hardcoded masternode IDs
            return k == Key.FromHex("806d68717bcdffa66ba465f906c2896aaefc14756e67381f1b9d9772d03fd97d");
            //return true;
        }
    }
}
