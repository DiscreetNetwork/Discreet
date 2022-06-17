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
    public class FullTransaction: IReadable
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

        public List<Transparent.TXOutput> TInputs { get; set; }
        public List<Transparent.TXOutput> TOutputs { get; set; }
        public List<string> TSignatures { get; set; }

        public string TransactionKey { get; set; }
        public List<TXInput> PInputs { get; set; }
        public List<TXOutput> POutputs { get; set; }
        public Bulletproof RangeProof { get; set; }
        public BulletproofPlus RangeProofPlus { get; set; }
        public List<Triptych> PSignatures { get; set; }
        public List<string> PseudoOutputs { get; set; }

        public string TxID { get; set; }

        public string JSON()
        {
            return Version switch
            {
                0 or 1 or 2 => ToPrivate().JSON(),
                3 => ToTransparent().JSON(),
                4 => ToMixed().JSON(),
                _ => JsonSerializer.Serialize(this, ReadableOptions.Options),
            };
        }

        public override string ToString()
        {
            return JSON();
        }

        public MixedTransaction ToMixed()
        {
            MixedTransaction transaction = new MixedTransaction();

            transaction.Version = Version;
            transaction.NumInputs = NumInputs;
            transaction.NumOutputs = NumOutputs;
            transaction.NumSigs = NumSigs;

            transaction.NumTInputs = NumTInputs;
            transaction.NumPInputs = NumPInputs;
            transaction.NumTOutputs = NumTOutputs;
            transaction.NumPOutputs = NumPOutputs;

            transaction.SigningHash = SigningHash;

            transaction.Fee = Fee;

            transaction.TInputs = TInputs;
            transaction.TOutputs = TOutputs;
            transaction.TSignatures = TSignatures;

            transaction.TransactionKey = TransactionKey;
            transaction.PInputs = PInputs;
            transaction.POutputs = POutputs;
            transaction.RangeProofPlus = RangeProofPlus;
            transaction.PSignatures = PSignatures;
            transaction.PseudoOutputs = PseudoOutputs;

            transaction.TxID = TxID;

            return transaction;
        }

        public Transparent.Transaction ToTransparent()
        {
            Transparent.Transaction transaction = new Transparent.Transaction();

            transaction.Version = Version;
            transaction.NumInputs = NumInputs;
            transaction.NumOutputs = NumOutputs;
            transaction.NumSigs = NumSigs;

            transaction.InnerHash = SigningHash;

            transaction.Fee = Fee;

            transaction.Inputs = TInputs;
            transaction.Outputs = TOutputs;
            transaction.Signatures = TSignatures;

            transaction.TxID = TxID;

            return transaction;
        }

        public Transaction ToPrivate()
        {
            Transaction transaction = new Transaction();

            transaction.Version = Version;
            transaction.NumInputs = NumInputs;
            transaction.NumOutputs = NumOutputs;
            transaction.NumSigs = NumSigs;

            transaction.Fee = Fee;

            transaction.TransactionKey = TransactionKey;
            transaction.Inputs = PInputs;
            transaction.Outputs = POutputs;
            transaction.RangeProofPlus = RangeProofPlus;
            transaction.RangeProof = RangeProof;
            transaction.Signatures = PSignatures;
            transaction.PseudoOutputs = PseudoOutputs;

            transaction.TxID = TxID;

            return transaction;
        }

        public void FromJSON(string json)
        {
            FullTransaction transaction = JsonSerializer.Deserialize<FullTransaction>(json);
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
            PSignatures = transaction.PSignatures;
            RangeProof = transaction.RangeProof;
            RangeProofPlus = transaction.RangeProofPlus;
            PseudoOutputs = transaction.PseudoOutputs;

            TxID = transaction.TxID;
        }

        public FullTransaction(Coin.FullTransaction obj)
        {
            FromObject(obj);
        }

        public FullTransaction(Transaction tx)
        {
            Version = tx.Version;
            NumInputs = tx.NumInputs;
            NumOutputs = tx.NumOutputs;
            NumPInputs = tx.NumInputs;
            NumPOutputs = tx.NumOutputs;
            NumTInputs = 0;
            NumTOutputs = 0;
            NumSigs = tx.NumSigs;
            Fee = tx.Fee;
            TransactionKey = tx.TransactionKey;
            PInputs = tx.Inputs;
            POutputs = tx.Outputs;
            PSignatures = tx.Signatures;
            RangeProof = tx.RangeProof;
            RangeProofPlus = tx.RangeProofPlus;
            PseudoOutputs = tx.PseudoOutputs;

            TxID = tx.TxID;
        }

        public FullTransaction(MixedTransaction tx)
        {
            Version = tx.Version;
            NumInputs = tx.NumInputs;
            NumOutputs = tx.NumOutputs;
            NumPInputs = tx.NumPInputs;
            NumPOutputs = tx.NumPOutputs;
            NumTInputs = tx.NumTInputs;
            NumTOutputs = tx.NumTOutputs;
            NumSigs = tx.NumSigs;
            Fee = tx.Fee;
            TransactionKey = tx.TransactionKey;
            PInputs = tx.PInputs;
            POutputs = tx.POutputs;
            PSignatures = tx.PSignatures;
            RangeProofPlus = tx.RangeProofPlus;
            PseudoOutputs = tx.PseudoOutputs;
            TInputs = tx.TInputs;
            TOutputs = tx.TOutputs;
            TSignatures = tx.TSignatures;
            SigningHash = tx.SigningHash;

            TxID = tx.TxID;
        }

        public FullTransaction(Transparent.Transaction tx)
        {
            Version = tx.Version;
            NumInputs = tx.NumInputs;
            NumOutputs = tx.NumOutputs;
            NumSigs = tx.NumSigs;
            NumTInputs = tx.NumInputs;
            NumTOutputs = tx.NumOutputs;
            NumPInputs = 0;
            NumPOutputs = 0;
            Fee = tx.Fee;
            SigningHash = tx.InnerHash;
            Fee = tx.Fee;
            TInputs = tx.Inputs;
            TOutputs = tx.Outputs;
            TSignatures = tx.Signatures;

            TxID = tx.TxID;
        }

        public FullTransaction(string json)
        {
            FromJSON(json);
        }

        public FullTransaction() { }

        public void FromObject<T>(object obj)
        {
            if (typeof(T) == typeof(Coin.FullTransaction))
            {
                FromObject((Coin.FullTransaction)obj);
            }
            else
            {
                throw new ReadableException(typeof(T).FullName, typeof(FullTransaction).FullName);
            }
        }

        public void FromObject(Coin.FullTransaction obj)
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
            if (obj.RangeProof != null) RangeProof = new Bulletproof(obj.RangeProof);
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
            if (typeof(T) == typeof(Coin.FullTransaction))
            {
                return (T)ToObject();
            }
            else
            {
                throw new ReadableException(typeof(FullTransaction).FullName, typeof(T).FullName);
            }
        }

        public object ToObject()
        {
            Coin.FullTransaction obj = new();

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
                obj.TInputs = new Coin.Transparent.TXOutput[TInputs.Count];

                for (int i = 0; i < TInputs.Count; i++)
                {
                    obj.TInputs[i] = (Coin.Transparent.TXOutput)TInputs[i].ToObject();
                }
            }
            if (TOutputs != null)
            {
                obj.TOutputs = new Coin.Transparent.TXOutput[TOutputs.Count];

                for (int i = 0; i < TOutputs.Count; i++)
                {
                    obj.TOutputs[i] = (Coin.Transparent.TXOutput)TOutputs[i].ToObject();
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
                    obj.PInputs[i] = (Coin.TXInput)PInputs[i].ToObject();
                }
            }
            if (POutputs != null)
            {
                obj.POutputs = new Coin.TXOutput[POutputs.Count];

                for (int i = 0; i < POutputs.Count; i++)
                {
                    obj.POutputs[i] = (Coin.TXOutput)POutputs[i].ToObject();
                }
            }
            if (RangeProof != null) obj.RangeProof = (Coin.Bulletproof)RangeProof.ToObject();
            if (RangeProofPlus != null) obj.RangeProofPlus = (Coin.BulletproofPlus)RangeProofPlus.ToObject();
            if (PSignatures != null)
            {
                obj.PSignatures = new Coin.Triptych[PSignatures.Count];

                for (int i = 0; i < PSignatures.Count; i++)
                {
                    obj.PSignatures[i] = (Coin.Triptych)PSignatures[i].ToObject();
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

        public static Coin.FullTransaction FromReadable(string json)
        {
            return (Coin.FullTransaction)new FullTransaction(json).ToObject();
        }

        public static string ToReadable(Coin.FullTransaction obj)
        {
            return new FullTransaction(obj).JSON();
        }
    }
}
