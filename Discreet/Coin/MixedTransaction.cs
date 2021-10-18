﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Cipher;

namespace Discreet.Coin
{
    /**
     * A mixed transaction contains both transparent and private inputs and outputs. 
     * The transaction cannot be partially signed like normal transparent transactions.
     * Additionally, there is no Extra field, and the transaction must use one uniform TXKey.
     */
    public class MixedTransaction: ICoin
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
        public BulletproofPlus RangeProof;
        public Triptych[] PSignatures;
        public Key[] PseudoOutputs;

        public SHA256 Hash()
        {
            return SHA256.HashData(Marshal());
        }

        public SHA256 TXSigningHash()
        {
            byte[] bytes = new byte[16 + 65 * TInputs.Length + 33 * TOutputs.Length + 32 + PInputs.Length * TXInput.Size() + POutputs.Length * 40];

            bytes[0] = Version;
            bytes[1] = NumInputs;
            bytes[2] = NumOutputs;
            bytes[3] = NumSigs;

            bytes[4] = NumTInputs;
            bytes[5] = NumPInputs;
            bytes[6] = NumTOutputs;
            bytes[7] = NumPOutputs;

            uint offset = 8;
            Serialization.CopyData(bytes, offset, Fee);
            offset += 8;

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

            Array.Copy(TransactionKey.bytes, 0, bytes, offset, 32);
            offset += 32;

            for (int i = 0; i < PInputs.Length; i++)
            {
                PInputs[i].Marshal(bytes, offset);
                offset += TXInput.Size();
            }

            for (int i = 0; i < POutputs.Length; i++)
            {
                Array.Copy(POutputs[i].UXKey.bytes, 0, bytes, offset, 32);
                offset += 32;
                Serialization.CopyData(bytes, offset, POutputs[i].Amount);
                offset += 8;
            }

            return SHA256.HashData(bytes);
        }

        public byte[] Marshal()
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
            Array.Copy(SigningHash.Bytes, 0, bytes, offset, 32);
            offset += 32;

            Serialization.CopyData(bytes, offset, Fee);
            offset += 8;

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

            for (int i = 0; i < lenPInputs; i++)
            {
                Array.Copy(PseudoOutputs[i].bytes, 0, bytes, offset, 32);
                offset += 32;
            }

            return bytes;
        }

        public void Marshal(byte[] bytes, uint offset)
        {
            byte[] _bytes = Marshal();
            Array.Copy(_bytes, 0, bytes, offset, _bytes.Length);
        }

        public string Readable()
        {
            return Discreet.Readable.MixedTransaction.ToReadable(this);
        }

        public static MixedTransaction FromReadable(string json)
        {
            return Discreet.Readable.MixedTransaction.FromReadable(json);
        }

        public void Unmarshal(byte[] bytes)
        {
            Version = bytes[0];
            NumInputs = bytes[1];
            NumOutputs = bytes[2];
            NumSigs = bytes[3];

            NumTInputs = bytes[4];
            NumPInputs = bytes[5];
            NumTOutputs = bytes[6];
            NumPOutputs = bytes[7];

            uint offset = 8;
            byte[] signingHash = new byte[32];
            Array.Copy(bytes, offset, signingHash, 0, 32);
            SigningHash = new SHA256(signingHash, false);
            offset += 32;

            Fee = Serialization.GetUInt64(bytes, offset);
            offset += 8;

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
            for (int i = 0; i < NumTInputs; i++)
            {
                TSignatures[i] = new Signature(bytes, offset);
                offset += 96;
            }

            if (NumPOutputs > 0)
            {
                byte[] transactionKey = new byte[32];
                Array.Copy(bytes, offset, transactionKey, 0, 32);
                TransactionKey = new Key(transactionKey);
                offset += 32;
            }

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

            if (NumPOutputs > 0)
            {
                RangeProof = new BulletproofPlus();
                RangeProof.Unmarshal(bytes, offset);
                offset += RangeProof.Size();
            }

            PSignatures = new Triptych[NumPInputs];
            for (int i = 0; i < NumPInputs; i++)
            {
                PSignatures[i] = new Triptych();
                PSignatures[i].Unmarshal(bytes, offset);
                offset += Triptych.Size();
            }

            PseudoOutputs = new Key[NumPInputs];
            for (int i = 0; i < NumPInputs; i++)
            {
                PseudoOutputs[i] = new Cipher.Key(new byte[32]);
                Array.Copy(bytes, offset, PseudoOutputs[i].bytes, 0, 32);
                offset += 32;
            }
        }

        public uint Unmarshal(byte[] bytes, uint offset)
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

            byte[] signingHash = new byte[32];
            Array.Copy(bytes, offset, signingHash, 0, 32);
            SigningHash = new SHA256(signingHash, false);
            offset += 32;

            Fee = Serialization.GetUInt64(bytes, offset);
            offset += 8;

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

            RangeProof = new BulletproofPlus();
            RangeProof.Unmarshal(bytes, offset);
            offset += RangeProof.Size();

            PSignatures = new Triptych[NumPInputs];
            for (int i = 0; i < NumPInputs; i++)
            {
                PSignatures[i] = new Triptych();
                PSignatures[i].Unmarshal(bytes, offset);
                offset += Triptych.Size();
            }

            PseudoOutputs = new Key[NumPInputs];
            for (int i = 0; i < NumPInputs; i++)
            {
                PseudoOutputs[i] = new Cipher.Key(new byte[32]);
                Array.Copy(bytes, offset, PseudoOutputs[i].bytes, 0, 32);
                offset += 32;
            }

            return offset;
        }

        public uint Size()
        {
            return (uint)(48 + 65 * (TInputs == null ? 0 : TInputs.Length)
                             + 33 * (TOutputs == null ? 0 : TOutputs.Length)
                             + 96 * (TSignatures == null ? 0 : TSignatures.Length)
                             + (NumPOutputs > 0 ? 32 : 0)
                             + (PInputs == null ? 0 : PInputs.Length) * TXInput.Size()
                             + (POutputs == null ? 0 : POutputs.Length) * 72
                             + ((RangeProof == null && NumPOutputs > 0) ? 0 : RangeProof.Size())
                             + Triptych.Size() * (PSignatures == null ? 0 : PSignatures.Length)
                             + 32 * (PseudoOutputs == null ? 0 : PseudoOutputs.Length));
        }

        public Key[] GetCommitments()
        {
            Key[] Comms = new Key[NumPOutputs];

            for (int i = 0; i < NumPOutputs; i++)
            {
                Comms[i] = POutputs[i].Commitment;
            }

            return Comms;
        }

        public VerifyException Verify()
        {
            bool tInNull = TInputs == null;
            bool tOutNull = TOutputs == null;
            bool pInNull = PInputs == null;
            bool pOutNull = POutputs == null;
            bool tSigNull = TSignatures == null;
            bool pSigNull = PSignatures == null;

            int lenTInputs = tInNull ? 0 : TInputs.Length;
            int lenTOutputs = tOutNull ? 0 : TOutputs.Length;
            int lenPInputs = pInNull ? 0 : POutputs.Length;
            int lenPOutputs = pOutNull ? 0 : POutputs.Length;
            int lenTSigs = tSigNull ? 0 : TSignatures.Length;
            int lenPSigs = pSigNull ? 0 : PSignatures.Length;

            if (Version != (byte)Config.TransactionVersions.MIXED)
            {
                return new VerifyException("MixedTransaction", $"Invalid transaction version: expected {(byte)Config.TransactionVersions.MIXED}, but got {Version}");
            }

            if (lenTInputs + lenPInputs == 0)
            {
                return new VerifyException("MixedTransaction", "Zero inputs");
            }

            if (lenTOutputs + lenPOutputs == 0)
            {
                return new VerifyException("MixedTransaction", "Zero outputs");
            }

            if (NumInputs != lenTInputs + lenPInputs)
            {
                return new VerifyException("MixedTransaction", $"Input number mismatch: expected {NumInputs}, but got {lenTInputs + lenPOutputs}");
            }

            if (NumOutputs != lenTOutputs + lenPOutputs)
            {
                return new VerifyException("MixedTransaction", $"Output number mismatch: expected {NumOutputs}, but got {lenTOutputs + lenPOutputs}");
            }

            if (NumInputs > Config.TRANSPARENT_MAX_NUM_INPUTS)
            {
                return new VerifyException("MixedTransaction", $"Number of Inputs exceeds the maximum of {Config.TRANSPARENT_MAX_NUM_INPUTS} ({NumInputs} Inputs present)");
            }

            if (NumOutputs > Config.TRANSPARENT_MAX_NUM_OUTPUTS)
            {
                return new VerifyException("MixedTransaction", $"Number of Inputs exceeds the maximum of {Config.TRANSPARENT_MAX_NUM_OUTPUTS} ({NumOutputs} Inputs present)");
            }

            if (NumSigs > NumInputs)
            {
                return new VerifyException("MixedTransaction", $"Invalid number of signatures: {NumSigs} > {NumInputs}, exceeding the number of inputs");
            }

            if (lenTInputs != NumTInputs)
            {
                return new VerifyException("MixedTransaction", $"Transparent input number mismatch: expected {NumTInputs}, but got {lenTInputs}");
            }

            if (lenTOutputs != NumTOutputs)
            {
                return new VerifyException("MixedTransaction", $"Transparent output number mismatch: expected {NumTOutputs}, but got {lenTOutputs}");
            }

            if (lenPInputs != NumPInputs)
            {
                return new VerifyException("MixedTransaction", $"Private input number mismatch: expected {NumPInputs}, but got {lenPInputs}");
            }

            if (lenPOutputs != NumPOutputs)
            {
                return new VerifyException("MixedTransaction", $"Private output number mismatch: expected {NumPOutputs}, but got {lenPOutputs}");
            }

            if (NumTInputs > Config.TRANSPARENT_MAX_NUM_INPUTS)
            {
                return new VerifyException("MixedTransaction", $"Number of transparent inputs exceeds the maximum of {Config.TRANSPARENT_MAX_NUM_INPUTS} ({NumTInputs} Inputs present)");
            }

            if (NumTOutputs > Config.TRANSPARENT_MAX_NUM_OUTPUTS)
            {
                return new VerifyException("MixedTransaction", $"Number of transparent outputs exceeds the maximum of {Config.TRANSPARENT_MAX_NUM_OUTPUTS} ({NumTOutputs} Inputs present)");
            }

            if (lenTSigs + lenPSigs != NumSigs)
            {
                return new VerifyException("MixedTransaction", $"Signature number mismatch: expected {NumSigs}, but got {lenTSigs + lenPSigs}");
            }

            if (lenTSigs != NumTInputs)
            {
                return new VerifyException("MixedTransaction", $"Transactions must have the same number of transparent signatures as transparent inputs ({NumTInputs} inputs, but {lenTSigs} sigs)");
            }

            if (lenPSigs != NumPInputs)
            {
                return new VerifyException("MixedTransaction", $"Transactions must have the same number of private signatures as private inputs ({NumPInputs} inputs, but {lenPSigs} sigs)");
            }

            if (lenPOutputs > 0 && RangeProof == null)
            {
                return new VerifyException("MixedTransaction", $"Transaction contains at least one private output, but has no range proof!");
            }

            if (lenPOutputs > 0 && (TransactionKey.Equals(default) || TransactionKey.bytes == null))
            {
                return new VerifyException("MixedTransaction", $"Transaction contains at least one private output, but has no transaction key!");
            }

            HashSet<SHA256> _in = new HashSet<SHA256>();

            for (int i = 0; i < NumTInputs; i++)
            {
                _in.Add(TInputs[i].Hash());
            }

            if (_in.Count != NumTInputs)
            {
                return new VerifyException("MixedTransaction", $"Duplicate transparent inputs detected!");
            }

            foreach (Transparent.TXOutput output in TOutputs)
            {
                if (output.Amount == 0)
                {
                    return new VerifyException("MixedTransaction", "zero coins in output!");
                }
            }

            ulong _amount = 0;

            foreach (Transparent.TXOutput output in TOutputs)
            {
                try
                {
                    _amount = checked(_amount + output.Amount);
                }
                catch (OverflowException e)
                {
                    return new VerifyException("MixedTransaction", $"transaction transparent outputs resulted in overflow!");
                }
            }

            SHA256 txhash = Hash();

            HashSet<SHA256> _out = new HashSet<SHA256>();

            for (int i = 0; i < NumTOutputs; i++)
            {
                _out.Add(new Transparent.TXOutput(txhash, TOutputs[i].Address, TOutputs[i].Amount).Hash());
            }

            if (_out.Count != NumOutputs)
            {
                return new VerifyException("MixedTransaction", $"Duplicate transparent outputs detected!");
            }

            if (SigningHash != TXSigningHash())
            {
                return new VerifyException("MixedTransaction", $"Signing Hash {SigningHash.ToHexShort()} does not match computed signing hash {TXSigningHash().ToHexShort()}");
            }

            for (int i = 0; i < lenTSigs; i++)
            {
                if (TSignatures[i].IsNull())
                {
                    return new VerifyException("MixedTransaction", $"Unsigned transparent input present in transaction!");
                }

                byte[] data = new byte[64];
                Array.Copy(SigningHash.Bytes, data, 32);
                Array.Copy(TInputs[i].Hash().Bytes, 0, data, 32, 32);

                SHA256 checkSig = SHA256.HashData(data);

                if (!TSignatures[i].Verify(checkSig))
                {
                    return new VerifyException("MixedTransaction", $"Signature failed verification!");
                }
            }

            if (NumPOutputs > 16)
            {
                return new VerifyException("MixedTransaction", $"Transactions cannot have more than 16 private outputs");
            }

            for (int i = 0; i < NumPOutputs; i++)
            {
                if (POutputs[i].Amount == 0)
                {
                    return new VerifyException("MixedTransaction", $"Amount field in private output at index {i} is not set!");
                }

                if (POutputs[i].Commitment.Equals(Key.Z))
                {
                    return new VerifyException("MixedTransaction", $"Commitment field in private output at index {i} is not set!");
                }
            }

            if (NumPInputs > 0)
            {
                if (PseudoOutputs == null || PseudoOutputs.Length == 0)
                {
                    return new VerifyException("MixedTransaction", $"Transaction contains private inputs but does not contain any PseudoOutputs");
                }

                if (NumPInputs != PseudoOutputs.Length)
                {
                    return new VerifyException("MixedTransaction", $"PseudoOutput length mismatch: expected {NumPInputs}, but got {PseudoOutputs.Length}");
                }
            }

            Key sumPseudos = new(new byte[32]);
            Key tmp = new(new byte[32]);

            for (int i = 0; i < lenPInputs; i++)
            {
                KeyOps.AddKeys(ref tmp, ref sumPseudos, ref PseudoOutputs[i]);
                Array.Copy(tmp.bytes, sumPseudos.bytes, 32);
            }

            Key sumComms = new(new byte[32]);

            for (int i = 0; i < lenPOutputs; i++)
            {
                KeyOps.AddKeys(ref tmp, ref sumComms, ref POutputs[i].Commitment);
                Array.Copy(tmp.bytes, sumComms.bytes, 32);
            }

            /* since we may have transparent inputs as well, we need to commit sumTInputs and sumTOutputs; this is just a blinding factor of 0 */

            ulong sumTInputs = 0;
            for (int i = 0; i < lenTInputs; i++)
            {
                sumTInputs += TInputs[i].Amount;
            }

            ulong sumTOutputs = 0;
            for (int i = 0; i < lenTOutputs; i++)
            {
                sumTOutputs += TOutputs[i].Amount;
            }

            Key commTInputs = new(new byte[32]);
            KeyOps.GenCommitment(ref commTInputs, ref Key.Z, sumTInputs);
            KeyOps.AddKeys(ref tmp, ref sumPseudos, ref commTInputs);
            Array.Copy(tmp.bytes, sumPseudos.bytes, 32);

            Key commTOutputs = new(new byte[32]);
            KeyOps.GenCommitment(ref commTOutputs, ref Key.Z, sumTOutputs);
            KeyOps.AddKeys(ref tmp, ref sumComms, ref commTOutputs);
            Array.Copy(tmp.bytes, sumComms.bytes, 32);

            //Cipher.Key dif = new(new byte[32]);
            //Cipher.KeyOps.SubKeys(ref dif, ref sumPseudos, ref sumComms);

            if (!sumPseudos.Equals(sumComms))
            {
                return new VerifyException("MixedTransaction", $"Transaction does not balance! (sumC'a != sumCb)");
            }

            /* validate range sig */
            VerifyException bpexc = null;

            if (RangeProof != null)
            {
                bpexc = RangeProof.Verify(this);
            }

            if (bpexc != null)
            {
                return bpexc;
            }

            DB.DB db = DB.DB.GetDB();

            /* validate inputs */
            for (int i = 0; i < lenPInputs; i++)
            {
                if (PInputs[i].Offsets.Length != 64)
                {
                    return new VerifyException("MixedTransaction", $"Private input at index {i} has an anonymity set of size {PInputs[i].Offsets.Length}; expected 64");
                }

                TXOutput[] mixins;
                try
                {
                    mixins = db.GetMixins(PInputs[i].Offsets);
                }
                catch (Exception e)
                {
                    return new VerifyException("MixedTransaction", $"Got error when getting mixin data at input at index {i}: " + e.Message);
                }

                Cipher.Key[] M = new Cipher.Key[64];
                Cipher.Key[] P = new Cipher.Key[64];

                for (int k = 0; k < 64; k++)
                {
                    M[k] = mixins[k].UXKey;
                    P[k] = mixins[k].Commitment;
                }

                var sigexc = PSignatures[i].Verify(M, P, PseudoOutputs[i], SigningHash.ToKey(), PInputs[i].KeyImage);

                if (sigexc != null)
                {
                    return sigexc;
                }

                if (!db.CheckSpentKey(PInputs[i].KeyImage))
                {
                    return new VerifyException("MixedTransaction", $"Key image for input at index {i} ({PInputs[i].KeyImage.ToHexShort()}) already spent! (double spend)");
                }
            }

            /* this *should* be everything needed to validate a mixed transaction */
            return null;
        }
    }
}