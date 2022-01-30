using Discreet.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace Discreet.Readable
{
    public class TXOutput: IReadable
    {
        public string TransactionSrc { get; set; }
        public string UXKey { get; set; }
        public string Commitment { get; set; }
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
            UXKey = tXOutput.UXKey;
            Commitment = tXOutput.Commitment;
            Amount = tXOutput.Amount;
        }

        public TXOutput(Coin.TXOutput obj)
        {
            FromObject(obj);
        }

        public TXOutput(Coin.TXOutput obj, bool tx)
        {
            if (tx)
                FromTXObject(obj);
            else
                FromObject(obj);
        }

        public TXOutput() { }

        public TXOutput(string json)
        {
            FromJSON(json);
        }

        public void FromObject<T>(object obj)
        {
            if (typeof(T) == typeof(Coin.TXOutput))
            {
                FromObject((Coin.TXOutput)obj);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(TXOutput).FullName);
            }
        }

        public void FromObject(Coin.TXOutput obj)
        {
            if (obj.TransactionSrc.Bytes != null) TransactionSrc = obj.TransactionSrc.ToHex();
            if (obj.UXKey.bytes != null) UXKey = obj.UXKey.ToHex();
            if (obj.Commitment.bytes != null) Commitment = obj.UXKey.ToHex();
            Amount = obj.Amount;
        }

        public T ToObject<T>()
        {
            if (typeof(T) == typeof(Coin.TXOutput))
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
            Coin.TXOutput obj = new();

            if (TransactionSrc != null && TransactionSrc != "") obj.TransactionSrc = Cipher.SHA256.FromHex(TransactionSrc);
            if (UXKey != null && UXKey != "") obj.UXKey = Cipher.Key.FromHex(UXKey);
            if (Commitment != null && Commitment != "") obj.Commitment = Cipher.Key.FromHex(Commitment);
            obj.Amount = Amount;

            return obj;
        }

        public void FromTXObject(Coin.TXOutput obj)
        {
            if (obj.UXKey.bytes != null) UXKey = obj.UXKey.ToHex();
            if (obj.Commitment.bytes != null) Commitment = obj.UXKey.ToHex();
            Amount = obj.Amount;
        }

        public static Coin.TXOutput FromReadable(string json)
        {
            return (Coin.TXOutput)new TXOutput(json).ToObject();
        }

        public static string ToReadable(Coin.TXOutput obj)
        {
            return new TXOutput(obj).JSON();
        }

        internal static string ToTXReadable(Coin.TXOutput obj)
        {
            return new TXOutput(obj, true).JSON();
        }
    }
}
