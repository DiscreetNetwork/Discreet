using Discreet.Cipher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Coin
{
    public class BlockHeader: ICoin
    {
        public byte Version;

        public ulong Timestamp;
        public long Height;
        public ulong Fee;      // 25

        public SHA256 PreviousBlock;
        public SHA256 BlockHash;          //  89

        public SHA256 MerkleRoot;        // 121

        public uint NumTXs;
        public uint BlockSize;              // 129
        public uint NumOutputs;             // 133

        public uint ExtraLen;
        public byte[] Extra;

        public void Deserialize(byte[] bytes)
        {
            Deserialize(bytes, 0);
        }

        public uint Deserialize(byte[] bytes, uint offset)
        {
            Version = bytes[offset];

            Timestamp = Serialization.GetUInt64(bytes, offset + 1);
            Height = Serialization.GetInt64(bytes, offset + 9);
            Fee = Serialization.GetUInt64(bytes, offset + 17);

            PreviousBlock = new SHA256(bytes, offset + 25);
            BlockHash = new SHA256(bytes, offset + 57);
            MerkleRoot = new SHA256(bytes, offset + 89);

            NumTXs = Serialization.GetUInt32(bytes, offset + 121);
            BlockSize = Serialization.GetUInt32(bytes, offset + 125);
            NumOutputs = Serialization.GetUInt32(bytes, offset + 129);

            ExtraLen = Serialization.GetUInt32(bytes, offset + 133);
            Extra = new byte[ExtraLen];
            Array.Copy(bytes, offset + 137, Extra, 0, ExtraLen);

            return offset + Size();
        }

        public void Deserialize(Stream s)
        {
            Version = (byte)s.ReadByte();

            Timestamp = Serialization.GetUInt64(s);
            Height = Serialization.GetInt64(s);
            Fee = Serialization.GetUInt64(s);

            PreviousBlock = new SHA256(s);
            BlockHash = new SHA256(s);
            MerkleRoot = new SHA256(s);

            NumTXs = Serialization.GetUInt32(s);
            BlockSize = Serialization.GetUInt32(s);
            NumOutputs = Serialization.GetUInt32(s);

            ExtraLen = Serialization.GetUInt32(s);
            Extra = new byte[ExtraLen];
            s.Read(Extra);
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
            return Discreet.Readable.BlockHeader.ToReadable(this);
        }

        public object ToReadable()
        {
            return new Discreet.Readable.BlockHeader(this);
        }

        public static BlockHeader FromReadable(string json)
        {
            return Discreet.Readable.BlockHeader.FromReadable(json);
        }

        public byte[] Serialize()
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

            Serialization.CopyData(bytes, 133, ExtraLen);
            Array.Copy(Extra ?? Array.Empty<byte>(), bytes, Extra?.Length ?? 0);

            return bytes;
        }

        public void Serialize(byte[] bytes, uint offset)
        {
            byte[] _bytes = Serialize();

            Array.Copy(_bytes, 0, bytes, offset, _bytes.Length);
        }

        public void Serialize(Stream s)
        {
            s.WriteByte(Version);
            
            s.Write(Serialization.UInt64(Timestamp));
            s.Write(Serialization.Int64(Height));
            s.Write(Serialization.UInt64(Fee));

            s.Write(PreviousBlock.Bytes);
            s.Write(BlockHash.Bytes);
            s.Write(MerkleRoot.Bytes);

            s.Write(Serialization.UInt32(NumTXs));
            s.Write(Serialization.UInt32(BlockSize));
            s.Write(Serialization.UInt32(NumOutputs));

            s.Write(Serialization.UInt32(ExtraLen));
            s.Write(Extra ?? Array.Empty<byte>());
        }

        public VerifyException Verify()
        {
            return null;
        }

        public uint Size()
        {
            return 137 + (uint)(Extra?.Length ?? 0);
        }
    }
}
