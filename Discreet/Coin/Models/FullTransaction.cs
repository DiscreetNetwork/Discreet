﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Discreet.Cipher;
using Discreet.Common;
using Discreet.Common.Exceptions;
using Discreet.Common.Serialize;

namespace Discreet.Coin.Models
{
    public class FullTransaction : IHashable
    {
        public byte Version { get; set; }
        public byte NumInputs { get; set; }
        public byte NumOutputs { get; set; }
        public byte NumSigs { get; set; }

        public byte NumTInputs { get; set; }
        public byte NumPInputs { get; set; }
        public byte NumTOutputs { get; set; }
        public byte NumPOutputs { get; set; }

        public SHA256 SigningHash { get; set; }

        public ulong Fee { get; set; }

        /* Transparent part */
        public TTXInput[] TInputs { get; set; }
        public TTXOutput[] TOutputs { get; set; }
        public Signature[] TSignatures { get; set; }

        /* Private part */
        public Key TransactionKey { get; set; }
        public TXInput[] PInputs { get; set; }
        public TXOutput[] POutputs { get; set; }
        public Bulletproof RangeProof { get; set; }
        public BulletproofPlus RangeProofPlus { get; set; }
        public Triptych[] PSignatures { get; set; }
        public Key[] PseudoOutputs { get; set; }

        private SHA256 _txid;
        public SHA256 TxID { get { if (_txid == default || _txid.Bytes == null) _txid = Hash(); return _txid; } }

        public FullTransaction() { Version = byte.MaxValue; }

        public FullTransaction(byte[] bytes)
        {
            var reader = new MemoryReader(bytes.AsMemory());
            Deserialize(ref reader);
        }

        public FullTransaction(Transaction tx)
        {
            FromPrivate(tx);
        }

        public FullTransaction(TTransaction tx)
        {
            FromTransparent(tx);
        }

        public FullTransaction(MixedTransaction tx)
        {
            FromMixed(tx);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is FullTransaction tx)
            {
                return Cipher.Extensions.ByteArrayExtensions.BEquals(tx.Serialize(), this.Serialize());
            }

            return false;
        }

        public Transaction ToPrivate()
        {
            if (Version != 1 && Version != 2 && Version != 0) throw new Exception("FullTransaction.ToPrivate: version must match!");

            var tx = new Transaction
            {
                Version = Version,
                NumInputs = NumInputs,
                NumOutputs = NumOutputs,
                NumSigs = NumSigs,
                Fee = Fee,
                TransactionKey = TransactionKey,
                Inputs = PInputs,
                Outputs = POutputs,
                Signatures = PSignatures,
                RangeProof = RangeProof,
                RangeProofPlus = RangeProofPlus,
                PseudoOutputs = PseudoOutputs,
            };

            return tx;
        }

        public Transaction ToCoinbase() { if (Version != 0) throw new Exception("FullTransaction.ToCoinbase: version must match!"); return ToPrivate(); }

        public TTransaction ToTransparent()
        {
            if (Version != 3) throw new Exception("FullTransaction.ToTransparent: version must match!");

            var tx = new TTransaction
            {
                Version = Version,
                NumInputs = NumInputs,
                NumOutputs = NumOutputs,
                NumSigs = NumSigs,
                InnerHash = SigningHash,
                Fee = Fee,
                Inputs = TInputs,
                Outputs = TOutputs,
                Signatures = TSignatures,
            };

            return tx;
        }

        public MixedTransaction ToMixed()
        {
            if (Version != 4) throw new Exception("FullTransaction.ToMixed: version must match!");

            var tx = new MixedTransaction
            {
                Version = Version,
                NumInputs = NumInputs,
                NumOutputs = NumOutputs,
                NumPInputs = NumPInputs,
                NumPOutputs = NumPOutputs,
                NumTInputs = NumTInputs,
                NumTOutputs = NumTOutputs,
                NumSigs = NumSigs,
                Fee = Fee,
                TransactionKey = TransactionKey,
                PInputs = PInputs,
                POutputs = POutputs,
                PSignatures = PSignatures,
                RangeProofPlus = RangeProofPlus,
                PseudoOutputs = PseudoOutputs,
                TInputs = TInputs,
                TOutputs = TOutputs,
                TSignatures = TSignatures,
                SigningHash = SigningHash,
            };

            return tx;
        }

        public void FromPrivate(Transaction tx)
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
        }

        public void FromCoinbase(Transaction tx) { FromPrivate(tx); }

        public void FromTransparent(TTransaction tx)
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
        }

        public void FromMixed(MixedTransaction tx)
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
        }

        public void Serialize(BEBinaryWriter writer)
        {
            switch (Version)
            {
                case 0 or 1 or 2:
                    ToPrivate().Serialize(writer);
                    break;
                case 3:
                    ToTransparent().Serialize(writer);
                    break;
                case 4:
                    ToMixed().Serialize(writer);
                    break;
                default:
                    throw new Exception("Unknown transaction type: " + Version);
            }
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Version = reader.Peek();
            switch (Version)
            {
                case 0 or 1 or 2:
                    FromPrivate(reader.ReadSerializable<Transaction>());
                    break;
                case 3:
                    FromTransparent(reader.ReadSerializable<TTransaction>());
                    break;
                case 4:
                    FromMixed(reader.ReadSerializable<MixedTransaction>());
                    break;
                default:
                    throw new Exception("Unknown transaction type: " + Version);
            }
        }

        public SHA256 Hash()
        {
            return Version switch
            {
                0 => ToCoinbase().Hash(),
                1 or 2 => ToPrivate().Hash(),
                3 => ToTransparent().Hash(),
                4 => ToMixed().Hash(),
                _ => throw new Exception("Unknown transaction type: " + Version),
            };
        }

        public string Readable()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new Converters.TransactionConverter());

            return JsonSerializer.Serialize(this, typeof(FullTransaction), options);
        }

        public uint GetSize()
        {
            return Version switch
            {
                0 => (uint)(4 + 32 + NumOutputs * 72),
                1 => (uint)(4 + TXInput.GetSize() * PInputs.Length + 72 * POutputs.Length + RangeProof.Size + 8 + Triptych.GetSize() * PSignatures.Length + 32 * PseudoOutputs.Length + 32),
                2 => (uint)(4 + TXInput.GetSize() * PInputs.Length + 72 * POutputs.Length + RangeProofPlus.Size + 8 + Triptych.GetSize() * PSignatures.Length + 32 * PseudoOutputs.Length + 32),
                3 => (uint)(44 + TTXInput.GetSize() * TInputs.Length + 33 * TOutputs.Length + 96 * TSignatures.Length),
                4 => (uint)(48 + TTXInput.GetSize() * (TInputs == null ? 0 : TInputs.Length)
                               + 33 * (TOutputs == null ? 0 : TOutputs.Length)
                               + 96 * (TSignatures == null ? 0 : TSignatures.Length)
                               + (NumPOutputs > 0 ? 32 : 0)
                               + (PInputs == null ? 0 : PInputs.Length) * TXInput.GetSize()
                               + (POutputs == null ? 0 : POutputs.Length) * 72
                               + (RangeProofPlus == null && NumPOutputs == 0 ? 0 : RangeProofPlus.Size)
                               + Triptych.GetSize() * (PSignatures == null ? 0 : PSignatures.Length)
                               + (NumPInputs == 0 ? 0 : 32 * PseudoOutputs.Length)),
                _ => throw new Exception("Unknown transaction type: " + Version),
            };
        }

        public int Size => (int)GetSize();

        public VerifyException Verify()
        {
            return Verify(inBlock: false);
        }

        public VerifyException Verify(bool inBlock = false)
        {
            return Version switch
            {
                0 => ToCoinbase().Verify(inBlock),
                1 => ToPrivate().Verify(inBlock),
                2 => ToPrivate().Verify(inBlock),
                3 => ToTransparent().Verify(inBlock),
                4 => ToMixed().Verify(inBlock),
                _ => throw new Exception("Unknown transaction type: " + Version),
            };
        }

        public bool HasInputs()
        {
            int _noInputs = 0;

            if (TInputs != null)
            {
                _noInputs += TInputs.Length;
            }
            if (PInputs != null)
            {
                _noInputs += PInputs.Length;
            }

            return NumInputs != 0 && NumPInputs + NumTInputs != 0 && _noInputs != 0;
        }

        public bool HasOutputs()
        {
            int _noOutputs = 0;

            if (TOutputs != null)
            {
                _noOutputs += TOutputs.Length;
            }
            if (POutputs != null)
            {
                _noOutputs += POutputs.Length;
            }

            return NumOutputs != 0 && NumTOutputs + NumPOutputs != 0 && _noOutputs != 0;
        }

        public SHA256 GetSigningHash()
        {
            return Version switch
            {
                0 or 1 or 2 => ToPrivate().SigningHash(),
                3 => ToTransparent().SigningHash(),
                4 => ToMixed().TXSigningHash(),
                _ => throw new Exception("Unknown transaction type: " + Version),
            };
        }

        public Key[] GetCommitments() => POutputs == null ? Array.Empty<Key>() : POutputs.Select(x => x.Commitment).ToArray();

        public static Exception Precheck(FullTransaction tx, bool mustBeCoinbase = false)
        {
            var npin = tx.PInputs == null ? 0 : tx.PInputs.Length;
            var npout = tx.POutputs == null ? 0 : tx.POutputs.Length;
            var ntin = tx.TInputs == null ? 0 : tx.TInputs.Length;
            var ntout = tx.TOutputs == null ? 0 : tx.TOutputs.Length;

            var npsg = tx.PSignatures == null ? 0 : tx.PSignatures.Length;
            var ntsg = tx.TSignatures == null ? 0 : tx.TSignatures.Length;

            /* malformed checks; length checks, sig length checks */
            if (ntin != tx.NumTInputs) return new VerifyException("FullTransaction", $"Transparent input mismatch: expected {tx.NumTInputs}; got {ntin}");
            if (ntout != tx.NumTOutputs) return new VerifyException("FullTransaction", $"Transparent output mismatch: expected {tx.NumTOutputs}; got {ntout}");
            if (npin != tx.NumPInputs) return new VerifyException("FullTransaction", $"Private input mismatch: expected {tx.NumPInputs}; got {npin}");
            if (npout != tx.NumPOutputs) return new VerifyException("FullTransaction", $"Private output mismatch: expected {tx.NumPOutputs}; got {npout}");
            if (ntsg != ntin) return new VerifyException("FullTransaction", $"Number of transparent input signatures ({ntsg}) not equal to number of transparent inputs ({ntin})");
            if (npsg != npin) return new VerifyException("FullTransaction", $"Number of  Triptych signatures ({npsg}) not equal to number of private inputs ({npin})");
            if (npin + ntin != tx.NumInputs) return new VerifyException("FullTransaction", $"Input mismatch: expected {tx.NumInputs}; got {ntin + npin}");
            if (ntout + npout != tx.NumOutputs) return new VerifyException("FullTransaction", $"Output mismatch: expected {tx.NumOutputs}; got {ntout + npout}");

            /* ensure at least 1 in, 1 out */
            if (!mustBeCoinbase && tx.NumInputs == 0) return new VerifyException("FullTransaction", $"Transactions must have at least one input");
            if (mustBeCoinbase && tx.NumInputs > 0) return new VerifyException("FullTransaction", $"Coinbase transaction must not have any inputs");
            if (tx.NumOutputs == 0) return new VerifyException("FullTransaction", $"Transactions must have at least one output");

            /* ensure no size limit reached */
            if (ntin > Config.TRANSPARENT_MAX_NUM_INPUTS) return new VerifyException("FullTransaction", $"Number of transparent inputs exceeds maximum ({Config.TRANSPARENT_MAX_NUM_INPUTS})");
            if (ntout > Config.TRANSPARENT_MAX_NUM_OUTPUTS) return new VerifyException("FullTransaction", $"Number of transparent outputs exceeds maximum ({Config.TRANSPARENT_MAX_NUM_OUTPUTS})");
            if (npin > Config.PRIVATE_MAX_NUM_INPUTS) return new VerifyException("FullTransaction", $"Number of private inputs exceeds maximum ({Config.PRIVATE_MAX_NUM_INPUTS})");
            if (npout > Config.PRIVATE_MAX_NUM_OUTPUTS) return new VerifyException("FullTransaction", $"Number of private outputs exceeds maximum ({Config.PRIVATE_MAX_NUM_OUTPUTS})");

            /* ensure no coinbase */
            if (!mustBeCoinbase && tx.Version == 0) return new VerifyException("FullTransaction", $"Coinbase transaction must be in a block");
            else if (mustBeCoinbase && tx.Version != 0) return new VerifyException("FullTransaction", $"Transaction must be coinbase");

            /* verify additional presence */
            if (npout > 0 && !mustBeCoinbase && tx.RangeProof == null && tx.RangeProofPlus == null) return new VerifyException("FullTransaction", $"Transaction has no range proof but has private ouputs");
            if (npout > 0 && !mustBeCoinbase && tx.RangeProof != null && tx.RangeProofPlus != null) return new VerifyException("FullTransaction", $"Transaction has private outputs and sets both range proof types");
            if (npout == 0 && !mustBeCoinbase && (tx.RangeProof != null || tx.RangeProofPlus != null)) return new VerifyException("FullTransaction", $"Transaction has no private outputs but has a range proof present");
            if (npout > 0 && (tx.TransactionKey == default || tx.TransactionKey.bytes == null)) return new VerifyException("FullTransaction", $"Transaction has private outputs but no transaction key");
            if (npout == 0 && tx.TransactionKey != default && tx.TransactionKey.bytes != null) return new VerifyException("FullTransaction", $"Transaction has no private outputs but has a transaction key");
            if (mustBeCoinbase && (tx.RangeProof != null || tx.RangeProofPlus != null)) return new VerifyException("FullTransaction", $"Coinbase transaction has range proof");


            /* transparent checks */
            if (ntin > 0)
            {
                HashSet<TTXInput> _in = new HashSet<TTXInput>(new Comparers.TTXInputEqualityComparer());
                for (int i = 0; i < ntin; i++)
                {
                    _in.Add(tx.TInputs[i]);
                }

                /* duplicate inputs in same tx */
                if (_in.Count < ntin) return new VerifyException("FullTransaction", $"Transparent input double spend in same tx");
            }

            /* check for zero coin output */
            for (int i = 0; i < ntout; i++)
            {
                if (tx.TOutputs[i].Amount == 0) return new VerifyException("FullTransaction", $"Transparent output at index {i} is a zero coin output");
            }

            /* private checks */
            var npsout = tx.PseudoOutputs == null ? 0 : tx.PseudoOutputs.Length;
            if (npsout != npin) return new VerifyException("FullTransaction", $"PseudoOutput mismatch: expected {npin} (one for each private input); got {npsout}");

            TXOutput[][] mixins = new TXOutput[npin][];
            if (npin > 0)
            {
                var uncheckedTags = new List<Key>(tx.PInputs.Select(x => x.KeyImage));

                for (int i = 0; i < npin; i++)
                {
                    /* check if duplicate private inputs */
                    uncheckedTags.Remove(tx.PInputs[i].KeyImage);
                    if (uncheckedTags.Any(x => x == tx.PInputs[i].KeyImage))
                    {
                        return new VerifyException("FullTransaction", $"Key image for {i} ({tx.PInputs[i].KeyImage.ToHexShort()}) already spent in this transaction");
                    }
                }
            }

            if (npout > 0)
            {
                /* nonzero amount and commitment for each output */
                for (int i = 0; i < npout; i++)
                {
                    if (tx.POutputs[i].Amount == 0) return new VerifyException("FullTransaction", $"Zero amount field in private output at index {i}");
                    if (tx.POutputs[i].Commitment.Equals(Key.Z)) return new VerifyException("FullTransaction", $"Zero commitment field in private output at index {i}");
                }
            }

            /* validate signing hash */
            if (!mustBeCoinbase && tx.SigningHash != default && tx.SigningHash.Bytes != null && tx.SigningHash != tx.GetSigningHash()) return new VerifyException("FullTransaction", $"Signing Hash {tx.SigningHash.ToHexShort()} does not match computed signing hash {tx.GetSigningHash().ToHexShort()}");
            if (!mustBeCoinbase && (tx.SigningHash == default || tx.SigningHash.Bytes == null)) tx.SigningHash = tx.GetSigningHash();
            if (mustBeCoinbase && tx.SigningHash != default && tx.SigningHash.Bytes != null) return new VerifyException("FullTransaction", $"Signing hash is not null for coinbase tx");
            return null;
        }

        public Exception Precheck(bool mustBeCoinbase = false) => Precheck(this, mustBeCoinbase);
    }
}
