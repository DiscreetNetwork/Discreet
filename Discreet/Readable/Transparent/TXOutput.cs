using Discreet.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Discreet.Coin.Models;

namespace Discreet.Readable.Transparent
{
    public class TXOutput: IReadable
    {
        public string TransactionSrc { get; set; }
        public string Address { get; set; }
        public ulong Amount { get; set; }

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
            TXOutput tXOutput = JsonSerializer.Deserialize<TXOutput>(json);
            TransactionSrc = tXOutput.TransactionSrc;
            Address = tXOutput.Address;
            Amount = tXOutput.Amount;
        }

        public TXOutput(TTXOutput obj)
        {
            FromObject(obj);
        }

        public TXOutput(TTXOutput obj, bool tx)
        {
            if (tx)
                FromTXObject(obj);
            else
                FromObject(obj);
        }

        public TXOutput(string json)
        {
            FromJSON(json);
        }

        public TXOutput() { }

        public void FromObject<T>(object obj)
        {
            if (typeof(T) == typeof(TTXOutput))
            {
                FromObject((TTXOutput)obj);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(TXOutput).FullName);
            }
        }

        public void FromObject(TTXOutput obj)
        {
            if (obj.TransactionSrc.Bytes != null) TransactionSrc = obj.TransactionSrc.ToHex();
            if (obj.Address != null) Address = obj.Address.ToString();
            Amount = obj.Amount;
        }

        public T ToObject<T>()
        {
            if (typeof(T) == typeof(TTXOutput))
            {
                return (T)ToObject();
            }
            else
            {
                throw new ReadableException(typeof(TXOutput).FullName, typeof(T).FullName);
            }
        }

        public object ToObject()
        {
            TTXOutput obj = new();

            if (TransactionSrc != null && TransactionSrc != "") obj.TransactionSrc = Cipher.SHA256.FromHex(TransactionSrc);
            if (Address != null && Address != "") obj.Address = new Coin.TAddress(Address);
            obj.Amount = Amount;

            return obj;
        }

        public void FromTXObject(TTXOutput obj)
        {
            if (obj.Address != null) Address = obj.Address.ToString();
            Amount = obj.Amount;
        }

        public static TTXOutput FromReadable(string json)
        {
            return (TTXOutput)new TXOutput(json).ToObject();
        }

        public static string ToReadable(TTXOutput obj)
        {
            return new TXOutput(obj).JSON();
        }

        public static string ToTXReadable(TTXOutput obj)
        {
            return new TXOutput(obj, true).JSON();
        }
    }
}
