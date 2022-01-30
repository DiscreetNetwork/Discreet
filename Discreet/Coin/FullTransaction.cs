﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Cipher;

namespace Discreet.Coin
{
    public class FullTransaction: ICoin
    {
        public byte Version;
        public byte NumInputs;
        public byte NumOutputs;
        public byte NumSigs;

        public byte NumTInputs;
        public byte NumPInputs;
        public byte NumTOutputs;
        public byte NumPOutputs;

        public SHA256 SigningHash;

        public ulong Fee;

        /* Transparent part */
        public Transparent.TXOutput[] TInputs;
        public Transparent.TXOutput[] TOutputs;
        public Signature[] TSignatures;

        /* Private part */
        public Key TransactionKey;
        public TXInput[] PInputs;
        public TXOutput[] POutputs;
        public Bulletproof RangeProof;
        public BulletproofPlus RangeProofPlus;
        public Triptych[] PSignatures;
        public Key[] PseudoOutputs;

        public FullTransaction() { }

        public FullTransaction(byte[] bytes)
        {
            Unmarshal(bytes);
        }

        public FullTransaction(Transaction tx)
        {
            FromPrivate(tx);
        }

        public FullTransaction(Transparent.Transaction tx)
        {
            FromTransparent(tx);
        }

        public FullTransaction(MixedTransaction tx)
        {
            FromMixed(tx);
        }

        public void FromCoinbase(byte[] bytes) { FromCoinbase(bytes, 0); }
        public void FromPrivate(byte[] bytes) { FromPrivate(bytes, 0); }
        public void FromTransparent(byte[] bytes) { FromTransparent(bytes, 0); }
        public void FromMixed(byte[] bytes) { FromMixed(bytes, 0); }

        public Transaction ToPrivate()
        {
            if (Version != 1 && Version != 2 && Version != 0) throw new Exception("Discreet.Coin.FullTransaction.ToPrivate: version must match!");

            return new Transaction
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
        }

        public Transaction ToCoinbase() { if (Version != 0) throw new Exception("Discreet.Coin.FullTransaction.ToCoinbase: version must match!"); return ToPrivate(); }

        public Transparent.Transaction ToTransparent()
        {
            if (Version != 3) throw new Exception("Discreet.Coin.FullTransaction.ToTransparent: version must match!");

            return new Transparent.Transaction
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
        }

        public MixedTransaction ToMixed()
        {
            if (Version != 4) throw new Exception("Discreet.Coin.FullTransaction.ToMixed: version must match!");

            return new MixedTransaction
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

        public void FromTransparent(Transparent.Transaction tx)
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

        public void Unmarshal(byte[] bytes) { Unmarshal(bytes, 0); }

        public uint Unmarshal(byte[] bytes, uint offset)
        {
            return bytes[offset] switch
            {
                0 => FromCoinbase(bytes, offset),
                1 or 2 => FromPrivate(bytes, offset),
                3 => FromTransparent(bytes, offset),
                4 => FromMixed(bytes, offset),
                _ => throw new Exception("Unknown transaction type: " + bytes[0]),
            };
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
            return Discreet.Readable.FullTransaction.ToReadable(this);
        }

        public static FullTransaction FromReadable(string json)
        {
            return Discreet.Readable.FullTransaction.FromReadable(json);
        }

        public byte[] Marshal()
        {
            switch (Version)
            {
                case 0:
                    return ToCoinbaseBytes();
                case 1:
                case 2:
                    return ToPrivateBytes();
                case 3:
                    return ToTransparentBytes();
                case 4:
                    return ToMixedBytes();
                default:
                    throw new Exception("Unknown transaction type: " + Version);
            }
        }

        public void Marshal(byte[] bytes, uint offset)
        {
            byte[] data = Marshal();
            Array.Copy(data, 0, bytes, offset, data.Length);
        }

        public byte[] ToCoinbaseBytes()
        {
            byte[] bytes = new byte[4 + 32 + NumOutputs * 72];

            bytes[0] = Version;
            bytes[1] = NumInputs;
            bytes[2] = NumOutputs;
            bytes[3] = NumSigs;

            uint offset = 4;

            Array.Copy(TransactionKey.bytes, 0, bytes, offset, 32);
            offset += 32;

            for (int i = 0; i < POutputs.Length; i++)
            {
                POutputs[i].TXMarshal(bytes, offset);
                offset += 72;
            }

            return bytes;
        }

        public byte[] ToPrivateBytes()
        {
            byte[] bytes = new byte[Size()];

            bytes[0] = Version;
            bytes[1] = NumInputs;
            bytes[2] = NumOutputs;
            bytes[3] = NumSigs;

            uint offset = 4;

            byte[] fee = BitConverter.GetBytes(Fee);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(fee);
            }

            Array.Copy(fee, 0, bytes, offset, 8);
            offset += 8;

            Array.Copy(TransactionKey.bytes, 0, bytes, offset, 32);
            offset += 32;

            for (int i = 0; i < PInputs.Length; i++)
            {
                PInputs[i].Marshal(bytes, offset);
                offset += TXInput.Size();
            }

            for (int i = 0; i < POutputs.Length; i++)
            {
                POutputs[i].TXMarshal(bytes, offset);
                offset += 72;
            }

            if (Version == 2)
            {
                RangeProofPlus.Marshal(bytes, offset);
                offset += RangeProofPlus.Size();
            }
            else
            {
                RangeProof.Marshal(bytes, offset);
                offset += RangeProof.Size();
            }

            for (int i = 0; i < NumSigs; i++)
            {
                PSignatures[i].Marshal(bytes, offset);
                offset += Triptych.Size();
            }

            for (int i = 0; i < PseudoOutputs.Length; i++)
            {
                Array.Copy(PseudoOutputs[i].bytes, 0, bytes, offset, 32);
                offset += 32;
            }

            return bytes;
        }

        public byte[] ToTransparentBytes()
        {
            byte[] bytes = new byte[Size()];

            bytes[0] = Version;
            bytes[1] = NumInputs;
            bytes[2] = NumOutputs;
            bytes[3] = NumSigs;

            uint offset = 4;

            byte[] fee = BitConverter.GetBytes(Fee);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(fee);
            }

            Array.Copy(fee, 0, bytes, offset, 8);
            offset += 8;

            Array.Copy(SigningHash.Bytes, 0, bytes, offset, 32);
            offset += 32;


            for (int i = 0; i < TInputs.Length; i++)
            {
                TInputs[i].Marshal(bytes, offset);
                offset += 65;
            }

            for (int i = 0; i < TOutputs.Length; i++)
            {
                TOutputs[i].TXMarshal(bytes, offset);
                offset += 33;
            }

            for (int i = 0; i < TSignatures.Length; i++)
            {
                TSignatures[i].ToBytes(bytes, offset);
                offset += 96;
            }

            return bytes;
        }

        public byte[] ToMixedBytes()
        {
            byte[] bytes = new byte[Size()];

            bytes[0] = Version;
            bytes[1] = NumInputs;
            bytes[2] = NumOutputs;
            bytes[3] = NumSigs;

            bytes[4] = NumTInputs;
            bytes[5] = NumPInputs;
            bytes[6] = NumTOutputs;
            bytes[7] = NumPOutputs;

            int lenTInputs = TInputs == null ? 0 : TInputs.Length;
            int lenTOutputs = TOutputs == null ? 0 : TOutputs.Length;
            int lenPInputs = PInputs == null ? 0 : POutputs.Length;
            int lenPOutputs = POutputs == null ? 0 : POutputs.Length;
            int lenTSigs = TSignatures == null ? 0 : TSignatures.Length;
            int lenPSigs = PSignatures == null ? 0 : PSignatures.Length;

            uint offset = 8;

            Serialization.CopyData(bytes, offset, Fee);
            offset += 8;

            Array.Copy(SigningHash.Bytes, 0, bytes, offset, 32);
            offset += 32;

            for (int i = 0; i < lenTInputs; i++)
            {
                TInputs[i].Marshal(bytes, offset);
                offset += 65;
            }

            for (int i = 0; i < lenTOutputs; i++)
            {
                TOutputs[i].TXMarshal(bytes, offset);
                offset += 33;
            }

            for (int i = 0; i < lenTSigs; i++)
            {
                TSignatures[i].ToBytes(bytes, offset);
                offset += 96;
            }

            if (lenPOutputs > 0)
            {
                Array.Copy(TransactionKey.bytes, 0, bytes, offset, 32);
                offset += 32;
            }

            for (int i = 0; i < lenPInputs; i++)
            {
                PInputs[i].Marshal(bytes, offset);
                offset += TXInput.Size();
            }

            for (int i = 0; i < lenPOutputs; i++)
            {
                POutputs[i].Marshal(bytes, offset);
                offset += 72;
            }

            if (RangeProof != null && lenPOutputs > 0)
            {
                RangeProof.Marshal(bytes, offset);
                offset += RangeProof.Size();
            }

            for (int i = 0; i < lenPSigs; i++)
            {
                PSignatures[i].Marshal(bytes, offset);
                offset += Triptych.Size();
            }

            /* all MixedTransactions will contain PseudoOutputs */
            for (int i = 0; i < NumInputs; i++)
            {
                Array.Copy(PseudoOutputs[i].bytes, 0, bytes, offset, 32);
                offset += 32;
            }

            return bytes;
        }

        public uint FromCoinbase(byte[] bytes, uint offset)
        {
            Version = bytes[offset];
            NumInputs = bytes[offset + 1];
            NumOutputs = bytes[offset + 2];
            NumSigs = bytes[offset + 3];

            NumPInputs = 0;
            NumPOutputs = NumOutputs;
            NumTInputs = 0;
            NumTOutputs = 0;

            offset += 4;

            TransactionKey = new Cipher.Key(new byte[32]);
            Array.Copy(bytes, offset, TransactionKey.bytes, 0, 32);
            offset += 32;

            POutputs = new TXOutput[NumOutputs];

            for (int i = 0; i < NumOutputs; i++)
            {
                POutputs[i] = new TXOutput();

                POutputs[i].TXUnmarshal(bytes, offset);
                offset += 72;
            }

            return offset;
        }

        public uint FromPrivate(byte[] bytes, uint offset)
        {
            Version = bytes[offset];
            NumInputs = bytes[offset + 1];
            NumOutputs = bytes[offset + 2];
            NumSigs = bytes[offset + 3];

            NumPInputs = NumInputs;
            NumPOutputs = NumOutputs;
            NumTInputs = 0;
            NumTOutputs = 0;

            offset += 4;

            byte[] fee = new byte[8];

            Array.Copy(bytes, offset, fee, 0, 8);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(fee);
            }

            Fee = BitConverter.ToUInt64(fee);
            offset += 8;

            TransactionKey = new Cipher.Key(new byte[32]);
            Array.Copy(bytes, offset, TransactionKey.bytes, 0, 32);
            offset += 32;

            PInputs = new TXInput[NumInputs];

            for (int i = 0; i < NumInputs; i++)
            {
                PInputs[i] = new TXInput();

                PInputs[i].Unmarshal(bytes, offset);
                offset += TXInput.Size();
            }

            POutputs = new TXOutput[NumOutputs];

            for (int i = 0; i < NumOutputs; i++)
            {
                POutputs[i] = new TXOutput();

                POutputs[i].TXUnmarshal(bytes, offset);
                offset += 72;
            }

            if (Version == 2)
            {
                RangeProofPlus = new BulletproofPlus();

                RangeProofPlus.Unmarshal(bytes, offset);
                offset += RangeProofPlus.Size();
            }
            else
            {
                RangeProof = new Bulletproof();

                RangeProof.Unmarshal(bytes, offset);
                offset += RangeProof.Size();
            }

            PSignatures = new Triptych[NumSigs];

            for (int i = 0; i < NumSigs; i++)
            {
                PSignatures[i] = new Triptych();

                PSignatures[i].Unmarshal(bytes, offset);
                offset += Triptych.Size();
            }

            PseudoOutputs = new Key[NumInputs];
            for (int i = 0; i < NumInputs; i++)
            {
                PseudoOutputs[i] = new Key(new byte[32]);
                Array.Copy(bytes, offset, PseudoOutputs[i].bytes, 0, 32);
                offset += 32;
            }

            return offset;
        }

        public uint FromTransparent(byte[] bytes, uint offset)
        {
            Version = bytes[offset];
            NumInputs = bytes[offset + 1];
            NumOutputs = bytes[offset + 2];
            NumSigs = bytes[offset + 3];

            NumTInputs = NumInputs;
            NumTOutputs = NumOutputs;
            NumPInputs = 0;
            NumPOutputs = 0;

            SigningHash = new SHA256(new byte[32], false);
            Array.Copy(bytes, 4, SigningHash.Bytes, 0, 32);

            offset += 36;

            TInputs = new Transparent.TXOutput[NumInputs];

            for (int i = 0; i < TInputs.Length; i++)
            {
                TInputs[i] = new Transparent.TXOutput();
                TInputs[i].Unmarshal(bytes, offset);
                offset += Transparent.TXOutput.Size();
            }

            TOutputs = new Transparent.TXOutput[NumOutputs];

            for (int i = 0; i < TOutputs.Length; i++)
            {
                TOutputs[i] = new Transparent.TXOutput();
                TOutputs[i].TXUnmarshal(bytes, offset);
                offset += 33;
            }

            TSignatures = new Signature[NumSigs];

            for (int i = 0; i < TSignatures.Length; i++)
            {
                TSignatures[i] = new Signature(bytes, offset);
                offset += 96;
            }

            byte[] fee = new byte[8];
            Array.Copy(bytes, offset, fee, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(fee);
            }

            Fee = BitConverter.ToUInt64(fee);

            return offset + 8;
        }

        public uint FromMixed(byte[] bytes, uint offset)
        {
            Version = bytes[offset];
            NumInputs = bytes[offset + 1];
            NumOutputs = bytes[offset + 2];
            NumSigs = bytes[offset + 3];

            NumTInputs = bytes[offset + 4];
            NumPInputs = bytes[offset + 5];
            NumTOutputs = bytes[offset + 6];
            NumPOutputs = bytes[offset + 7];

            offset += 8;

            Fee = Serialization.GetUInt64(bytes, offset);
            offset += 8;

            byte[] signingHash = new byte[32];
            Array.Copy(bytes, offset, signingHash, 0, 32);
            SigningHash = new SHA256(signingHash, false);
            offset += 32;

            TInputs = new Transparent.TXOutput[NumTInputs];
            for (int i = 0; i < NumTInputs; i++)
            {
                TInputs[i] = new Transparent.TXOutput();
                TInputs[i].Unmarshal(bytes, offset);
                offset += 65;
            }

            TOutputs = new Transparent.TXOutput[NumTOutputs];
            for (int i = 0; i < NumTOutputs; i++)
            {
                TOutputs[i] = new Transparent.TXOutput();
                TOutputs[i].TXUnmarshal(bytes, offset);
                offset += 33;
            }

            TSignatures = new Signature[NumTInputs];
            for (int i = 0; i < NumTOutputs; i++)
            {
                TSignatures[i] = new Signature(bytes, offset);
                offset += 96;
            }

            byte[] transactionKey = new byte[32];
            Array.Copy(bytes, offset, transactionKey, 0, 32);
            TransactionKey = new Key(transactionKey);
            offset += 32;

            PInputs = new TXInput[NumPInputs];
            for (int i = 0; i < NumPInputs; i++)
            {
                PInputs[i] = new TXInput();
                PInputs[i].Unmarshal(bytes, offset);
                offset += TXInput.Size();
            }

            POutputs = new TXOutput[NumPOutputs];
            for (int i = 0; i < NumPOutputs; i++)
            {
                POutputs[i] = new TXOutput();
                POutputs[i].TXUnmarshal(bytes, offset);
                offset += 72;
            }

            RangeProofPlus = new BulletproofPlus();
            RangeProofPlus.Unmarshal(bytes, offset);
            offset += RangeProofPlus.Size();

            PSignatures = new Triptych[NumPInputs];
            for (int i = 0; i < NumPInputs; i++)
            {
                PSignatures[i] = new Triptych();
                PSignatures[i].Unmarshal(bytes, offset);
                offset += Triptych.Size();
            }

            PseudoOutputs = new Key[NumInputs];
            for (int i = 0; i < NumInputs; i++)
            {
                PseudoOutputs[i] = new Cipher.Key(new byte[32]);
                Array.Copy(bytes, offset, PseudoOutputs[i].bytes, 0, 32);
                offset += 32;
            }

            return offset;
        }

        public uint Size()
        {
            return Version switch
            {
                0 => (uint)(4 + 32 + NumOutputs * 72),
                1 => (uint)(4 + TXInput.Size() * PInputs.Length + 72 * POutputs.Length + RangeProof.Size() + 8 + Triptych.Size() * PSignatures.Length + 32 * PseudoOutputs.Length + 32),
                2 => (uint)(4 + TXInput.Size() * PInputs.Length + 72 * POutputs.Length + RangeProofPlus.Size() + 8 + Triptych.Size() * PSignatures.Length + 32 * PseudoOutputs.Length + 32),
                3 => (uint)(44 + TXOutput.Size() * TInputs.Length + 33 * TOutputs.Length + 96 * TSignatures.Length),
                4 => (uint)(48 + 65 * (TInputs == null ? 0 : TInputs.Length)
                               + 33 * (TOutputs == null ? 0 : TOutputs.Length)
                               + 96 * (TSignatures == null ? 0 : TSignatures.Length)
                               + (NumPOutputs > 0 ? 32 : 0)
                               + (PInputs == null ? 0 : PInputs.Length) * TXInput.Size()
                               + (POutputs == null ? 0 : POutputs.Length) * 72
                               + ((RangeProof == null && NumPOutputs > 0) ? 0 : RangeProof.Size())
                               + Triptych.Size() * (PSignatures == null ? 0 : PSignatures.Length)
                               + 32 * PseudoOutputs.Length),
                _ => throw new Exception("Unknown transaction type: " + Version),
            };
        }

        public bool HasPrivateOutputs()
        {
            return POutputs != null && POutputs.Length > 0;
        }

        public TXOutput[] GetPrivateOutputs()
        {
            if (Version == 3) return new TXOutput[0];

            return POutputs;
        }

        public static TXOutput[] GetPrivateOutputs(byte[] Data)
        {
            if (Data[0] == 3) return new TXOutput[0];

            return (Data[0] == 4) ? new MixedTransaction(Data).POutputs : new Transaction(Data).Outputs;
        }

        public static (TXOutput[], TXInput[]) GetPrivateOutputsAndInputs(byte[] Data)
        {
            if (Data[0] == 3) return (new TXOutput[0], new TXInput[0]);

            if (Data[0] == 4)
            {
                MixedTransaction tx = new MixedTransaction(Data);
                return (tx.POutputs, tx.PInputs);
            }
            else
            {
                Transaction tx = new Transaction(Data);
                return (tx.Outputs, tx.Inputs);
            }
        }

        public VerifyException Verify()
        {
            return Version switch
            {
                0 => ToCoinbase().Verify(),
                1 => ToPrivate().Verify(),
                2 => ToPrivate().Verify(),
                3 => ToTransparent().Verify(),
                4 => ToMixed().Verify(),
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

            return NumInputs != 0 && (NumPInputs + NumTInputs) != 0 && _noInputs != 0;
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

            return NumOutputs != 0 && (NumTOutputs + NumPOutputs) != 0 && _noOutputs != 0;
        }
    }
}