using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Discreet.Common.Exceptions;
using Discreet.Common;
using Discreet.RPC.Common;

namespace Discreet.Readable
{
    public class Transaction : IReadable
    {
        public byte Version { get; set; }
        public byte NumInputs { get; set; }
        public byte NumOutputs { get; set; }
        public byte NumSigs { get; set; }

        public ulong Fee { get; set; }

        public string TransactionKey { get; set; }

        public List<TXInput> Inputs { get; set; }
        public List<TXOutput> Outputs { get; set; }

        public Bulletproof RangeProof { get; set; }
        public BulletproofPlus RangeProofPlus { get; set; }



        public List<Triptych> Signatures { get; set; }

        public List<string> PseudoOutputs { get; set; }

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

            Inputs = transaction.Inputs;
            Outputs = transaction.Outputs;

            RangeProof = transaction.RangeProof;
            RangeProofPlus = transaction.RangeProofPlus;

            Fee = transaction.Fee;

            Signatures = transaction.Signatures;

            PseudoOutputs = transaction.PseudoOutputs;

            TransactionKey = transaction.TransactionKey;
        }

        public Transaction(Coin.Transaction obj)
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
            if (typeof(T) == typeof(Coin.Transaction))
            {
                FromObject((Coin.Transaction)obj);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(Transaction).FullName);
            }
        }

        public void FromObject(Coin.Transaction obj)
        {
            Version = obj.Version;
            NumInputs = obj.NumInputs;
            NumOutputs = obj.NumOutputs;
            NumSigs = obj.NumSigs;

            if (obj.Inputs != null)
            {
                Inputs = new List<TXInput>(obj.Inputs.Length);

                for (int i = 0; i < obj.Inputs.Length; i++)
                {
                    Inputs.Add(new TXInput(obj.Inputs[i]));
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

            if (obj.RangeProof != null) RangeProof = new Bulletproof(obj.RangeProof);
            if (obj.RangeProofPlus != null) RangeProofPlus = new BulletproofPlus(obj.RangeProofPlus);

            Fee = obj.Fee;

            if (obj.Signatures != null)
            {
                Signatures = new List<Triptych>(obj.Signatures.Length);

                for (int i = 0; i < obj.Signatures.Length; i++)
                {
                    Signatures.Add(new Triptych(obj.Signatures[i]));
                }
            }

            if (obj.PseudoOutputs != null)
            {
                PseudoOutputs = new List<string>(obj.PseudoOutputs.Length);

                for (int i = 0; i < obj.PseudoOutputs.Length; i++)
                {
                    PseudoOutputs.Add(obj.PseudoOutputs[i].ToHex());
                }
            }

            if (obj.TransactionKey.bytes != null)
            {
                TransactionKey = obj.TransactionKey.ToHex();
            }
        }

        public T ToObject<T>()
        {
            if (typeof(T) == typeof(Coin.Transaction))
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
            Coin.Transaction obj = new();

            obj.Version = Version;
            obj.NumInputs = NumInputs;
            obj.NumOutputs = NumOutputs;
            obj.NumSigs = NumSigs;

            if (Inputs != null)
            {
                obj.Inputs = new Coin.TXInput[Inputs.Count];

                for (int i = 0; i < Inputs.Count; i++)
                {
                    obj.Inputs[i] = (Coin.TXInput)Inputs[i].ToObject();
                }
            }
            if (Outputs != null)
            {
                obj.Outputs = new Coin.TXOutput[Outputs.Count];

                for (int i = 0; i < Outputs.Count; i++)
                {
                    obj.Outputs[i] = (Coin.TXOutput)Outputs[i].ToObject();
                }
            }

            if (RangeProof != null) obj.RangeProof = (Coin.Bulletproof)RangeProof.ToObject();
            if (RangeProofPlus != null) obj.RangeProofPlus = (Coin.BulletproofPlus)RangeProofPlus.ToObject();

            obj.Fee = Fee;

            if (Signatures != null)
            {
                obj.Signatures = new Coin.Triptych[Signatures.Count];

                for (int i = 0; i < Signatures.Count; i++)
                {
                    obj.Signatures[i] = (Coin.Triptych)Signatures[i].ToObject();
                }
            }

            if (PseudoOutputs != null)
            {
                obj.PseudoOutputs = new Cipher.Key[PseudoOutputs.Count];

                for (int i = 0; i < PseudoOutputs.Count; i++)
                {
                    obj.PseudoOutputs[i] = new Cipher.Key(Printable.Byteify(PseudoOutputs[i]));
                }
            }

            if (TransactionKey != null && TransactionKey != "") obj.TransactionKey = new Cipher.Key(Printable.Byteify(TransactionKey));

            return obj;
        }

        public static Coin.Transaction FromReadable(string json)
        {
            return (Coin.Transaction)new Transaction(json).ToObject();
        }

        public static string ToReadable(Coin.Transaction obj)
        {
            return new Transaction(obj).JSON();
        }

        public FullTransaction ToFull()
        {
            return new FullTransaction(this);
        }
    }
}
