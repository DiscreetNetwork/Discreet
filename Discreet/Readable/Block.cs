using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Discreet.Common.Exceptions;
using Discreet.Common;
using Discreet.RPC.Common;
using System.Text.Json.Serialization;

namespace Discreet.Readable
{
    public class Block : IReadable
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

        public List<string> Transactions { get; set; }

        /* this is fine since we disallow both serialization of Transactions and transactions; mutually exclusive */
        [JsonPropertyName("Transactions")]
        public List<string> transactions { get; set; }

        public string JSON()
        {
            return JsonSerializer.Serialize(this, ReadableOptions.Options);
        }

        public override string ToString()
        {
            return JSON();
        }

        public void FromJSON(string json)
        {
            Block b = JsonSerializer.Deserialize<Block>(json);
            
            Version = b.Version;
            Timestamp = b.Timestamp;
            Height = b.Height;
            
            PreviousBlock = b.PreviousBlock;
            BlockHash = b.BlockHash;
            
            MerkleRoot = b.MerkleRoot;
            
            NumTXs = b.NumTXs;
            BlockSize = b.BlockSize;
            NumOutputs = b.NumOutputs;
            
            Transactions = b.Transactions;

            transactions = b.transactions;
        }

        public Block(Coin.Block obj)
        {
            FromObject(obj);
        }
        public Block(string json)
        {
            FromJSON(json);
        }

        public Block() { }

        public void FromObject<T>(T obj)
        {
            if (typeof(T) == typeof(Coin.Block))
            {
                dynamic t = obj;
                FromObject((Coin.Block)t);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(Block).FullName);
            }
        }

        public void FromObject(Coin.Block obj)
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

            if (obj.transactions != null)
            {
                transactions = new List<string>(obj.transactions.Length);

                for (int i = 0; i < obj.transactions.Length; i++)
                {
                    transactions.Add(new FullTransaction(obj.transactions[i]).JSON());
                }
            }
            else if (obj.Transactions != null)
            {
                Transactions = new List<string>(obj.Transactions.Length);

                for (int i = 0; i < obj.Transactions.Length; i++)
                {
                    Transactions.Add(obj.Transactions[i].ToHex());
                }
            }
        }

        public T ToObject<T>()
        {
            if (typeof(T) == typeof(Coin.Block))
            {
                dynamic t = ToObject();
                return (T)t;
            }
            else
            {
                throw new ReadableException(typeof(Block).FullName, typeof(T).FullName);
            }

        }

        public Coin.Block ToObject()
        {
            Coin.Block obj = new();

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

            if (transactions != null)
            {
                obj.transactions = new Coin.FullTransaction[transactions.Count];

                for (int i = 0; i < transactions.Count; i++)
                {
                    obj.transactions[i] = new FullTransaction(transactions[i]).ToObject();
                }
            }
            else if (Transactions != null)
            {
                obj.Transactions = new Cipher.SHA256[Transactions.Count];

                for (int i = 0; i < Transactions.Count; i++)
                {
                    obj.Transactions[i] = new Cipher.SHA256(Printable.Byteify(Transactions[i]), false);
                }
            }

            return obj;
        }

        public static Coin.Block FromReadable(string json)
        {
            return new Block(json).ToObject();
        }

        public static string ToReadable(Coin.Block obj)
        {
            return new Block(obj).JSON();
        }
    }
}
