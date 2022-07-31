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

        public List<object> Transactions { get; set; }

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
                Transactions = new List<object>(obj.Transactions.Length);

                for (int i = 0; i < obj.Transactions.Length; i++)
                {
                    Transactions.Add(obj.Transactions[i].ToReadable());
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
                    if      (Transactions[i] is Transaction ptx)                obj.Transactions[i] = ((Coin.Transaction)ptx.ToObject()).ToFull();
                    else if (Transactions[i] is MixedTransaction mtx)           obj.Transactions[i] = ((Coin.MixedTransaction)mtx.ToObject()).ToFull();
                    else if (Transactions[i] is Transparent.Transaction ttx)    obj.Transactions[i] = ((Coin.Transparent.Transaction)ttx.ToObject()).ToFull();
                    else                                                        obj.Transactions[i] = (Coin.FullTransaction)((FullTransaction)Transactions[i]).ToObject();
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
