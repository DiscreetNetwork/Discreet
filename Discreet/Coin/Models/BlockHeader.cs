using Discreet.Cipher;
using Discreet.Common;
using Discreet.Common.Exceptions;
using Discreet.Common.Serialize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Discreet.Coin.Models
{
    public class BlockHeader : IHashable
    {
        public byte Version { get; set; }

        public ulong Timestamp { get; set; }
        public long Height { get; set; }
        public ulong Fee { get; set; }      // 25

        public SHA256 PreviousBlock { get; set; }
        public SHA256 BlockHash { get; set; }          //  89

        public SHA256 MerkleRoot { get; set; }        // 121

        public uint NumTXs { get; set; }
        public uint BlockSize { get; set; }              // 129
        public uint NumOutputs { get; set; }             // 133

        public uint ExtraLen => (uint)Extra.Length;
        public byte[] Extra { get; set; }

        public int Size => 137 + (int)ExtraLen;

        public void Deserialize(ref MemoryReader reader)
        {
            Version = reader.ReadUInt8();

            Timestamp = reader.ReadUInt64();
            Height = reader.ReadInt64();
            Fee = reader.ReadUInt64();

            PreviousBlock = reader.ReadSHA256();
            BlockHash = reader.ReadSHA256();
            MerkleRoot = reader.ReadSHA256();

            NumTXs = reader.ReadUInt32();
            BlockSize = reader.ReadUInt32();
            NumOutputs = reader.ReadUInt32();

            Extra = reader.ReadByteArray();
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

        public string Readable()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new Converters.BlockHeaderConverter());

            return JsonSerializer.Serialize(this, typeof(BlockHeader), options);
        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(Timestamp);
            writer.Write(Height);
            writer.Write(Fee);
            writer.WriteSHA256(PreviousBlock);
            writer.WriteSHA256(BlockHash);
            writer.WriteSHA256(MerkleRoot);
            writer.Write(NumTXs);
            writer.Write(BlockSize);
            writer.Write(NumOutputs);
            writer.WriteByteArray(Extra);
        }

        public bool CheckSignature()
        {
            if (Extra == null || Extra.Length != 96) return false;

            var sig = new Signature(Extra);
            return sig.Verify(BlockHash) && Block.IsMasternode(sig.y);
        }
    }
}
