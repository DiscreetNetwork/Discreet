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
    public class Transaction
    {
        public byte Version;
        public byte NumInputs;
        public byte NumOutputs;
        public byte NumSigs;

        public string InnerHash;

        public List<string> Inputs;
        public List<string> Outputs;
        public List<string> Signatures;

        public ulong Fee;

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
        }

        public Transaction(Coin.Transparent.Transaction obj)
        {
            FromObject(obj);
        }

        public Transaction(string json)
        {
            FromJSON(json);
        }

        public void FromObject<T>(T obj)
        {
            if (typeof(T) == typeof(Coin.Transparent.Transaction))
            {
                dynamic t = obj;
                FromObject((Coin.Transparent.Transaction)t);
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
                    Inputs.Add(TXOutput.ToReadable(obj.Inputs[i]));
                }
            }
            if (obj.Outputs != null)
            {
                Outputs = new List<string>(obj.Outputs.Length);

                for (int i = 0; i < obj.Outputs.Length; i++)
                {
                    Outputs.Add(TXOutput.ToReadable(obj.Outputs[i]));
                }
            }
            if (obj.Signatures != null)
            {
                Signatures = new List<string>(obj.Signatures.Length);

                for (int i = 0; i < obj.Signatures.Length; i++)
                {
                    Outputs.Add(Printable.Hexify(obj.Signatures[i].ToBytes()));
                }
            }

            Fee = obj.Fee;
        }

        public T ToObject<T>()
        {
            if (typeof(T) == typeof(Coin.Transparent.Transaction))
            {
                dynamic t = ToObject();
                return (T)t;
            }
            else
            {
                throw new ReadableException(typeof(Transaction).FullName, typeof(T).FullName);
            }

        }

        public Coin.Transparent.Transaction ToObject()
        {
            Coin.Transparent.Transaction obj = new();

            obj.Version = Version;
            obj.NumInputs = NumInputs;
            obj.NumOutputs = NumOutputs;
            obj.NumSigs = NumSigs;

            if (InnerHash != null && InnerHash != "") obj.InnerHash = Cipher.SHA256.FromHex(InnerHash);
            if (Inputs != null)
            {
                obj.Inputs = new Coin.Transparent.TXOutput[Inputs.Count];

                for (int i = 0; i < Inputs.Count; i++)
                {
                    obj.Inputs[i] = TXOutput.FromReadable(Inputs[i]);
                }
            }
            if (Outputs != null)
            {
                obj.Outputs = new Coin.Transparent.TXOutput[Outputs.Count];

                for (int i = 0; i < Outputs.Count; i++)
                {
                    obj.Outputs[i] = TXOutput.FromReadable(Outputs[i]);
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
            return new Transaction(json).ToObject();
        }

        public static string ToReadable(Coin.Transparent.Transaction obj)
        {
            return new Transaction(obj).JSON();
        }
    }
}
