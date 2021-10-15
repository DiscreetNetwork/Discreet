using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Discreet.Cipher;

namespace Discreet.Coin.Transparent
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

        [MarshalAs(UnmanagedType.Struct)]
        public SHA256 InnerHash;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct)]
        public TXOutput[] Inputs;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct)]
        public TXOutput[] Outputs;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct)]
        public Signature[] Signatures;

        [MarshalAs(UnmanagedType.U8)]
        public ulong Fee;

        public SHA256 Hash()
        {
            return SHA256.HashData(Marshal());
        }

        public byte[] Marshal()
        {
            byte[] bytes = new byte[Size()];

            bytes[0] = Version;
            bytes[1] = NumInputs;
            bytes[2] = NumOutputs;
            bytes[3] = NumSigs;

            Array.Copy(InnerHash.Bytes, 0, bytes, 4, 32);

            uint offset = 36;

            for (int i = 0; i < Inputs.Length; i++)
            {
                Inputs[i].TXMarshal(bytes, offset);
                offset += 33;
            }

            for (int i = 0; i < Outputs.Length; i++)
            {
                Outputs[i].TXMarshal(bytes, offset);
                offset += 33;
            }

            for (int i = 0; i < Signatures.Length; i++)
            {
                Signatures[i].ToBytes(bytes, offset);
                offset += 64;
            }

            byte[] fee = BitConverter.GetBytes(Fee);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(fee);
            }

            Array.Copy(fee, 0, bytes, offset, 8);

            return bytes;
        }

        public void Marshal(byte[] bytes, uint offset)
        {
            byte[] rv = Marshal();

            Array.Copy(rv, 0, bytes, offset, rv.Length);
        }

        public string Readable()
        {
            return Discreet.Readable.Transparent.Transaction.ToReadable(this);
        }

        public static Transaction FromReadable(string json)
        {
            return Discreet.Readable.Transparent.Transaction.FromReadable(json);
        }

        public void Unmarshal(byte[] bytes)
        {
            Version = bytes[0];
            NumInputs = bytes[1];
            NumOutputs = bytes[2];
            NumSigs = bytes[3];

            InnerHash = new SHA256(new byte[32], false);
            Array.Copy(bytes, 4, InnerHash.Bytes, 0, 32);

            uint offset = 36;

            Inputs = new TXOutput[NumInputs];

            for (int i = 0; i < Inputs.Length; i++)
            {
                Inputs[i] = new TXOutput();
                Inputs[i].TXUnmarshal(bytes, offset);
                offset += 33;
            }

            Outputs = new TXOutput[NumOutputs];

            for (int i = 0; i < Inputs.Length; i++)
            {
                Outputs[i] = new TXOutput();
                Outputs[i].TXUnmarshal(bytes, offset);
                offset += 33;
            }

            Signatures = new Signature[NumSigs];

            for (int i = 0; i < Signatures.Length; i++)
            {
                Signatures[i] = new Signature(bytes, offset);
                offset += 64;
            }

            byte[] fee = new byte[8];
            Array.Copy(bytes, offset, fee, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(fee);
            }

            Fee = BitConverter.ToUInt64(fee);
        }

        public uint Unmarshal(byte[] bytes, uint offset)
        {
            Version = bytes[offset];
            NumInputs = bytes[offset + 1];
            NumOutputs = bytes[offset + 2];
            NumSigs = bytes[offset + 3];

            InnerHash = new SHA256(new byte[32], false);
            Array.Copy(bytes, offset + 4, InnerHash.Bytes, 0, 32);

            offset += 36;

            Inputs = new TXOutput[NumInputs];

            for (int i = 0; i < Inputs.Length; i++)
            {
                Inputs[i] = new TXOutput();
                Inputs[i].TXUnmarshal(bytes, offset);
                offset += 33;
            }

            Outputs = new TXOutput[NumOutputs];

            for (int i = 0; i < Inputs.Length; i++)
            {
                Outputs[i] = new TXOutput();
                Outputs[i].TXUnmarshal(bytes, offset);
                offset += 33;
            }

            Signatures = new Signature[NumSigs];

            for (int i = 0; i < Signatures.Length; i++)
            {
                Signatures[i] = new Signature(bytes, offset);
                offset += 64;
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

        public uint Size()
        {
            return (uint)(44 + 33 * (Inputs.Length + Outputs.Length) + 64 * Signatures.Length);
        }

        public VerifyException Verify()
        {
            throw new NotImplementedException();
        }
    }
}
