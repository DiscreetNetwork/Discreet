using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Cipher;

namespace Discreet.Coin
{
    /**
     * Signed Blocks are used by the Discreet Testnet to verify a masternode minted it.
     */
    public class SignedBlock: Block
    {
        public Cipher.Signature Sig;

        public override byte[] Marshal()
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

            Array.Copy(Sig.ToBytes(), 0, bytes, 133, 96);

            uint offset = 133 + 96;

            if (Version == 2)
            {
                Coinbase.Marshal(bytes, offset);
                offset += Coinbase.Size();
            }

            foreach (SHA256 tx in Transactions)
            {
                Array.Copy(tx.Bytes, 0, bytes, offset, 32);
                offset += 32;
            }

            return bytes;
        }

        public override byte[] MarshalFull()
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

            Array.Copy(Sig.ToBytes(), 0, bytes, 133, 96);

            uint offset = 133 + 96;

            if (Version == 2)
            {
                Coinbase.Marshal(bytes, offset);
                offset += Coinbase.Size();
            }

            for (int i = 0; i < transactions.Length; i++)
            {
                transactions[i].Marshal(bytes, offset);
                offset += transactions[i].Size();
            }

            return bytes;
        }

        public override void Marshal(byte[] bytes, uint offset)
        {
            Array.Copy(Marshal(), 0, bytes, offset, Size());
        }

        public override void MarshalFull(byte[] bytes, uint offset)
        {
            Array.Copy(MarshalFull(), 0, bytes, offset, SizeFull());
        }

        public override string Readable()
        {
            return Discreet.Readable.SignedBlock.ToReadable(this);
        }

        public override string ReadableFull()
        {
            return Readable();
        }

        public static new SignedBlock FromReadable(string json)
        {
            return Discreet.Readable.SignedBlock.FromReadable(json);
        }

        public override void Unmarshal(byte[] bytes)
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

            Sig = new Signature(bytes, 133);

            Transactions = new Cipher.SHA256[NumTXs];

            uint offset = 133 + 96;

            if (Version == 2)
            {
                Coinbase = new Transaction();
                offset = Coinbase.Unmarshal(bytes, offset);
            }
            
            for (int i = 0; i < NumTXs; i++)
            {
                byte[] data = new byte[32];
                Array.Copy(bytes, offset, data, 0, 32);
                offset += 32;
                Transactions[i] = new SHA256(data, false);
            }
        }

        public override void UnmarshalFull(byte[] bytes)
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

            Sig = new Signature(bytes, 133);

            Transactions = new Cipher.SHA256[NumTXs];

            uint _offset = 133 + 96;

            if (Version == 2)
            {
                Coinbase = new Transaction();
                _offset = Coinbase.Unmarshal(bytes, _offset);
            }

            transactions = new FullTransaction[NumTXs];

            for (int i = 0; i < NumTXs; i++)
            {
                transactions[i] = new FullTransaction();
                transactions[i].Unmarshal(bytes, _offset);
                _offset += transactions[i].Size();
                Transactions[i] = transactions[i].Hash();
            }
        }

        public override uint Unmarshal(byte[] bytes, uint _offset)
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

            Sig = new Signature(bytes, _offset + 133);

            Transactions = new Cipher.SHA256[NumTXs];

            if (Version == 2)
            {
                Coinbase = new Transaction();
                _offset = Coinbase.Unmarshal(bytes, _offset + 133 + 96);
            }

            for (int i = 0; i < NumTXs; i++)
            {
                byte[] data = new byte[32];
                Array.Copy(bytes, _offset, data, 0, 32);
                _offset += 32;
                Transactions[i] = new SHA256(data, false);
            }

            return _offset + Size();
        }

        public override uint UnmarshalFull(byte[] bytes, uint _offset)
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

            Sig = new Signature(bytes, _offset + 133);

            if (Version == 2)
            {
                Coinbase = new Transaction();
                _offset = Coinbase.Unmarshal(bytes, _offset + 133 + 96);
            }

            for (int i = 0; i < NumTXs; i++)
            {
                transactions[i] = new FullTransaction();
                transactions[i].Unmarshal(bytes, _offset);
                _offset += transactions[i].Size();
                Transactions[i] = transactions[i].Hash();
            }

            return _offset + SizeFull();
        }

        public override uint Size()
        {
            return base.Size() + 96;
        }

        public override uint SizeFull()
        {
            return base.SizeFull() + 96;
        }

        public static SignedBlock Build(List<FullTransaction> txs, StealthAddress miner, Key signingKey)
        {
            SignedBlock block = new();
            block.Timestamp = (ulong)DateTime.Now.Ticks;
            block.NumTXs = (uint)txs.Count;
            block.Version = 1;

            block.Fee = 0;
            block.NumOutputs = 0;
            block.BlockSize = 133 + 96;

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
                block.Version = 2;

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

            block.Sig = KeyOps.Sign(ref signingKey, block.BlockHash);

            return block;
        }

        public override VerifyException Verify()
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

            if (Version != 1 && Version != 2)
            {
                return new VerifyException("Block", $"Unsupported version (signed blocks are either version 1 or 2); got version {Version}");
            }

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

            if (!merkleRoot.Equals(MerkleRoot))
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
            uint blockSize = 133 + 96 + ((Coinbase == null) ? 0 : Coinbase.Size());

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

            /* verify coinbase; only performed if Coinbase is not null */
            if (Version == 2)
            {
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
                Key _I = Key.Copy(Key.I);
                KeyOps.GenCommitment(ref feeComm, ref _I, Fee);

                if (!feeComm.Equals(Coinbase.Outputs[0].Commitment))
                {
                    return new VerifyException("Block", "Coinbase transaction in block does not balance with fee commitment!");
                }
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

            if (!CheckSignature())
            {
                return new VerifyException("SignedBlock", "block signature is invalid and/or does not come from a masternode!");
            }

            return null;
        }

        public bool CheckSignature()
        {
            return Sig.Verify(BlockHash) && IsMasternode(Sig.y);
        }

        public static bool IsMasternode(Cipher.Key k)
        {
            //TODO: Implement hardcoded masternode IDs
            return true;
        }
    }
}
