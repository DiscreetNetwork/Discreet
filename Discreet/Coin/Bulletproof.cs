using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;

namespace Discreet.Coin
{
    [StructLayout(LayoutKind.Sequential)]
    public class Bulletproof : ICoin
    {
        [MarshalAs(UnmanagedType.U4)]
        public uint size;

        [MarshalAs(UnmanagedType.Struct)]
        public Key A, S, T1, T2, taux, mu;

        [MarshalAs(UnmanagedType.ByValArray,  ArraySubType = UnmanagedType.Struct)]
        public Key[] L, R;

        [MarshalAs(UnmanagedType.Struct)]
        public Key a, b, t;

        public SHA256 Hash()
        {
            return SHA256.HashData(Marshal());
        }

        public byte[] Marshal()
        {
            byte[] bytes = new byte[L.Length * 64 + 9 * 32 + 4];
            byte[] sz = BitConverter.GetBytes(size);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(sz);
            }

            Array.Copy(sz, 0, bytes, 0, 4);
            Array.Copy(A.bytes, 0, bytes, 4, 32);
            Array.Copy(S.bytes, 0, bytes, 4 + 32, 32);
            Array.Copy(T1.bytes, 0, bytes, 4 + 32 * 2, 32);
            Array.Copy(T2.bytes, 0, bytes, 4 + 32 * 3, 32);
            Array.Copy(taux.bytes, 0, bytes, 4 + 32 * 4, 32);
            Array.Copy(mu.bytes, 0, bytes, 4 + 32 * 5, 32);

            for (int i = 0; i < 6; i++)
            {
                Array.Copy(L[i].bytes, 0, bytes, 4 + 32 * 6 + 32 * i, 32);
            }

            for (int i = 0; i < 6; i++)
            {
                Array.Copy(R[i].bytes, 0, bytes, 4 + 32 * 6 + L.Length * 32 + 32 * i, 32);
            }

            Array.Copy(a.bytes, 0, bytes, 4 + 32 * 6 + 2 * L.Length * 32, 32);
            Array.Copy(b.bytes, 0, bytes, 4 + 32 * 6 + 2 * L.Length * 32 + 32, 32);
            Array.Copy(t.bytes, 0, bytes, 4 + 32 * 6 + 2 * L.Length * 32 + 64, 32);

            return bytes;
        }

        public void Marshal(byte[] bytes, uint offset)
        {
            byte[] _bytes = Marshal();
            Array.Copy(_bytes, 0, bytes, offset, _bytes.Length);
        }

        public string Readable()
        {
            string rv = "{";
            rv += $"\"A\":\"{A.ToHex()}\",";
            rv += $"\"S\":\"{S.ToHex()}\",";
            rv += $"\"T1\":\"{T1.ToHex()}\",";
            rv += $"\"T2\":\"{T2.ToHex()}\",";
            rv += $"\"taux\":\"{taux.ToHex()}\",";
            rv += $"\"mu\":\"{mu.ToHex()}\",";
            rv += "\"L\":[";

            for (int i = 0; i < L.Length; i++)
            {
                rv += $"\"{L[i].ToHex()}\"";
                if (i < L.Length - 1)
                {
                    rv += ",";
                }
            }

            rv += "],\"R\":[";

            for (int i = 0; i < R.Length; i++)
            {
                rv += $"\"{R[i].ToHex()}\"";
                if (i < R.Length - 1)
                {
                    rv += ",";
                }
            }

            rv += $"],\"a\":\"{a.ToHex()}\",";
            rv += $"\"b\":\"{b.ToHex()}\",";
            rv += $"\"t\":\"{t.ToHex()}\"}}";

            return rv;
        }

        public void Unmarshal(byte[] bytes)
        {
            byte[] sz = new byte[4];
            Array.Copy(bytes, 0, sz, 0, 4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(sz);
            }

            size = BitConverter.ToUInt32(sz);

            Array.Copy(bytes, 4, A.bytes, 0, 32);
            Array.Copy(bytes, 4 + 32, S.bytes, 0, 32);
            Array.Copy(bytes, 4 + 32 * 2, T1.bytes, 0, 32);
            Array.Copy(bytes, 4 + 32 * 3, T2.bytes, 0, 32);
            Array.Copy(bytes, 4 + 32 * 4, taux.bytes, 0, 32);
            Array.Copy(bytes, 4 + 32 * 5, mu.bytes, 0, 32);

            L = new Key[size];
            R = new Key[size];

            for (int i = 0; i < size; i++)
            {
                Array.Copy(bytes, 4 + 32 * 6 + 32 * i, L[i].bytes, 0, 32);
            }

            for (int i = 0; i < size; i++)
            {
                Array.Copy(bytes, 4 + 32 * 6 + L.Length * 32 + 32 * i, R[i].bytes, 0, 32);
            }

            Array.Copy(bytes, 4 + 32 * 6 + 2 * L.Length * 32, a.bytes, 0, 32);
            Array.Copy(bytes, 4 + 32 * 6 + 2 * L.Length * 32 + 32, b.bytes, 0, 32);
            Array.Copy(bytes, 4 + 32 * 6 + 2 * L.Length * 32 + 64, t.bytes, 0, 32);
        }

        public void Unmarshal(byte[] bytes, uint offset)
        {
            byte[] sz = new byte[4];
            Array.Copy(bytes, offset, sz, 0, 4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(sz);
            }

            size = BitConverter.ToUInt32(sz);

            Array.Copy(bytes, offset + 4, A.bytes, 0, 32);
            Array.Copy(bytes, offset + 4 + 32, S.bytes, 0, 32);
            Array.Copy(bytes, offset + 4 + 32 * 2, T1.bytes, 0, 32);
            Array.Copy(bytes, offset + 4 + 32 * 3, T2.bytes, 0, 32);
            Array.Copy(bytes, offset + 4 + 32 * 4, taux.bytes, 0, 32);
            Array.Copy(bytes, offset + 4 + 32 * 5, mu.bytes, 0, 32);

            L = new Key[size];
            R = new Key[size];

            for (int i = 0; i < size; i++)
            {
                Array.Copy(bytes, offset + 4 + 32 * 6 + 32 * i, L[i].bytes, 0, 32);
            }

            for (int i = 0; i < size; i++)
            {
                Array.Copy(bytes, offset + 4 + 32 * 6 + L.Length * 32 + 32 * i, R[i].bytes, 0, 32);
            }

            Array.Copy(bytes, offset + 4 + 32 * 6 + 2 * L.Length * 32, a.bytes, 0, 32);
            Array.Copy(bytes, offset + 4 + 32 * 6 + 2 * L.Length * 32 + 32, b.bytes, 0, 32);
            Array.Copy(bytes, offset + 4 + 32 * 6 + 2 * L.Length * 32 + 64, t.bytes, 0, 32);
        }

        public uint Size()
        {
            return (uint)L.Length * 64 + 9 * 32 + 4;
        }
    }
}
