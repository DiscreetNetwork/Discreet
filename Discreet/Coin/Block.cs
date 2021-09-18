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
        public Cipher.SHA256[] Transactions; // 32 * len + 129

        public byte[] Marshal()
        {
            byte[] rv = new byte[129 + 32 * Transactions.Length];

            rv[0] = Version;

            Serialization.CopyData(rv, 1, Timestamp);
            Serialization.CopyData(rv, 9, Height);
            Serialization.CopyData(rv, 17, Fee);

            Array.Copy(PreviousBlock.Bytes, 0, rv, 25, 32);
            Array.Copy(BlockHash.Bytes, 0, rv, 57, 32);
            Array.Copy(MerkleRoot.Bytes, 0, rv, 89, 32);

            Serialization.CopyData(rv, 121, NumTXs);
            Serialization.CopyData(rv, 125, BlockSize);

            for (int i = 0; i < NumTXs; i++)
            {
                Array.Copy(Transactions[i].Bytes, 0, rv, 129 + i * 32, 32);
            }

            return rv;
        }

        public void Marshal(byte[] bytes, uint offset)
        {
            Array.Copy(Marshal(), 0, bytes, offset, Size());
        }

        public string Readable()
        {
            /* not really used yet */
            throw new NotImplementedException();
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

            Transactions = new Cipher.SHA256[NumTXs];

            for (int i = 0; i < NumTXs; i++)
            {
                Transactions[i] = new SHA256(bytes[(129 + i * 32)..(129 + (i + 1) * 32)], false);
            }
        }

        public void Unmarshal(byte[] bytes, uint _offset)
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

            Transactions = new Cipher.SHA256[NumTXs];

            for (int i = 0; i < NumTXs; i++)
            {
                Transactions[i] = new SHA256(bytes[(offset + 129 + i * 32)..(offset + 129 + (i + 1) * 32)], false);
            }
        }

        public uint Size()
        {
            return 129 + 32 * (uint)Transactions.Length;
        }

        public SHA256 Hash()
        {
            throw new NotImplementedException();
        }
    }
}
