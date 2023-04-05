using Discreet.Coin.Models;
using Discreet.Common;
using Discreet.Common.Exceptions;
using Discreet.Common.Serialize;
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
        public byte NumTOutputs { get; set; }
        public byte NumPOutputs { get; set; }

        public ulong Fee { get; set; }

        public string SigningHash { get; set; }

        public List<string> TInputs { get; set; }
        public List<Transparent.TXOutput> TOutputs { get; set; }
        public List<string> TSignatures { get; set; }

        public string TransactionKey { get; set; }
        public List<TXInput> PInputs { get; set; }
        public List<TXOutput> POutputs { get; set; }
        public BulletproofPlus RangeProofPlus { get; set; }
        public List<Triptych> PSignatures { get; set; }
        public List<string> PseudoOutputs { get; set; }

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
            MixedTransaction transaction = JsonSerializer.Deserialize<MixedTransaction>(json);
            Version = transaction.Version;
            NumInputs = transaction.NumInputs;
            NumOutputs = transaction.NumOutputs;
            NumSigs = transaction.NumSigs;

            NumTInputs = transaction.NumTInputs;
            NumPInputs = transaction.NumPInputs;
            NumTOutputs = transaction.NumTOutputs;
            NumPOutputs = transaction.NumPOutputs;

            SigningHash = transaction.SigningHash;

            Fee = transaction.Fee;

            TInputs = transaction.TInputs;
            TOutputs = transaction.TOutputs;
            TSignatures = transaction.TSignatures;

            TransactionKey = transaction.TransactionKey;
            PInputs = transaction.PInputs;
            POutputs = transaction.POutputs;
            RangeProofPlus = transaction.RangeProofPlus;
            PSignatures = transaction.PSignatures;
            PseudoOutputs = transaction.PseudoOutputs;

            TxID = transaction.TxID;
        }

        public MixedTransaction(Coin.Models.MixedTransaction obj)
        {
            FromObject(obj);
        }
        public MixedTransaction(string json)
        {
            FromJSON(json);
        }

        public MixedTransaction() { }

        public void FromObject<T>(object obj)
        {
            if (typeof(T) == typeof(Coin.Models.MixedTransaction))
            {
                FromObject((Coin.Models.MixedTransaction)obj);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(MixedTransaction).FullName);
            }
        }

        public void FromObject(Coin.Models.MixedTransaction obj)
        {
            Version = obj.Version;
            NumInputs = obj.NumInputs;
            NumOutputs = obj.NumOutputs;
            NumSigs = obj.NumSigs;

            NumTInputs = obj.NumTInputs;
            NumPInputs = obj.NumPInputs;
            NumTOutputs = obj.NumTOutputs;
            NumPOutputs = obj.NumPOutputs;

            if (obj.SigningHash.Bytes != null) SigningHash = obj.SigningHash.ToHex();

            Fee = obj.Fee;

            if (obj.TInputs != null)
            {
                TInputs = new List<string>(obj.TInputs.Length);

                for (int i = 0; i < obj.TInputs.Length; i++)
                {
                    TInputs.Add(Printable.Hexify(obj.TInputs[i].Serialize()));
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

            if (obj.TxID.Bytes != null) { TxID = obj.TxID.ToHex(); } else TxID = obj.Hash().ToHex();
        }

        public T ToObject<T>()
        {
            if (typeof(T) == typeof(Coin.Models.MixedTransaction))
            {
                return (T)ToObject();
            }
            else
            {
                throw new ReadableException(typeof(MixedTransaction).FullName, typeof(T).FullName);
            }
        }

        public object ToObject()
        {
            Coin.Models.MixedTransaction obj = new();

            obj.Version = Version;
            obj.NumInputs = NumInputs;
            obj.NumOutputs = NumOutputs;
            obj.NumSigs = NumSigs;

            obj.NumTInputs = NumTInputs;
            obj.NumPInputs = NumPInputs;
            obj.NumTOutputs = NumTOutputs;
            obj.NumPOutputs = NumPOutputs;

            if (SigningHash != null && SigningHash != "") obj.SigningHash = new Cipher.SHA256(Printable.Byteify(SigningHash), false);
            obj.Fee = Fee;

            if (TInputs != null)
            {
                obj.TInputs = new TTXInput[TInputs.Count];

                for (int i = 0; i < TInputs.Count; i++)
                {
                    obj.TInputs[i] = new TTXInput();
                    obj.Deserialize(Printable.Byteify(TInputs[i]));
                }
            }
            if (TOutputs != null)
            {
                obj.TOutputs = new TTXOutput[TOutputs.Count];

                for (int i = 0; i < TOutputs.Count; i++)
                {
                    obj.TOutputs[i] = (TTXOutput)TOutputs[i].ToObject();
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
                obj.PInputs = new Coin.Models.TXInput[PInputs.Count];

                for (int i = 0; i < PInputs.Count; i++)
                {
                    obj.PInputs[i] = (Coin.Models.TXInput)PInputs[i].ToObject();
                }
            }
            if (POutputs != null)
            {
                obj.POutputs = new Coin.Models.TXOutput[POutputs.Count];

                for (int i = 0; i < POutputs.Count; i++)
                {
                    obj.POutputs[i] = (Coin.Models.TXOutput)POutputs[i].ToObject();
                }
            }
            if (RangeProofPlus != null) obj.RangeProofPlus = (Coin.Models.BulletproofPlus)RangeProofPlus.ToObject();
            if (PSignatures != null)
            {
                obj.PSignatures = new Coin.Models.Triptych[PSignatures.Count];

                for (int i = 0; i < PSignatures.Count; i++)
                {
                    obj.PSignatures[i] = (Coin.Models.Triptych)PSignatures[i].ToObject();
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

        public static Coin.Models.MixedTransaction FromReadable(string json)
        {
            return (Coin.Models.MixedTransaction)new MixedTransaction(json).ToObject();
        }

        public static string ToReadable(Coin.Models.MixedTransaction obj)
        {
            return new MixedTransaction(obj).JSON();
        }

        public FullTransaction ToFull()
        {
            return new FullTransaction(this);
        }
    }
}
