using Discreet.Common;
using Discreet.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Discreet.Readable
{
    public class MixedTransaction : IReadable
    {
        public byte Version { get; set; }
        public byte NumInputs { get; set; }
        public byte NumOutputs { get; set; }
        public byte NumSigs { get; set; }

        public byte NumTInputs { get; set; }
        public byte NumPInputs { get; set; }
        public byte NumTOuputs { get; set; }
        public byte NumPOutputs { get; set; }

        public ulong Fee { get; set; }

        public string SigningHash { get; set; }

        public List<Transparent.TXOutput> TInputs { get; set; }
        public List<Transparent.TXOutput> TOutputs { get; set; }
        public List<string> TSignatures { get; set; }

        public string TransactionKey { get; set; }
        public List<TXInput> PInputs { get; set; }
        public List<TXOutput> POutputs { get; set; }
        public BulletproofPlus RangeProofPlus { get; set; }
        public List<Triptych> PSignatures { get; set; }
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
            MixedTransaction transaction = JsonSerializer.Deserialize<MixedTransaction>(json);
            Version = transaction.Version;
            NumInputs = transaction.NumInputs;
            NumOutputs = transaction.NumOutputs;
            NumSigs = transaction.NumSigs;

            NumTInputs = transaction.NumTInputs;
            NumPInputs = transaction.NumPInputs;
            NumTOuputs = transaction.NumTOuputs;
            NumPOutputs = transaction.NumPOutputs;

            SigningHash = transaction.SigningHash;

            Fee = transaction.Fee;

            TInputs = transaction.TInputs;
            TOutputs = transaction.TOutputs;
            TSignatures = transaction.TSignatures;

            TransactionKey = transaction.TransactionKey;
            PInputs = transaction.PInputs;
            POutputs = transaction.POutputs;
            PSignatures = transaction.PSignatures;
            PseudoOutputs = transaction.PseudoOutputs;
        }

        public MixedTransaction(Coin.MixedTransaction obj)
        {
            FromObject(obj);
        }
        public MixedTransaction(string json)
        {
            FromJSON(json);
        }

        public MixedTransaction() { }

        public void FromObject<T>(T obj)
        {
            if (typeof(T) == typeof(Coin.MixedTransaction))
            {
                dynamic t = obj;
                FromObject((Coin.MixedTransaction)t);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(MixedTransaction).FullName);
            }
        }

        public void FromObject(Coin.MixedTransaction obj)
        {
            Version = obj.Version;
            NumInputs = obj.NumInputs;
            NumOutputs = obj.NumOutputs;
            NumSigs = obj.NumSigs;

            NumTInputs = obj.NumTInputs;
            NumPInputs = obj.NumPInputs;
            NumTOuputs = obj.NumTOutputs;
            NumPOutputs = obj.NumPOutputs;

            if (obj.SigningHash.Bytes != null) SigningHash = obj.SigningHash.ToHex();

            Fee = obj.Fee;

            if (obj.TInputs != null)
            {
                TInputs = new List<Transparent.TXOutput>(obj.TInputs.Length);

                for (int i = 0; i < obj.TInputs.Length; i++)
                {
                    TInputs.Add(new Transparent.TXOutput(obj.TInputs[i], false));
                }
            }
            if (obj.TOutputs != null)
            {
                TOutputs = new List<Transparent.TXOutput>(obj.TOutputs.Length);

                for (int i = 0; i < obj.TOutputs.Length; i++)
                {
                    TOutputs.Add(new Transparent.TXOutput(obj.TOutputs[i], true));
                }
            }
            if (obj.TSignatures != null)
            {
                TSignatures = new List<string>(obj.TSignatures.Length);

                for (int i = 0; i < obj.TSignatures.Length; i++)
                {
                    TSignatures.Add(Printable.Hexify(obj.TSignatures[i].ToBytes()));
                }
            }

            if (obj.TransactionKey.bytes != null) TransactionKey = obj.TransactionKey.ToHex();
            if (obj.PInputs != null)
            {
                PInputs = new List<TXInput>(obj.PInputs.Length);

                for (int i = 0; i < obj.PInputs.Length; i++)
                {
                    PInputs.Add(new TXInput(obj.PInputs[i]));
                }
            }
            if (obj.POutputs != null)
            {
                POutputs = new List<TXOutput>(obj.POutputs.Length);

                for (int i = 0; i < obj.POutputs.Length; i++)
                {
                    POutputs.Add(new TXOutput(obj.POutputs[i], true));
                }
            }
            if (obj.RangeProofPlus != null) RangeProofPlus = new BulletproofPlus(obj.RangeProofPlus);
            if (obj.PSignatures != null)
            {
                PSignatures = new List<Triptych>(obj.PSignatures.Length);

                for (int i = 0; i < obj.PSignatures.Length; i++)
                {
                    PSignatures.Add(new Triptych(obj.PSignatures[i]));
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
        }

        public T ToObject<T>()
        {
            if (typeof(T) == typeof(Coin.MixedTransaction))
            {
                dynamic t = ToObject();
                return (T)t;
            }
            else
            {
                throw new ReadableException(typeof(MixedTransaction).FullName, typeof(T).FullName);
            }

        }

        public Coin.MixedTransaction ToObject()
        {
            Coin.MixedTransaction obj = new();

            obj.Version = Version;
            obj.NumInputs = NumInputs;
            obj.NumOutputs = NumOutputs;
            obj.NumSigs = NumSigs;

            obj.NumTInputs = NumTInputs;
            obj.NumPInputs = NumPInputs;
            obj.NumTOutputs = NumTOuputs;
            obj.NumPOutputs = NumPOutputs;

            if (SigningHash != null && SigningHash != "") obj.SigningHash = new Cipher.SHA256(Printable.Byteify(SigningHash), false);
            obj.Fee = Fee;

            if (TInputs != null)
            {
                obj.TInputs = new Coin.Transparent.TXOutput[TInputs.Count];

                for (int i = 0; i < TInputs.Count; i++)
                {
                    obj.TInputs[i] = TInputs[i].ToObject();
                }
            }
            if (TOutputs != null)
            {
                obj.TOutputs = new Coin.Transparent.TXOutput[TOutputs.Count];

                for (int i = 0; i < TOutputs.Count; i++)
                {
                    obj.TOutputs[i] = TOutputs[i].ToObject();
                }
            }
            if (TSignatures != null)
            {
                obj.TSignatures = new Cipher.Signature[TSignatures.Count];

                for (int i = 0; i < TSignatures.Count; i++)
                {
                    obj.TSignatures[i] = new Cipher.Signature(Printable.Byteify(TSignatures[i]));
                }
            }

            if (TransactionKey != null && TransactionKey != "") obj.TransactionKey = new Cipher.Key(Printable.Byteify(TransactionKey));
            if (PInputs != null)
            {
                obj.PInputs = new Coin.TXInput[PInputs.Count];

                for (int i = 0; i < PInputs.Count; i++)
                {
                    obj.PInputs[i] = PInputs[i].ToObject();
                }
            }
            if (POutputs != null)
            {
                obj.POutputs = new Coin.TXOutput[POutputs.Count];

                for (int i = 0; i < POutputs.Count; i++)
                {
                    obj.POutputs[i] = POutputs[i].ToObject();
                }
            }
            if (RangeProofPlus != null) obj.RangeProofPlus = RangeProofPlus.ToObject();
            if (PSignatures != null)
            {
                obj.PSignatures = new Coin.Triptych[PSignatures.Count];

                for (int i = 0; i < PSignatures.Count; i++)
                {
                    obj.PSignatures[i] = PSignatures[i].ToObject();
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

            return obj;
        }

        public static Coin.MixedTransaction FromReadable(string json)
        {
            return new MixedTransaction(json).ToObject();
        }

        public static string ToReadable(Coin.MixedTransaction obj)
        {
            return new MixedTransaction(obj).JSON();
        }
    }
}
