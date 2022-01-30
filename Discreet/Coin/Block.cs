using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;

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
        public byte Version;

        public ulong Timestamp;
        public long Height;
        public ulong Fee;      // 25

        public Cipher.SHA256 PreviousBlock;
        public Cipher.SHA256 BlockHash;          //  89

        public Cipher.SHA256 MerkleRoot;        // 121

        public uint NumTXs;
        public uint BlockSize;              // 129
        public uint NumOutputs;             // 133

        public Transaction Coinbase;

        public Cipher.SHA256[] Transactions; // 32 * len + 133

        /* used for full blocks (not packed with blocks) */
        public FullTransaction[] transactions;

        public virtual byte[] Marshal()
        {
            byte[] bytes = new byte[Size()];

            bytes[0] = Version;

            Serialization.CopyData(bytes, 1, Timestamp);
            Serialization.CopyData(bytes, 9, Height);
            Serialization.CopyData(bytes, 17, Fee);

            Array.Copy(PreviousBlock.Bytes, 0, bytes, 25, 32);
            Array.Copy(BlockHash.Bytes, 0, bytes, 57, 32);
            Array.Copy(MerkleRoot.Bytes, 0, bytes, 89, 32);

            Serialization.CopyData(bytes, 121, NumTXs);
            Serialization.CopyData(bytes, 125, BlockSize);
            Serialization.CopyData(bytes, 129, NumOutputs);

            byte[] coinbase = Coinbase.Marshal();
            Array.Copy(coinbase, 0, bytes, 133, coinbase.Length);

            for (int i = 0; i < NumTXs; i++)
            {
                Array.Copy(Transactions[i].Bytes, 0, bytes, 133 + coinbase.Length + i * 32, 32);
            }

            return bytes;
        }

        public virtual byte[] MarshalFull()
        {
            byte[] bytes = new byte[SizeFull()];

            bytes[0] = Version;

            Serialization.CopyData(bytes, 1, Timestamp);
            Serialization.CopyData(bytes, 9, Height);
            Serialization.CopyData(bytes, 17, Fee);

            Array.Copy(PreviousBlock.Bytes, 0, bytes, 25, 32);
            Array.Copy(BlockHash.Bytes, 0, bytes, 57, 32);
            Array.Copy(MerkleRoot.Bytes, 0, bytes, 89, 32);

            Serialization.CopyData(bytes, 121, NumTXs);
            Serialization.CopyData(bytes, 125, BlockSize);
            Serialization.CopyData(bytes, 129, NumOutputs);

            uint offset = 133;
            byte[] coinbase = Coinbase.Marshal();
            Array.Copy(coinbase, 0, bytes, 133, coinbase.Length);
            offset += (uint)coinbase.Length;

            for (int i = 0; i < NumTXs; i++)
            {
                transactions[i].Marshal(bytes, offset);
                offset += transactions[i].Size();
            }

            return bytes;
        }

        public virtual void MarshalFull(byte[] bytes, uint offset)
        {
            Array.Copy(MarshalFull(), 0, bytes, offset, SizeFull());
        }

        public virtual void Marshal(byte[] bytes, uint offset)
        {
            Array.Copy(Marshal(), 0, bytes, offset, Size());
        }

        public virtual string Readable()
        {
            return Discreet.Readable.Block.ToReadable(this);
        }

        public virtual string ReadableFull()
        {
            return Readable();
        }

        public static Block FromReadable(string json)
        {
            return Discreet.Readable.Block.FromReadable(json);
        }

        public virtual void Unmarshal(byte[] bytes)
        {
            Version = bytes[0];

            Timestamp = Serialization.GetUInt64(bytes, 1);
            Height = Serialization.GetInt64(bytes, 9);
            Fee = Serialization.GetUInt64(bytes, 17);

            PreviousBlock = new SHA256(bytes[25..57], false);
            BlockHash = new SHA256(bytes[57..89], false);
            MerkleRoot = new SHA256(bytes[89..121], false);

            NumTXs = Serialization.GetUInt32(bytes, 121);
            BlockSize = Serialization.GetUInt32(bytes, 125);
            NumOutputs = Serialization.GetUInt32(bytes, 129);

            Transactions = new Cipher.SHA256[NumTXs];

            Coinbase = new Transaction();
            uint offset = Coinbase.Unmarshal(bytes, 133);

            for (int i = 0; i < NumTXs; i++)
            {
                byte[] data = new byte[32];
                Array.Copy(bytes, offset, data, 0, 32);
                offset += 32;
                Transactions[i] = new SHA256(data, false);
            }
        }

        public virtual void UnmarshalFull(byte[] bytes)
        {
            Version = bytes[0];

            Timestamp = Serialization.GetUInt64(bytes, 1);
            Height = Serialization.GetInt64(bytes, 9);
            Fee = Serialization.GetUInt64(bytes, 17);

            PreviousBlock = new SHA256(bytes[25..57], false);
            BlockHash = new SHA256(bytes[57..89], false);
            MerkleRoot = new SHA256(bytes[89..121], false);

            NumTXs = Serialization.GetUInt32(bytes, 121);
            BlockSize = Serialization.GetUInt32(bytes, 125);
            NumOutputs = Serialization.GetUInt32(bytes, 129);

            Transactions = new Cipher.SHA256[NumTXs];

            Coinbase = new Transaction();
            uint _offset = Coinbase.Unmarshal(bytes, 133);

            transactions = new FullTransaction[NumTXs];

            for (int i = 0; i < NumTXs; i++)
            {
                transactions[i] = new FullTransaction();
                transactions[i].Unmarshal(bytes, _offset);
                _offset += transactions[i].Size();
                Transactions[i] = transactions[i].Hash();
            }
        }

        public virtual uint Unmarshal(byte[] bytes, uint _offset)
        {
            int offset = (int)_offset;
            Version = bytes[offset];

            Timestamp = Serialization.GetUInt64(bytes, _offset + 1);
            Height = Serialization.GetInt64(bytes, _offset + 9);
            Fee = Serialization.GetUInt64(bytes, _offset + 17);

            PreviousBlock = new SHA256(bytes[(offset + 25)..(offset + 57)], false);
            BlockHash = new SHA256(bytes[(offset + 57)..(offset + 89)], false);
            MerkleRoot = new SHA256(bytes[(offset + 89)..(offset + 121)], false);

            NumTXs = Serialization.GetUInt32(bytes, _offset + 121);
            BlockSize = Serialization.GetUInt32(bytes, _offset + 125);
            NumOutputs = Serialization.GetUInt32(bytes, _offset + 129);

            Transactions = new Cipher.SHA256[NumTXs];

            Coinbase = new Transaction();
            _offset = Coinbase.Unmarshal(bytes, _offset + 133);

            for (int i = 0; i < NumTXs; i++)
            {
                byte[] data = new byte[32];
                Array.Copy(bytes, _offset, data, 0, 32);
                _offset += 32;
                Transactions[i] = new SHA256(data, false);
            }

            return _offset + Size();
        }

        public virtual uint UnmarshalFull(byte[] bytes, uint _offset)
        {
            int offset = (int)_offset;
            Version = bytes[offset];

            Timestamp = Serialization.GetUInt64(bytes, _offset + 1);
            Height = Serialization.GetInt64(bytes, _offset + 9);
            Fee = Serialization.GetUInt64(bytes, _offset + 17);

            PreviousBlock = new SHA256(bytes[(offset + 25)..(offset + 57)], false);
            BlockHash = new SHA256(bytes[(offset + 57)..(offset + 89)], false);
            MerkleRoot = new SHA256(bytes[(offset + 89)..(offset + 121)], false);

            NumTXs = Serialization.GetUInt32(bytes, _offset + 121);
            BlockSize = Serialization.GetUInt32(bytes, _offset + 125);
            NumOutputs = Serialization.GetUInt32(bytes, _offset + 129);

            Transactions = new Cipher.SHA256[NumTXs];
            transactions = new FullTransaction[NumTXs];

            Coinbase = new Transaction();
            _offset = Coinbase.Unmarshal(bytes, _offset + 133);

            for (int i = 0; i < NumTXs; i++)
            {
                transactions[i] = new FullTransaction();
                transactions[i].Unmarshal(bytes, _offset);
                _offset += transactions[i].Size();
                Transactions[i] = transactions[i].Hash();
            }

            return _offset + SizeFull();
        }

        public virtual uint Size()
        {
            return 133 + (Coinbase != null ? Coinbase.Size() : 0) + 32 * (uint)Transactions.Length;
        }

        public virtual uint SizeFull()
        {
            uint size = 133 + (Coinbase != null ? Coinbase.Size() : 0);

            for (int i = 0; i < transactions.Length; i++)
            {
                size += transactions[i].Size();
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

            return Build(txs, null);
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

            return Build(txs, null);
        }

        public static Block Build(List<FullTransaction> txs, StealthAddress miner)
        {
            Block block = new();
            block.Timestamp = (ulong)DateTime.Now.Ticks;
            block.NumTXs = (uint)txs.Count;
            block.Version = 0;

            block.Fee = 0;
            block.NumOutputs = 0;
            block.BlockSize = 133;

            for (int i = 0; i < txs.Count; i++)
            {
                block.Fee += txs[i].Fee;
                block.NumOutputs += txs[i].NumOutputs;
                block.BlockSize += txs[i].Size();
            }

            DB.DB db = DB.DB.GetDB();

            block.Height = db.GetChainHeight() + 1;

            if (block.Height > 0)
            {
                block.PreviousBlock = db.GetBlock(block.Height - 1).BlockHash;
            }
            else
            {
                block.PreviousBlock = new SHA256(new byte[32], false);
            }

            if (block.Fee > 0 && miner != null)
            {
                /* Construct miner TX */
                Transaction minertx = new();
                minertx.Version = 0;
                minertx.NumInputs = 0;
                minertx.NumOutputs = 1;
                minertx.NumSigs = 0;

                Key R = new(new byte[32]);
                Key r = new(new byte[32]);

                KeyOps.GenerateKeypair(ref r, ref R);

                TXOutput minerOutput = new();
                minerOutput.Commitment = new Key(new byte[32]);

                /* the mask is always the identity for miner tx */
                Key mask = Key.I;
                KeyOps.GenCommitment(ref minerOutput.Commitment, ref mask, block.Fee);

                minerOutput.UXKey = KeyOps.DKSAP(ref r, miner.view, miner.spend, 0);
                minerOutput.Amount = block.Fee;

                minertx.Outputs = new TXOutput[1] { minerOutput };

                minertx.TransactionKey = R;

                block.Coinbase = minertx;
            }
            else
            {
                /* Construct miner TX */
                Transaction minertx = new();
                minertx.Version = 0;
                minertx.NumInputs = 0;
                minertx.NumOutputs = 1;
                minertx.NumSigs = 0;

                Key R = new(new byte[32]);
                Key r = new(new byte[32]);

                KeyOps.GenerateKeypair(ref r, ref R);

                TXOutput minerOutput = new();
                minerOutput.Commitment = new Key(new byte[32]);

                /* the mask is always the identity for miner tx */
                Key mask = Key.I;
                KeyOps.GenCommitment(ref minerOutput.Commitment, ref mask, 0);

                Console.WriteLine(minerOutput.Commitment.ToHex());

                minerOutput.UXKey = Key.I;
                minerOutput.Amount = 0;

                minertx.Outputs = new TXOutput[1] { minerOutput };

                minertx.TransactionKey = R;

                block.Coinbase = minertx;
            }

            block.BlockSize += block.Coinbase.Size();

            block.MerkleRoot = GetMerkleRoot(txs, block.Coinbase);

            /* Block hash is just the header hash, i.e. Hash(Version, Timestamp, Height, BlockSize, NumTXs, NumOutputs, PreviousBlock, MerkleRoot) */
            block.BlockHash = block.Hash();

            block.transactions = txs.ToArray();

            block.Transactions = new SHA256[block.transactions.Length];

            for (int k = 0; k < txs.Count; k++)
            {
                block.Transactions[k] = txs[k].Hash();
            }

            return block;
        }

        public static SHA256 GetMerkleRoot(List<FullTransaction> txs, Transaction tx)
        {
            List<SHA256> hashes = new();

            hashes.Add(tx.Hash());

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
            if (Transactions == null && transactions != null)
            {
                Transactions = new SHA256[transactions.Length];

                for (int k = 0; k < transactions.Length; k++)
                {
                    Transactions[k] = transactions[k].Hash();
                }
            }

            List<SHA256> hashes = new(Transactions);

            hashes.Insert(0, Coinbase.Hash());

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
            byte[] bytes = new byte[93];
            bytes[0] = Version;

            Serialization.CopyData(bytes, 1, Timestamp);
            Serialization.CopyData(bytes, 9, Height);
            Serialization.CopyData(bytes, 17, NumTXs);
            Serialization.CopyData(bytes, 21, BlockSize);
            Serialization.CopyData(bytes, 25, NumOutputs);
            Array.Copy(PreviousBlock.Bytes, 0, bytes, 29, 32);
            Array.Copy(MerkleRoot.Bytes, 0, bytes, 61, 32);

            return SHA256.HashData(bytes);
        }

        public virtual VerifyException Verify()
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

            DB.DB db = DB.DB.GetDB();

            if (Height != db.GetChainHeight() + 1)
            {
                return new VerifyException("Block", $"Block is not next in sequence (expected {db.GetChainHeight() + 1}, but got {Height})");
            }

            if (transactions == null)
            {
                return new VerifyException("Block", $"Block does not contain full transaction information (malformed)");
            }

            if (NumTXs != transactions.Length)
            {
                return new VerifyException("Block", $"Transaction count mismatch: expected {NumTXs}, but got {Transactions.Length})");
            }

            SHA256 merkleRoot = GetMerkleRoot();

            if(!merkleRoot.Equals(MerkleRoot))
            {
                return new VerifyException("Block", $"Merkle root mismatch: expected {MerkleRoot.ToHexShort()}, but got {merkleRoot.ToHexShort()}");
            }

            if (Height == 0)
            {
                if (!PreviousBlock.Equals(new SHA256(new byte[32], false)))
                {
                    return new VerifyException("Block", $"genesis block should point to zero hash, but got {PreviousBlock.ToHexShort()}");
                }
            }
            else
            {
                SHA256 prevBlockHash = db.GetBlock(Height - 1).BlockHash;

                if (!prevBlockHash.Equals(PreviousBlock))
                {
                    return new VerifyException("Block", $"previous block mismatch: expected {prevBlockHash.ToHexShort()} (previous block in database), but got {PreviousBlock.ToHexShort()}");
                }
            }

            SHA256 blockHash = Hash();

            if (!blockHash.Equals(BlockHash))
            {
                return new VerifyException("Block", $"block hash mismatch: expected {BlockHash.ToHexShort()}, but got {blockHash.ToHexShort()}");
            }

            ulong fee = 0;
            uint numOutputs = 0;
            uint blockSize = 133 + ((Coinbase == null) ? 0 : Coinbase.Size());

            for (int i = 0; i < transactions.Length; i++)
            {
                fee += transactions[i].Fee;
                numOutputs += transactions[i].NumOutputs;
                blockSize += transactions[i].Size();
            }

            if (fee != Fee)
            {
                return new VerifyException("Block", $"block fee mismatch: expected {Fee} as included in block, but got {fee} from calculations");
            }

            if (numOutputs != NumOutputs)
            {
                return new VerifyException("Block", $"block output count mismatch: expected {NumOutputs} as included in block, but got {numOutputs} from calculations");
            }

            if (blockSize != BlockSize)
            {
                return new VerifyException("Block", $"block size (in bytes) mismatch: expected {BlockSize} as included in block, but got {blockSize} from calculations");
            }

            /* verify coinbase */
            if (Coinbase == null)
            {
                return new VerifyException("Block", "No coinbase transaction detected");
            }

            if (Coinbase.Version != 0)
            {
                return new VerifyException("Block", "Miner tx not present or invalid");
            }

            if (Coinbase.Outputs == null || Coinbase.Outputs.Length != 1)
            {
                return new VerifyException("Block", "Miner tx has invalid outputs");
            }

            var minerexc = Coinbase.Verify();

            if (minerexc != null)
            {
                return minerexc;
            }

            /* now verify output amount matches commitment */
            Key feeComm = new(new byte[32]);
            KeyOps.GenCommitment(ref feeComm, ref Key.I, Fee);

            if (!feeComm.Equals(Coinbase.Outputs[0].Commitment))
            {
                return new VerifyException("Block", "Coinbase transaction in block does not balance with fee commitment!");
            }

            for (int i = 0; i < transactions.Length; i++)
            {
                if (Height > 0 && transactions[i].Version == 0)
                {
                    return new VerifyException("Block", "block contains coinbase transaction outside of miner tx");
                }

                var txexc = transactions[i].Verify();

                if (txexc != null)
                {
                    return txexc;
                }
            }

            return null;
        }

        /* Should not be called on genesis block. */
        public virtual VerifyException VerifyIncoming()
        {
            /* VerifyIncoming does the following:
             *   - ensures Height is equal to db.GetChainHeight() + 1
             *   - ensures all transactions are present in TXPool 
             *   - ensures MerkleRoot is proper
             *   - ensures PreviousBlock is the result of db.GetBlock(block.Height - 1).BlockHash
             *   - ensures BlockHash is proper
             *   - ensures BlockSize, NumOutputs, NumTXs and Fee are proper
             *   
             * VerifyIncoming() should be used for incoming blocks only. Validating previous blocks 
             * is not needed, as blocks are always processed in order.
             */

            DB.DB db = DB.DB.GetDB();

            if (Height != db.GetChainHeight() + 1)
            {
                return new VerifyException("Block", "Incoming", $"Block is not next in sequence (expected {db.GetChainHeight() + 1}, but got {Height})");
            }

            if (transactions == null)
            {
                try
                {
                    transactions = db.GetTXsFromPool(Transactions);
                }
                catch (Exception e)
                {
                    return new VerifyException("Block", "Incoming", $"Error while getting transactions from TXPool: " + e.Message);
                }

                for (int i = 0; i < Transactions.Length; i++)
                {
                    if (transactions[i] == null)
                    {
                        return new VerifyException("Block", "Incoming", $"Not all transactions in block are in TXPool: first tx not present is {Transactions[i].ToHexShort()}");
                    }
                }
            }

            if (NumTXs != transactions.Length)
            {
                return new VerifyException("Block", "Incoming", $"Transaction count mismatch: expected {NumTXs}, but got {Transactions.Length})");
            }

            SHA256 merkleRoot = GetMerkleRoot();

            if (!merkleRoot.Equals(MerkleRoot))
            {
                return new VerifyException("Block", "Incoming", $"Merkle root mismatch: expected {MerkleRoot.ToHexShort()}, but got {merkleRoot.ToHexShort()}");
            }

            SHA256 prevBlockHash = db.GetBlock(Height - 1).BlockHash;

            if (!prevBlockHash.Equals(PreviousBlock))
            {
                return new VerifyException("Block", "Incoming", $"previous block mismatch: expected {prevBlockHash.ToHexShort()} (previous block in database), but got {PreviousBlock.ToHexShort()}");
            }

            SHA256 blockHash = Hash();

            if (!blockHash.Equals(BlockHash))
            {
                return new VerifyException("Block", "Incoming", $"block hash mismatch: expected {BlockHash.ToHexShort()}, but got {blockHash.ToHexShort()}");
            }

            ulong fee = 0;
            uint numOutputs = 0;
            uint blockSize = 133;

            for (int i = 0; i < transactions.Length; i++)
            {
                fee += transactions[i].Fee;
                numOutputs += transactions[i].NumOutputs;
                blockSize += transactions[i].Size();
            }

            if (fee != Fee)
            {
                return new VerifyException("Block", "Incoming", $"block fee mismatch: expected {Fee} as included in block, but got {fee} from calculations");
            }

            if (numOutputs != NumOutputs)
            {
                return new VerifyException("Block", "Incoming", $"block output count mismatch: expected {NumOutputs} as included in block, but got {numOutputs} from calculations");
            }

            if (blockSize != BlockSize)
            {
                return new VerifyException("Block", "Incoming", $"block size (in bytes) mismatch: expected {BlockSize} as included in block, but got {blockSize} from calculations");
            }

            return null;
        }
    }
}
