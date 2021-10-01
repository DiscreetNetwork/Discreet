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
        public ulong Height;
        public ulong Fee;      // 25

        public Cipher.SHA256 PreviousBlock;
        public Cipher.SHA256 BlockHash;          //  89

        public Cipher.SHA256 MerkleRoot;        // 121

        public uint NumTXs;
        public uint BlockSize;              // 129
        public uint NumOutputs;             // 133
        public Cipher.SHA256[] Transactions; // 32 * len + 133

        /* used for full blocks (not packed with blocks) */
        public Transaction[] transactions;

        public byte[] Marshal()
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

            for (int i = 0; i < NumTXs; i++)
            {
                Array.Copy(Transactions[i].Bytes, 0, bytes, 133 + i * 32, 32);
            }

            return bytes;
        }

        public byte[] MarshalFull()
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

            for (int i = 0; i < NumTXs; i++)
            {
                transactions[i].Marshal(bytes, offset);
                offset += transactions[i].Size();
            }

            return bytes;
        }

        public void MarshalFull(byte[] bytes, uint offset)
        {
            Array.Copy(MarshalFull(), 0, bytes, offset, SizeFull());
        }

        public void Marshal(byte[] bytes, uint offset)
        {
            Array.Copy(Marshal(), 0, bytes, offset, Size());
        }

        public string Readable()
        {
            /* not really used yet */
            string rv = $"{{\"Version\":{Version},\"Height\":{Height},\"Fee\":{Fee},\"PreviousBlock\":\"{PreviousBlock.ToHex()}\",\"BlockHash\":\"{BlockHash.ToHex()}\",\"MerkleRoot\":\"{MerkleRoot.ToHex()}\",\"NumTXs\":{NumTXs},\"BlockSize\":{BlockSize},\"NumOutputs\":{NumOutputs},\"Transactions\":[";

            for (int i = 0; i < Transactions.Length; i++)
            {
                rv += $"\"{Transactions[i].ToHex()}\"";

                if (i < Transactions.Length - 1)
                {
                    rv += ",";
                }
            }

            rv += "]}";

            return rv;
        }

        public string ReadableFull()
        {
            /* not really used yet */
            string rv = $"{{\"Version\":{Version},\"Height\":{Height},\"Fee\":{Fee},\"PreviousBlock\":\"{PreviousBlock.ToHex()}\",\"BlockHash\":\"{BlockHash.ToHex()}\",\"MerkleRoot\":\"{MerkleRoot.ToHex()}\",\"NumTXs\":{NumTXs},\"BlockSize\":{BlockSize},\"NumOutputs\":{NumOutputs},\"Transactions\":[";

            for (int i = 0; i < transactions.Length; i++)
            {
                rv += transactions[i].Readable();

                if (i < transactions.Length - 1)
                {
                    rv += ",";
                }
            }

            rv += "]}";

            return rv;
        }

        public void Unmarshal(byte[] bytes)
        {
            Version = bytes[0];

            Timestamp = Serialization.GetUInt64(bytes, 1);
            Height = Serialization.GetUInt64(bytes, 9);
            Fee = Serialization.GetUInt64(bytes, 17);

            PreviousBlock = new SHA256(bytes[25..57], false);
            BlockHash = new SHA256(bytes[57..89], false);
            MerkleRoot = new SHA256(bytes[89..121], false);

            NumTXs = Serialization.GetUInt32(bytes, 121);
            BlockSize = Serialization.GetUInt32(bytes, 125);
            NumOutputs = Serialization.GetUInt32(bytes, 129);

            Transactions = new Cipher.SHA256[NumTXs];

            for (int i = 0; i < NumTXs; i++)
            {
                Transactions[i] = new SHA256(bytes[(133 + i * 32)..(133 + (i + 1) * 32)], false);
            }
        }

        public void UnmarshalFull(byte[] bytes)
        {
            Version = bytes[0];

            Timestamp = Serialization.GetUInt64(bytes, 1);
            Height = Serialization.GetUInt64(bytes, 9);
            Fee = Serialization.GetUInt64(bytes, 17);

            PreviousBlock = new SHA256(bytes[25..57], false);
            BlockHash = new SHA256(bytes[57..89], false);
            MerkleRoot = new SHA256(bytes[89..121], false);

            NumTXs = Serialization.GetUInt32(bytes, 121);
            BlockSize = Serialization.GetUInt32(bytes, 125);
            NumOutputs = Serialization.GetUInt32(bytes, 129);

            Transactions = new Cipher.SHA256[NumTXs];

            uint _offset = 133;

            for (int i = 0; i < NumTXs; i++)
            {
                transactions[i] = new Transaction();
                transactions[i].Unmarshal(bytes, _offset);
                _offset += transactions[i].Size();
                Transactions[i] = transactions[i].Hash();
            }
        }

        public uint Unmarshal(byte[] bytes, uint _offset)
        {
            int offset = (int)_offset;
            Version = bytes[offset];

            Timestamp = Serialization.GetUInt64(bytes, _offset + 1);
            Height = Serialization.GetUInt64(bytes, _offset + 9);
            Fee = Serialization.GetUInt64(bytes, _offset + 17);

            PreviousBlock = new SHA256(bytes[(offset + 25)..(offset + 57)], false);
            BlockHash = new SHA256(bytes[(offset + 57)..(offset + 89)], false);
            MerkleRoot = new SHA256(bytes[(offset + 89)..(offset + 121)], false);

            NumTXs = Serialization.GetUInt32(bytes, _offset + 121);
            BlockSize = Serialization.GetUInt32(bytes, _offset + 125);
            NumOutputs = Serialization.GetUInt32(bytes, _offset + 129);

            Transactions = new Cipher.SHA256[NumTXs];

            for (int i = 0; i < NumTXs; i++)
            {
                Transactions[i] = new SHA256(bytes[(offset + 133 + i * 32)..(offset + 133 + (i + 1) * 32)], false);
            }

            return _offset + Size();
        }

        public uint UnmarshalFull(byte[] bytes, uint _offset)
        {
            int offset = (int)_offset;
            Version = bytes[offset];

            Timestamp = Serialization.GetUInt64(bytes, _offset + 1);
            Height = Serialization.GetUInt64(bytes, _offset + 9);
            Fee = Serialization.GetUInt64(bytes, _offset + 17);

            PreviousBlock = new SHA256(bytes[(offset + 25)..(offset + 57)], false);
            BlockHash = new SHA256(bytes[(offset + 57)..(offset + 89)], false);
            MerkleRoot = new SHA256(bytes[(offset + 89)..(offset + 121)], false);

            NumTXs = Serialization.GetUInt32(bytes, _offset + 121);
            BlockSize = Serialization.GetUInt32(bytes, _offset + 125);
            NumOutputs = Serialization.GetUInt32(bytes, _offset + 129);

            Transactions = new Cipher.SHA256[NumTXs];
            transactions = new Transaction[NumTXs];

            _offset += 133;

            for (int i = 0; i < NumTXs; i++)
            {
                transactions[i] = new Transaction();
                transactions[i].Unmarshal(bytes, _offset);
                _offset += transactions[i].Size();
                Transactions[i] = transactions[i].Hash();
            }

            return _offset + SizeFull();
        }

        public uint Size()
        {
            return 133 + 32 * (uint)Transactions.Length;
        }

        public uint SizeFull()
        {
            uint size = 133;

            for (int i = 0; i < transactions.Length; i++)
            {
                size += transactions[i].Size();
            }

            return size;
        }

        /* for testing purposes only */
        public static Block BuildRandom(StealthAddress[] addresses, int[] numOutputs)
        {
            List<Transaction> txs = new List<Transaction>();

            for (int i = 0; i < addresses.Length; i++)
            {
                for (int j = 0; j < numOutputs[i] / 16; j++)
                {
                    txs.Add(Transaction.GenerateRandomNoSpend(addresses[i], 16));
                }

                if (numOutputs[i] % 16 != 0)
                {
                    txs.Add(Transaction.GenerateRandomNoSpend(addresses[i], numOutputs[i] % 16));
                }
            }

            return Build(txs, null);
        }

        public static Block Build(List<Transaction> txs, StealthAddress miner)
        {
            Block block = new Block();
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

            if (block.Fee > 0 && miner != null)
            {
                /* Construct miner TX */
                Transaction minertx = new Transaction();
                minertx.Version = 0;
                minertx.NumInputs = 0;
                minertx.NumOutputs = 1;
                minertx.NumSigs = 0;

                minertx.ExtraLen = 34;
                minertx.Extra = new byte[34];
                minertx.Extra[0] = 1;
                minertx.Extra[1] = 0;

                Key R = new Key(new byte[32]);
                Key r = new Key(new byte[32]);

                KeyOps.GenerateKeypair(ref r, ref R);

                /*Key cscalar = KeyOps.ScalarmultKey(ref miner.view, ref r);
                byte[] tmp = new byte[36];
                Array.Copy(cscalar.bytes, tmp, 32);
                Key c = new Key(new byte[32]);
                HashOps.HashToScalar(ref c, tmp, 36);*/

                TXOutput minerOutput = new TXOutput();
                minerOutput.Commitment = new Key(new byte[32]);
                Key mask = KeyOps.GenCommitmentMask(ref r, ref miner.view, 0);
                KeyOps.GenCommitment(ref minerOutput.Commitment, ref mask, block.Fee);

                minerOutput.UXKey = KeyOps.DKSAP(ref r, miner.view, miner.spend, 0);
                minerOutput.Amount = KeyOps.GenAmountMask(ref r, ref miner.view, 0, block.Fee);

                minertx.Outputs = new TXOutput[1] { minerOutput };

                Array.Copy(R.bytes, 0, minertx.Extra, 2, 32);

                txs.Add(minertx);
            }

            block.MerkleRoot = GetMerkleRoot(txs);

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

        public static SHA256 GetMerkleRoot(List<Transaction> txs)
        {
            List<SHA256> hashes = new List<SHA256>();

            for (int k = 0; k < txs.Count; k++)
            {
                hashes.Add(txs[k].Hash());
            }

            while (hashes.Count > 1)
            {
                hashes = getMerkleRoot(hashes);
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

            List<SHA256> hashes = new List<SHA256>(Transactions);

            while (hashes.Count > 1)
            {
                hashes = getMerkleRoot(hashes);
            }

            return hashes[0];
        }

        private static List<SHA256> getMerkleRoot(List<SHA256> hashes)
        {
            List<SHA256> newHashes = new List<SHA256>();

            for (int i = 0; i < hashes.Count / 2; i++)
            {
                byte[] data = new byte[64];
                Array.Copy(hashes[2 * i].Bytes, data, 32);
                Array.Copy(hashes[2 * i + 1].Bytes, 0, data, 32, 32);
                newHashes.Add(SHA256.HashData(data));
            }

            if (hashes.Count % 2 != 0)
            {
                newHashes.Add(SHA256.HashData(hashes[hashes.Count - 1].Bytes));
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

            SHA256 prevBlockHash = db.GetBlock(Height - 1).BlockHash;

            if (!prevBlockHash.Equals(PreviousBlock))
            {
                return new VerifyException("Block", $"previous block mismatch: expected {prevBlockHash.ToHexShort()} (previous block in database), but got {PreviousBlock.ToHexShort()}");
            }

            SHA256 blockHash = Hash();

            if (!blockHash.Equals(BlockHash))
            {
                return new VerifyException("Block", $"block hash mismatch: expected {BlockHash.ToHexShort()}, but got {blockHash.ToHexShort()}");
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

            for (int i = 0; i < transactions.Length; i++)
            {
                var txexc = transactions[i].Verify();

                if (txexc != null)
                {
                    return txexc;
                }
            }

            return null;
        }

        public VerifyException VerifyIncoming()
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
