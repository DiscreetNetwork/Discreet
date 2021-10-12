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

        public Bulletproof() { }

        public Bulletproof(Cipher.Bulletproof bp)
        {
            A = bp.A;
            S = bp.S;
            T1 = bp.T1;
            T2 = bp.T2;
            taux = bp.taux;
            mu = bp.mu;

            L = new Key[KeyOps.KeyArrayLength(bp.L)];
            R = new Key[KeyOps.KeyArrayLength(bp.R)];

            for (int i = 0; i < KeyOps.KeyArrayLength(bp.L); i++)
            {
                L[i] = bp.L[i];
                R[i] = bp.R[i];
            }
            
            a = bp.a;
            b = bp.b;
            t = bp.t;

            size = (uint)bp.Size();
        }

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

            for (int i = 0; i < L.Length; i++)
            {
                Array.Copy(L[i].bytes, 0, bytes, 4 + 32 * 6 + 32 * i, 32);
            }

            for (int i = 0; i < R.Length; i++)
            {
                Array.Copy(R[i].bytes, 0, bytes, 4 + 32 * 6 + R.Length * 32 + 32 * i, 32);
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
            rv += $"\"size\":{size},";
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

            A = new Key(new byte[32]);
            S = new Key(new byte[32]);
            T1 = new Key(new byte[32]);
            T2 = new Key(new byte[32]);
            taux = new Key(new byte[32]);
            mu = new Key(new byte[32]);

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
                L[i] = new Key(new byte[32]);
                Array.Copy(bytes, 4 + 32 * 6 + 32 * i, L[i].bytes, 0, 32);
            }

            for (int i = 0; i < size; i++)
            {
                R[i] = new Key(new byte[32]);
                Array.Copy(bytes, 4 + 32 * 6 + L.Length * 32 + 32 * i, R[i].bytes, 0, 32);
            }

            a = new Key(new byte[32]);
            b = new Key(new byte[32]);
            t = new Key(new byte[32]);

            Array.Copy(bytes, 4 + 32 * 6 + 2 * L.Length * 32, a.bytes, 0, 32);
            Array.Copy(bytes, 4 + 32 * 6 + 2 * L.Length * 32 + 32, b.bytes, 0, 32);
            Array.Copy(bytes, 4 + 32 * 6 + 2 * L.Length * 32 + 64, t.bytes, 0, 32);
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
            S = new Key(new byte[32]);
            T1 = new Key(new byte[32]);
            T2 = new Key(new byte[32]);
            taux = new Key(new byte[32]);
            mu = new Key(new byte[32]);

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
                L[i] = new Key(new byte[32]);
                Array.Copy(bytes, offset + 4 + 32 * 6 + 32 * i, L[i].bytes, 0, 32);
            }

            for (int i = 0; i < size; i++)
            {
                R[i] = new Key(new byte[32]);
                Array.Copy(bytes, offset + 4 + 32 * 6 + L.Length * 32 + 32 * i, R[i].bytes, 0, 32);
            }

            a = new Key(new byte[32]);
            b = new Key(new byte[32]);
            t = new Key(new byte[32]);

            Array.Copy(bytes, offset + 4 + 32 * 6 + 2 * L.Length * 32, a.bytes, 0, 32);
            Array.Copy(bytes, offset + 4 + 32 * 6 + 2 * L.Length * 32 + 32, b.bytes, 0, 32);
            Array.Copy(bytes, offset + 4 + 32 * 6 + 2 * L.Length * 32 + 64, t.bytes, 0, 32);

            return offset + Size();
        }

        public uint Size()
        {
            return (uint)L.Length * 64 + 9 * 32 + 4;
        }

        public static Bulletproof GenerateMock()
        {
            Bulletproof bp = new Bulletproof();
            bp.size = 7;
            bp.A = Cipher.KeyOps.GeneratePubkey();
            bp.S = Cipher.KeyOps.GeneratePubkey();
            bp.T1 = Cipher.KeyOps.GeneratePubkey();
            bp.T2 = Cipher.KeyOps.GeneratePubkey();
            bp.taux = Cipher.KeyOps.GenerateSeckey();
            bp.mu = Cipher.KeyOps.GenerateSeckey();

            bp.L = new Key[7];
            bp.R = new Key[7];

            for (int i = 0; i < 7; i++)
            {
                bp.L[i] = Cipher.KeyOps.GeneratePubkey();
                bp.R[i] = Cipher.KeyOps.GeneratePubkey();
            }

            bp.a = Cipher.KeyOps.GenerateSeckey();
            bp.b = Cipher.KeyOps.GenerateSeckey();
            bp.t = Cipher.KeyOps.GenerateSeckey();

            return bp;
        }

        /* needs to recover the commitments from tx's outputs */
        public VerifyException Verify(Transaction tx)
        {
            Cipher.Bulletproof bp = new Cipher.Bulletproof(this, tx.GetCommitments());

            if(!Cipher.Bulletproof.Verify(bp))
            {
                return new VerifyException("Bulletproof", "Bulletproof is invalid!");
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
