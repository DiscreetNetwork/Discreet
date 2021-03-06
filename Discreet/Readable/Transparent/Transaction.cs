using Discreet.Common;
using Discreet.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Discreet.Readable.Transparent
{
    public class Transaction: IReadable
    {
        public byte Version { get; set; }
        public byte NumInputs { get; set; }
        public byte NumOutputs { get; set; }
        public byte NumSigs { get; set; }

        public ulong Fee { get; set; }

        public string InnerHash { get; set; }

        public List<string> Inputs { get; set; }
        public List<TXOutput> Outputs { get; set; }
        public List<string> Signatures { get; set; }

        public string TxID { get; set; }

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
            Transaction transaction = JsonSerializer.Deserialize<Transaction>(json);
            Version = transaction.Version;
            NumInputs = transaction.NumInputs;
            NumOutputs = transaction.NumOutputs;
            NumSigs = transaction.NumSigs;
            InnerHash = transaction.InnerHash;
            Inputs = transaction.Inputs;
            Outputs = transaction.Outputs;
            Signatures = transaction.Signatures;
            Fee = transaction.Fee;
            TxID = transaction.TxID;
        }

        public Transaction(Coin.Transparent.Transaction obj)
        {
            FromObject(obj);
        }

        public Transaction(string json)
        {
            FromJSON(json);
        }

        public Transaction() { }

        public void FromObject<T>(object obj)
        {
            if (typeof(T) == typeof(Coin.Transparent.Transaction))
            {
                FromObject((Coin.Transparent.Transaction)obj);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(Transaction).FullName);
            }
        }

        public void FromObject(Coin.Transparent.Transaction obj)
        {
            Version = obj.Version;
            NumInputs = obj.NumInputs;
            NumOutputs = obj.NumOutputs;
            NumSigs = obj.NumSigs;

            if (obj.InnerHash.Bytes != null) InnerHash = obj.InnerHash.ToHex();
            if (obj.Inputs != null)
            {
                Inputs = new List<string>(obj.Inputs.Length);

                for (int i = 0; i < obj.Inputs.Length; i++)
                {
                    Inputs.Add(Printable.Hexify(obj.Inputs[i].Serialize()));
                }
            }
            if (obj.Outputs != null)
            {
                Outputs = new List<TXOutput>(obj.Outputs.Length);

                for (int i = 0; i < obj.Outputs.Length; i++)
                {
                    Outputs.Add(new TXOutput(obj.Outputs[i], true));
                }
            }
            if (obj.Signatures != null)
            {
                Signatures = new List<string>(obj.Signatures.Length);

                for (int i = 0; i < obj.Signatures.Length; i++)
                {
                    Signatures.Add(Printable.Hexify(obj.Signatures[i].ToBytes()));
                }
            }

            if (obj.TxID.Bytes != null) { TxID = obj.TxID.ToHex(); } else TxID = obj.Hash().ToHex();

            Fee = obj.Fee;
        }

        public T ToObject<T>()
        {
            if (typeof(T) == typeof(Coin.Transparent.Transaction))
            {
                return (T)ToObject();
            }
            else
            {
                throw new ReadableException(typeof(Transaction).FullName, typeof(T).FullName);
            }
        }

        public object ToObject()
        {
            Coin.Transparent.Transaction obj = new();

            obj.Version = Version;
            obj.NumInputs = NumInputs;
            obj.NumOutputs = NumOutputs;
            obj.NumSigs = NumSigs;

            if (InnerHash != null && InnerHash != "") obj.InnerHash = Cipher.SHA256.FromHex(InnerHash);
            if (Inputs != null)
            {
                obj.Inputs = new Coin.Transparent.TXInput[Inputs.Count];

                for (int i = 0; i < Inputs.Count; i++)
                {
                    obj.Inputs[i] = new Coin.Transparent.TXInput();
                    obj.Inputs[i].Deserialize(Printable.Byteify(Inputs[i]));
                }
            }
            if (Outputs != null)
            {
                obj.Outputs = new Coin.Transparent.TXOutput[Outputs.Count];

                for (int i = 0; i < Outputs.Count; i++)
                {
                    obj.Outputs[i] = (Coin.Transparent.TXOutput)Outputs[i].ToObject();
                }
            }
            if (Signatures != null)
            {
                obj.Signatures = new Cipher.Signature[Signatures.Count];

                for (int i = 0; i < Signatures.Count; i++)
                {
                    obj.Signatures[i] = new Cipher.Signature(Printable.Byteify(Signatures[i]));
                }
            }

            obj.Fee = Fee;

            return obj;
        }

        public static Coin.Transparent.Transaction FromReadable(string json)
        {
            return (Coin.Transparent.Transaction)new Transaction(json).ToObject();
        }

        public static string ToReadable(Coin.Transparent.Transaction obj)
        {
            return new Transaction(obj).JSON();
        }

        public FullTransaction ToFull()
        {
            return new FullTransaction(this);
        }
    }
}
