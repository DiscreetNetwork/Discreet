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

        public TXOutput(Coin.Models.TXOutput obj)
        {
            FromObject(obj);
        }

        public TXOutput(Coin.Models.TXOutput obj, bool tx)
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
            if (typeof(T) == typeof(Coin.Models.TXOutput))
            {
                FromObject((Coin.Models.TXOutput)obj);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(TXOutput).FullName);
            }
        }

        public void FromObject(Coin.Models.TXOutput obj)
        {
            if (obj.TransactionSrc.Bytes != null) TransactionSrc = obj.TransactionSrc.ToHex();
            if (obj.UXKey.bytes != null) UXKey = obj.UXKey.ToHex();
            if (obj.Commitment.bytes != null) Commitment = obj.Commitment.ToHex();
            Amount = obj.Amount;
        }

        public T ToObject<T>()
        {
            if (typeof(T) == typeof(Coin.Models.TXOutput))
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
            Coin.Models.TXOutput obj = new();

            if (TransactionSrc != null && TransactionSrc != "") obj.TransactionSrc = Cipher.SHA256.FromHex(TransactionSrc);
            if (UXKey != null && UXKey != "") obj.UXKey = Cipher.Key.FromHex(UXKey);
            if (Commitment != null && Commitment != "") obj.Commitment = Cipher.Key.FromHex(Commitment);
            obj.Amount = Amount;

            return obj;
        }

        public void FromTXObject(Coin.Models.TXOutput obj)
        {
            if (obj.UXKey.bytes != null) UXKey = obj.UXKey.ToHex();
            if (obj.Commitment.bytes != null) Commitment = obj.Commitment.ToHex();
            Amount = obj.Amount;
        }

        public static Coin.Models.TXOutput FromReadable(string json)
        {
            return (Coin.Models.TXOutput)new TXOutput(json).ToObject();
        }

        public static string ToReadable(Coin.Models.TXOutput obj)
        {
            return new TXOutput(obj).JSON();
        }

        internal static string ToTXReadable(Coin.Models.TXOutput obj)
        {
            return new TXOutput(obj, true).JSON();
        }
    }
}
