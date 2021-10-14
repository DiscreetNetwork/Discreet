﻿using Discreet.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace Discreet.Readable.Transparent
{
    public class TXOutput
    {
        public string TransactionSrc;
        public string Address;
        public ulong Amount;

        public string JSON()
        {
            return JsonSerializer.Serialize(this);
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

        public TXOutput(Coin.Transparent.TXOutput obj)
        {
            FromObject(obj);
        }

        public TXOutput(string json)
        {
            FromJSON(json);
        }

        public void FromObject<T>(T obj)
        {
            if (typeof(T) == typeof(Coin.Transparent.TXOutput))
            {
                dynamic t = obj;
                FromObject((Coin.Transparent.TXOutput)t);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(TXOutput).FullName);
            }
        }

        public void FromObject(Coin.Transparent.TXOutput obj)
        {
            if (obj.TransactionSrc.Bytes != null) TransactionSrc = obj.TransactionSrc.ToHex();
            if (obj.Address != null) Address = obj.Address.ToString();
            Amount = obj.Amount;
        }

        public T ToObject<T>()
        {
            if (typeof(T) == typeof(Coin.Transparent.TXOutput))
            {
                dynamic t = ToObject();
                return (T)t;
            }
            else
            {
                throw new ReadableException(typeof(TXOutput).FullName, typeof(T).FullName);
            }

        }

        public Coin.Transparent.TXOutput ToObject()
        {
            Coin.Transparent.TXOutput obj = new();

            if (TransactionSrc != null && TransactionSrc != "") obj.TransactionSrc = Cipher.SHA256.FromHex(TransactionSrc);
            if (Address != null && Address != "") obj.Address = new Coin.TAddress(Address);
            obj.Amount = Amount;

            return obj;
        }

        public static Coin.Transparent.TXOutput FromReadable(string json)
        {
            return new TXOutput(json).ToObject();
        }

        public static string ToReadable(Coin.Transparent.TXOutput obj)
        {
            return new TXOutput(obj).JSON();
        }
    }
}
