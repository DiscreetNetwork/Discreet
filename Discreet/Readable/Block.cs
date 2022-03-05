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
        public BlockHeader Header { get; set; }

        /* this is fine since we disallow both serialization of Transactions and transactions; mutually exclusive */
        public List<FullTransaction> Transactions { get; set; }

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
            Block b = JsonSerializer.Deserialize<Block>(json);

            Header = b.Header;
            Transactions = b.Transactions;
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

        public virtual void FromObject<T>(object obj)
        {
            if (typeof(T) == typeof(Coin.Block))
            {
                FromObject((Coin.Block)obj);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(Block).FullName);
            }
        }

        public virtual void FromObject(Coin.Block obj)
        {
            Header = new BlockHeader(obj.Header);

            if (obj.Transactions != null)
            {
                Transactions = new List<FullTransaction>(obj.Transactions.Length);

                for (int i = 0; i < obj.Transactions.Length; i++)
                {
                    Transactions.Add(new FullTransaction(obj.Transactions[i]));
                }
            }
        }

        public virtual T ToObject<T>()
        {
            if (typeof(T) == typeof(Coin.Block))
            {
                return (T)ToObject();
            }
            else
            {
                throw new ReadableException(typeof(Block).FullName, typeof(T).FullName);
            }
        }

        public virtual object ToObject()
        {
            Coin.Block obj = new();

            obj.Header = (Coin.BlockHeader)Header.ToObject();

            if (Transactions != null)
            {
                obj.Transactions = new Coin.FullTransaction[Transactions.Count];

                for (int i = 0; i < Transactions.Count; i++)
                {
                    obj.Transactions[i] = (Coin.FullTransaction)Transactions[i].ToObject();
                }
            }

            return obj;
        }

        public static Coin.Block FromReadable(string json)
        {
            return (Coin.Block)new Block(json).ToObject();
        }

        public static string ToReadable(Coin.Block obj)
        {
            return new Block(obj).JSON();
        }
    }
}
