using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Coin
{
    [StructLayout(LayoutKind.Sequential)]
    public class Transaction: ICoin
    {
        [MarshalAs(UnmanagedType.U1)]
        public byte Version;
        [MarshalAs(UnmanagedType.U1)]
        public byte NumInputs;
        [MarshalAs(UnmanagedType.U1)]
        public byte NumOutputs;
        [MarshalAs(UnmanagedType.U1)]
        public byte NumSigs;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct)]
        public TXInput[] Inputs;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct)]
        public TXOutput[] Outputs;

        [MarshalAs(UnmanagedType.Struct)]
        public Bulletproof RangeProof;

        [MarshalAs(UnmanagedType.U8)]
        public ulong Fee;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct)]
        public Triptych[] Signatures;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct)]
        public Cipher.Key[] PseudoOutputs;

        [MarshalAs(UnmanagedType.U4)]
        public uint ExtraLen;

        [MarshalAs(UnmanagedType.ByValArray)]
        public byte[] Extra;

        public Cipher.SHA256 Hash()
        {
            return Cipher.SHA256.HashData(Marshal());
        }

        /**
         * This is the hash signed through signatures.
         * It is composed of:
         *  Version,
         *  Inputs,
         *  Outputs (only output pubkey and amount),
         *  Extra
         */
        public Cipher.SHA256 SigningHash()
        {
            byte[] bytes = new byte[1 + Inputs.Length * TXInput.Size() + (32 + 8) * Outputs.Length + Extra.Length];

            bytes[0] = Version;

            uint offset = 1;

            for (int i = 0; i < Inputs.Length; i++)
            {
                Array.Copy(Inputs[i].Marshal(), 0, bytes, offset, TXInput.Size());
                offset += TXInput.Size();
            }

            for (int i = 0; i < Outputs.Length; i++)
            {
                Array.Copy(Outputs[i].UXKey.bytes, 0, bytes, offset, 32);
                offset += 32;

                byte[] amount = BitConverter.GetBytes(Outputs[i].Amount);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(amount);
                }

                Array.Copy(amount, 0, bytes, offset, 8);
                offset += 8;
            }

            Array.Copy(Extra, 0, bytes, offset, Extra.Length);

            return Cipher.SHA256.HashData(bytes);
        }

        public byte[] Marshal()
        {
            byte[] bytes = new byte[Size()];

            bytes[0] = Version;
            bytes[1] = NumInputs;
            bytes[2] = NumOutputs;
            bytes[3] = NumSigs;

            uint offset = 4;

            for (int i = 0; i < Inputs.Length; i++)
            {
                Inputs[i].Marshal(bytes, offset);
                offset += TXInput.Size();
            }

            for (int i = 0; i < Outputs.Length; i++)
            {
                Outputs[i].TXMarshal(bytes, offset);
                offset += 72;
            }

            RangeProof.Marshal(bytes, offset);
            offset += RangeProof.Size();

            byte[] fee = BitConverter.GetBytes(Fee);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(fee);
            }

            Array.Copy(fee, 0, bytes, offset, 8);
            offset += 8;

            for (int i = 0; i < NumSigs; i++)
            {
                Signatures[i].Marshal(bytes, offset);
                offset += Triptych.Size();
            }

            for (int i = 0; i < NumSigs; i++)
            {
                Array.Copy(PseudoOutputs[i].bytes, 0, bytes, offset, 32);
                offset += 32;
            }

            byte[] extraLen = BitConverter.GetBytes(ExtraLen);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(extraLen);
            }

            Array.Copy(extraLen, 0, bytes, offset, 4);
            offset += 4;

            Array.Copy(Extra, 0, bytes, offset, Extra.Length);

            return bytes;
        }

        public void Marshal(byte[] bytes, uint offset)
        {
            byte[] rv = Marshal();

            Array.Copy(rv, 0, bytes, offset, rv.Length);
        }

        public string Readable()
        {
            string rv = "{";

            rv += $"\"Version:\":{Version},";
            rv += $"\"NumInputs:\":{NumInputs},";
            rv += $"\"NumOutputs:\":{NumOutputs},";
            rv += $"\"NumSigs:\":{NumSigs},";

            rv += "\"Inputs\":[";

            for (int i = 0; i < Inputs.Length; i++)
            {
                rv += Inputs[i].Readable();

                if (i < Inputs.Length - 1)
                {
                    rv += ",";
                }
            }

            rv += "],\"Outputs\":[";

            for (int i = 0; i < Outputs.Length; i++)
            {
                rv += Outputs[i].TXReadable();

                if (i < Outputs.Length - 1)
                {
                    rv += ",";
                }
            }

            rv += "],\"RangeProof\":" + RangeProof.Readable();
            rv += $",\"Fee\":{Fee}";
            rv += ",\"Signatures\":[";

            for (int i = 0; i < Signatures.Length; i++)
            {
                rv += Signatures[i].Readable();

                if (i < Signatures.Length - 1)
                {
                    rv += ",";
                }
            }

            rv += "],\"PseudoOutputs\":[";

            for (int i = 0; i < PseudoOutputs.Length; i++)
            {
                rv += $"\"{PseudoOutputs[i].ToHex()}\"";

                if (i < PseudoOutputs.Length - 1)
                {
                    rv += ",";
                }
            }

            rv += $"],\"ExtraLen\":{ExtraLen},\"Extra\":[";

            for (int i = 0; i < Extra.Length; i++)
            {
                rv += $"{Extra[i]}";

                if (i < Extra.Length - 1)
                {
                    rv += ",";
                }
            }

            rv += "]}";

            return rv;
        }

        public void Unmarshal(byte[] bytes)
        {
            Version = bytes[0];
            NumInputs = bytes[1];
            NumOutputs = bytes[2];
            NumSigs = bytes[3];

            uint offset = 4;

            Inputs = new TXInput[NumInputs];

            for (int i = 0; i < NumInputs; i++)
            {
                Inputs[i] = new TXInput();

                Inputs[i].Unmarshal(bytes, offset);
                offset += TXInput.Size();
            }

            Outputs = new TXOutput[NumOutputs];

            for (int i = 0; i < NumOutputs; i++)
            {
                Outputs[i] = new TXOutput();

                Outputs[i].TXUnmarshal(bytes, offset);
                offset += 72;
            }

            RangeProof = new Bulletproof();

            RangeProof.Unmarshal(bytes, offset);
            offset += RangeProof.Size();

            byte[] fee = new byte[8];

            Array.Copy(bytes, offset, fee, 0, 8);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(fee);
            }

            Fee = BitConverter.ToUInt64(fee);
            offset += 8;

            Signatures = new Triptych[NumSigs];

            for (int i = 0; i < NumSigs; i++)
            {
                Signatures[i] = new Triptych();

                Signatures[i].Unmarshal(bytes, offset);
                offset += Triptych.Size();
            }

            PseudoOutputs = new Cipher.Key[NumSigs];

            for (int i = 0; i < NumSigs; i++)
            {
                PseudoOutputs[i] = new Cipher.Key(new byte[32]);

                Array.Copy(bytes, offset, PseudoOutputs[i].bytes, 0, 32);
                offset += 32;
            }

            byte[] extraLen = new byte[4];

            Array.Copy(bytes, offset, extraLen, 0, 4);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(extraLen);
            }

            ExtraLen = BitConverter.ToUInt32(extraLen);
            offset += 4;

            Extra = new byte[ExtraLen];
            Array.Copy(bytes, offset, Extra, 0, ExtraLen);
        }

        public void Unmarshal(byte[] bytes, uint offset)
        {
            Version = bytes[offset];
            NumInputs = bytes[offset + 1];
            NumOutputs = bytes[offset + 2];
            NumSigs = bytes[offset + 3];

            offset += 4;

            Inputs = new TXInput[NumInputs];

            for (int i = 0; i < NumInputs; i++)
            {
                Inputs[i] = new TXInput();

                Inputs[i].Unmarshal(bytes, offset);
                offset += TXInput.Size();
            }

            Outputs = new TXOutput[NumOutputs];

            for (int i = 0; i < NumOutputs; i++)
            {
                Outputs[i] = new TXOutput();

                Outputs[i].TXUnmarshal(bytes, offset);
                offset += 72;
            }

            RangeProof = new Bulletproof();

            RangeProof.Unmarshal(bytes, offset);
            offset += RangeProof.Size();

            byte[] fee = new byte[8];

            Array.Copy(bytes, offset, fee, 0, 8);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(fee);
            }

            Fee = BitConverter.ToUInt64(fee);
            offset += 8;

            Signatures = new Triptych[NumSigs];

            for (int i = 0; i < NumSigs; i++)
            {
                Signatures[i] = new Triptych();

                Signatures[i].Unmarshal(bytes, offset);
                offset += Triptych.Size();
            }

            PseudoOutputs = new Cipher.Key[NumSigs];

            for (int i = 0; i < NumSigs; i++)
            {
                PseudoOutputs[i] = new Cipher.Key(new byte[32]);

                Array.Copy(bytes, offset, PseudoOutputs[i].bytes, 0, 32);
                offset += 32;
            }

            byte[] extraLen = new byte[4];
            
            Array.Copy(bytes, offset, extraLen, 0, 4);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(extraLen);
            }

            ExtraLen = BitConverter.ToUInt32(extraLen);
            offset += 4;

            Extra = new byte[ExtraLen];
            Array.Copy(bytes, offset, Extra, 0, ExtraLen);
        }

        public uint Size()
        {
            return (uint)(4 + TXInput.Size() * Inputs.Length + 72 * Outputs.Length + RangeProof.Size() + 8 + Triptych.Size() * Signatures.Length + PseudoOutputs.Length * 32 + 4 + Extra.Length);
        }

        public static Transaction GenerateMock()
        {
            Transaction tx = new Transaction();
            tx.Version = 1;
            tx.NumInputs = 2;
            tx.NumOutputs = 2;
            tx.NumSigs = 2;

            tx.Inputs = new TXInput[2];
            tx.Outputs = new TXOutput[2];
            tx.Signatures = new Triptych[2];
            tx.PseudoOutputs = new Cipher.Key[2];

            for (int i = 0; i < 2; i++)
            {
                tx.Inputs[i] = TXInput.GenerateMock();
                tx.Outputs[i] = TXOutput.GenerateMock();
                tx.Signatures[i] = Triptych.GenerateMock();
                tx.PseudoOutputs[i] = Cipher.KeyOps.GeneratePubkey();
            }

            tx.RangeProof = Bulletproof.GenerateMock();


            var buffer = new byte[8];
            new Random().NextBytes(buffer);
            tx.Fee = BitConverter.ToUInt64(buffer, 0);

            tx.ExtraLen = 34;
            
            Cipher.Key key = Cipher.KeyOps.GeneratePubkey();

            tx.Extra = new byte[34];
            tx.Extra[0] = 1;
            tx.Extra[1] = 34;

            Array.Copy(key.bytes, 0, tx.Extra, 2, 32);

            return tx;
        }
    }
}
