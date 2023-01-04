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
        public ulong Fee;                 // 25

        public SHA256 PreviousBlock;
        public SHA256 BlockHash;          // 89

        public SHA256 MerkleRoot;         // 121

        public uint NumTXs;
        public uint BlockSize;            // 129
        public uint NumOutputs;           // 133

        public uint ExtraLen;
        public byte[] Extra;

        /// <summary>
        /// Deserialize initializes the block instance from the deserialization
        /// of a byte array contaning the data of a block header.
        /// </summary>
        /// <param name="bytes">A byte array that will hold a copy of the block
        /// header serialization.</param>
        public void Deserialize(byte[] bytes)
        {
            Deserialize(bytes, 0);
        }

        /// <summary>
        /// Deserialize initializes the block header instance from the
        /// deserialization of a byte array, starting from an offset, which
        /// contains the data of a block header.
        /// </summary>
        /// <param name="bytes">A byte array that will hold a copy of the block
        /// header serialization.</param>
        /// <param name="offset">An offset that works as the starting index for
        /// the byte array to start the deserialization.</param>
        /// <returns>An offset equal to the original offset, plus the length of
        /// the deserialized block header.</returns>
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

        /// <summary>
        /// Deserialize initializes the block header instance from the
        /// deserialization of bytes contained in a stream, which represent the
        /// data of a block header.
        /// </summary>
        /// <param name="s">A byte stream.</param>
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

        /// <summary>
        /// Hash hashes the block's header.
        /// </summary>
        /// <returns>The hash of the block's header.</returns>
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

        /// <summary>
        /// Readable encodes this block header to a string containing an
        /// equivalent JSON.
        /// </summary>
        public string Readable()
        {
            return Discreet.Readable.BlockHeader.ToReadable(this);
        }

        /// <summary>
        /// ToReadable converts the block header to an object containing the
        /// block header in JSON format.
        /// </summary>
        /// <returns>A readable block header.</returns>
        public object ToReadable()
        {
            return new Discreet.Readable.BlockHeader(this);
        }

        /// <summary>
        /// FromReadable returns a BlockHeader that is created from a
        /// stringified readable block header.
        /// </summary>
        /// <returns>A BlockHeader instance equivalent to the provided
        /// JSON.</returns>
        public static BlockHeader FromReadable(string json)
        {
            return Discreet.Readable.BlockHeader.FromReadable(json);
        }

        /// <summary>
        /// Serialize returns an array that contains all the data in a
        /// BlockHeader transformed to bytes.
        /// </summary>
        /// <returns>The byte array containing the BlockHeader data.</returns>
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
            Array.Copy(Extra ?? Array.Empty<byte>(), 0, bytes, 137, ExtraLen);

            return bytes;
        }

        /// <summary>
        /// Serialize copies this BlockHeader's serialized representation to a given
        /// byte array, starting at an offset.
        /// </summary>
        /// <param name="bytes">A byte array that will hold a copy of the block
        /// header serialization.</param>
        /// <param name="offset">The offset at which we will copy the block
        /// header serialization.</param>
        public void Serialize(byte[] bytes, uint offset)
        {
            byte[] _header = Serialize();

            Array.Copy(_header, 0, bytes, offset, _header.Length);
        }

        /// <summary> Serialize populates a byte stream that contains all the
        /// data in a BlockHeader transformed to bytes.
        /// </summary>
        /// <param name="s">A byte stream.</param>
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

        /// <summary>
        /// Verify does the following:
        /// -
        /// </summary>
        /// <returns>An exception, in case of an error, null otherwise.</returns>
        public VerifyException Verify()
        {
            return null;
        }

        /// <summary>
        /// Size calculates the size of the block header.
        /// </summary>
        /// <returns>The size of the block header.</returns>
        public uint Size()
        {
            return 137 + ExtraLen;
        }

        /// <summary>
        /// CheckSignature checks that the block header signature is valid and
        /// that it's coming from a master node.
        /// </summary>
        /// <returns>True if the block signature is valid, false otherwise.</returns>
        public bool CheckSignature()
        {
            if (Extra == null || Extra.Length != 96) return false;

            var sig = new Signature(Extra);
            return sig.Verify(BlockHash) && Block.IsMasternode(sig.y);
        }
    }
}
