using System;
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

            uint offset = 8;
            Array.Copy(SigningHash.Bytes, 0, bytes, offset, 32);
            offset += 32;

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

            for (int i = 0; i < TSignatures.Length; i++)
            {
                TSignatures[i].ToBytes(bytes, offset);
                offset += 96;
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
                POutputs[i].Marshal(bytes, offset);
                offset += 72;
            }

            for (int i = 0; i < POutputs.Length; i++)
            {
                POutputs[i].TXMarshal(bytes, offset);
                offset += 72;
            }

            RangeProof.Marshal(bytes, offset);
            offset += RangeProof.Size();

            for (int i = 0; i < PSignatures.Length; i++)
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
            return (uint)(48 + 65 * TInputs.Length + 33 * TOutputs.Length + 96 * TSignatures.Length + 32 + PInputs.Length * TXInput.Size() + POutputs.Length * 72 + RangeProof.Size() + Triptych.Size() * PSignatures.Length + 32 * PseudoOutputs.Length);
        }

        public VerifyException Verify()
        {
            throw new NotImplementedException();
        }
    }
}
