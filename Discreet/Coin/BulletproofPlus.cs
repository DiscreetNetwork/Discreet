using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;

namespace Discreet.Coin
{
    [StructLayout(LayoutKind.Sequential)]
    public class BulletproofPlus : ICoin
    {
        [MarshalAs(UnmanagedType.U4)]
        public uint size;

        [MarshalAs(UnmanagedType.Struct)]
        public Key A, A1, B, r1, s1, d1;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct)]
        public Key[] L, R;

        public BulletproofPlus() { }

        public BulletproofPlus(Cipher.BulletproofPlus bp)
        {
            A = bp.A;
            A1 = bp.A1;
            B = bp.B;
            r1 = bp.r1;
            s1 = bp.s1;
            d1 = bp.d1;

            L = new Key[KeyOps.KeyArrayLength(bp.L)];
            R = new Key[KeyOps.KeyArrayLength(bp.R)];

            for (int i = 0; i < KeyOps.KeyArrayLength(bp.L); i++)
            {
                L[i] = bp.L[i];
                R[i] = bp.R[i];
            }

            size = (uint)bp.Size();
        }

        public SHA256 Hash()
        {
            return SHA256.HashData(Marshal());
        }

        public byte[] Marshal()
        {
            byte[] bytes = new byte[L.Length * 64 + 6 * 32 + 4];
            byte[] sz = BitConverter.GetBytes(size);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(sz);
            }

            Array.Copy(sz, 0, bytes, 0, 4);
            Array.Copy(A.bytes, 0, bytes, 4, 32);
            Array.Copy(A1.bytes, 0, bytes, 4 + 32, 32);
            Array.Copy(B.bytes, 0, bytes, 4 + 32 * 2, 32);
            Array.Copy(r1.bytes, 0, bytes, 4 + 32 * 3, 32);
            Array.Copy(s1.bytes, 0, bytes, 4 + 32 * 4, 32);
            Array.Copy(d1.bytes, 0, bytes, 4 + 32 * 5, 32);

            for (int i = 0; i < L.Length; i++)
            {
                Array.Copy(L[i].bytes, 0, bytes, 4 + 32 * 6 + 32 * i, 32);
            }

            for (int i = 0; i < R.Length; i++)
            {
                Array.Copy(R[i].bytes, 0, bytes, 4 + 32 * 6 + R.Length * 32 + 32 * i, 32);
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
            string rv = "{";
            rv += $"\"size\":{size},";
            rv += $"\"A\":\"{A.ToHex()}\",";
            rv += $"\"A1\":\"{A1.ToHex()}\",";
            rv += $"\"B\":\"{B.ToHex()}\",";
            rv += $"\"r1\":\"{r1.ToHex()}\",";
            rv += $"\"s1\":\"{s1.ToHex()}\",";
            rv += $"\"d1\":\"{d1.ToHex()}\",";
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

            rv += "]}";

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

            A = new Key(new byte[32]);
            A1 = new Key(new byte[32]);
            B = new Key(new byte[32]);
            r1 = new Key(new byte[32]);
            s1 = new Key(new byte[32]);
            d1 = new Key(new byte[32]);

            Array.Copy(bytes, 4, A.bytes, 0, 32);
            Array.Copy(bytes, 4 + 32, A1.bytes, 0, 32);
            Array.Copy(bytes, 4 + 32 * 2, B.bytes, 0, 32);
            Array.Copy(bytes, 4 + 32 * 3, r1.bytes, 0, 32);
            Array.Copy(bytes, 4 + 32 * 4, s1.bytes, 0, 32);
            Array.Copy(bytes, 4 + 32 * 5, d1.bytes, 0, 32);

            L = new Key[size];
            R = new Key[size];

            for (int i = 0; i < size; i++)
            {
                L[i] = new Key(new byte[32]);
                Array.Copy(bytes, 4 + 32 * 6 + 32 * i, L[i].bytes, 0, 32);
            }

            for (int i = 0; i < size; i++)
            {
                R[i] = new Key(new byte[32]);
                Array.Copy(bytes, 4 + 32 * 6 + L.Length * 32 + 32 * i, R[i].bytes, 0, 32);
            }
        }

        public uint Unmarshal(byte[] bytes, uint offset)
        {
            byte[] sz = new byte[4];
            Array.Copy(bytes, offset, sz, 0, 4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(sz);
            }

            size = BitConverter.ToUInt32(sz);

            A = new Key(new byte[32]);
            A1 = new Key(new byte[32]);
            B = new Key(new byte[32]);
            r1 = new Key(new byte[32]);
            s1 = new Key(new byte[32]);
            d1 = new Key(new byte[32]);

            Array.Copy(bytes, offset + 4, A.bytes, 0, 32);
            Array.Copy(bytes, offset + 4 + 32, A1.bytes, 0, 32);
            Array.Copy(bytes, offset + 4 + 32 * 2, B.bytes, 0, 32);
            Array.Copy(bytes, offset + 4 + 32 * 3, r1.bytes, 0, 32);
            Array.Copy(bytes, offset + 4 + 32 * 4, s1.bytes, 0, 32);
            Array.Copy(bytes, offset + 4 + 32 * 5, d1.bytes, 0, 32);

            L = new Key[size];
            R = new Key[size];

            for (int i = 0; i < size; i++)
            {
                L[i] = new Key(new byte[32]);
                Array.Copy(bytes, offset + 4 + 32 * 6 + 32 * i, L[i].bytes, 0, 32);
            }

            for (int i = 0; i < size; i++)
            {
                R[i] = new Key(new byte[32]);
                Array.Copy(bytes, offset + 4 + 32 * 6 + L.Length * 32 + 32 * i, R[i].bytes, 0, 32);
            }

            return offset + Size();
        }

        public uint Size()
        {
            return (uint)L.Length * 64 + 6 * 32 + 4;
        }

        /* needs to recover the commitments from tx's outputs */
        public VerifyException Verify(Transaction tx)
        {
            Cipher.BulletproofPlus bp = new Cipher.BulletproofPlus(this, tx.GetCommitments());

            if (!Cipher.BulletproofPlus.Verify(bp))
            {
                return new VerifyException("BulletproofPlus", "BulletproofPlus is invalid!");
            }

            return null;
        }

        /* UNUSED. Use Verify(Transaction tx) instead */
        public VerifyException Verify()
        {
            return null;
        }
    }
}
