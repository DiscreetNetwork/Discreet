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
            throw new NotImplementedException();
        }

        public void Unmarshal(byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public uint Unmarshal(byte[] bytes, uint offset)
        {
            throw new NotImplementedException();
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
