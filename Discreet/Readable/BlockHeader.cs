using Discreet.Common;
using Discreet.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Discreet.Readable
{
    public class BlockHeader
    {
        public byte Version { get; set; }
        public ulong Timestamp { get; set; }
        public long Height { get; set; }
        public ulong Fee { get; set; }

        public string PreviousBlock { get; set; }
        public string BlockHash { get; set; }

        public string MerkleRoot { get; set; }

        public uint NumTXs { get; set; }
        public uint BlockSize { get; set; }
        public uint NumOutputs { get; set; }

        public uint ExtraLen { get; set; }
        public string Extra { get; set; }


        public virtual string JSON()
        {
            return JsonSerializer.Serialize(this, ReadableOptions.Options);
        }

        public override string ToString()
        {
            return JSON();
        }

        public virtual void FromJSON(string json)
        {
            BlockHeader b = JsonSerializer.Deserialize<BlockHeader>(json);

            Version = b.Version;
            Timestamp = b.Timestamp;
            Height = b.Height;

            PreviousBlock = b.PreviousBlock;
            BlockHash = b.BlockHash;

            MerkleRoot = b.MerkleRoot;

            NumTXs = b.NumTXs;
            BlockSize = b.BlockSize;
            NumOutputs = b.NumOutputs;

            ExtraLen = b.ExtraLen;
            Extra = b.Extra;
        }

        public BlockHeader(Coin.Models.BlockHeader obj)
        {
            FromObject(obj);
        }
        public BlockHeader(string json)
        {
            FromJSON(json);
        }

        public BlockHeader() { }

        public virtual void FromObject<T>(object obj)
        {
            if (typeof(T) == typeof(Coin.Models.BlockHeader))
            {
                FromObject((Coin.Models.BlockHeader)obj);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(BlockHeader).FullName);
            }
        }

        public virtual void FromObject(Coin.Models.BlockHeader obj)
        {
            Version = obj.Version;
            Timestamp = obj.Timestamp;
            Height = obj.Height;
            Fee = obj.Fee;

            if (obj.PreviousBlock.Bytes != null) PreviousBlock = obj.PreviousBlock.ToHex();
            if (obj.BlockHash.Bytes != null) BlockHash = obj.BlockHash.ToHex();

            if (obj.MerkleRoot.Bytes != null) MerkleRoot = obj.MerkleRoot.ToHex();

            NumTXs = obj.NumTXs;
            BlockSize = obj.BlockSize;
            NumOutputs = obj.NumOutputs;

            ExtraLen = obj.ExtraLen;
            if (obj.Extra != null) Extra = Printable.Hexify(obj.Extra);
        }

        public virtual T ToObject<T>()
        {
            if (typeof(T) == typeof(Coin.Models.BlockHeader))
            {
                return (T)ToObject();
            }
            else
            {
                throw new ReadableException(typeof(BlockHeader).FullName, typeof(T).FullName);
            }
        }

        public virtual object ToObject()
        {
            Coin.Models.BlockHeader obj = new();

            obj.Version = Version;
            obj.Timestamp = Timestamp;
            obj.Height = Height;
            obj.Fee = Fee;

            if (PreviousBlock != null && PreviousBlock != "") obj.PreviousBlock = new Cipher.SHA256(Printable.Byteify(PreviousBlock), false);
            if (BlockHash != null && BlockHash != "") obj.BlockHash = new Cipher.SHA256(Printable.Byteify(BlockHash), false);

            if (MerkleRoot != null && MerkleRoot != "") obj.MerkleRoot = new Cipher.SHA256(Printable.Byteify(MerkleRoot), false);

            obj.NumTXs = NumTXs;
            obj.BlockSize = BlockSize;
            obj.NumOutputs = NumOutputs;

            if (Extra != null && Extra != "") obj.Extra = Printable.Byteify(Extra);

            return obj;
        }

        public static Coin.Models.BlockHeader FromReadable(string json)
        {
            return (Coin.Models.BlockHeader)new BlockHeader(json).ToObject();
        }

        public static string ToReadable(Coin.Models.BlockHeader obj)
        {
            return new BlockHeader(obj).JSON();
        }
    }
}
