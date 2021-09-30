using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;

namespace Discreet.Coin
{
    [StructLayout(LayoutKind.Sequential)]
    public class Triptych : ICoin
    {
        [MarshalAs(UnmanagedType.Struct)]
        public Key J;

        [MarshalAs(UnmanagedType.Struct)]
        public Key K;

        [MarshalAs(UnmanagedType.Struct)]
        public Key A, B, C, D;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.Struct)]
        public Key[] X, Y, f;

        [MarshalAs(UnmanagedType.Struct)]
        public Key zA, zC, z;

        public SHA256 Hash()
        {
            return SHA256.HashData(Marshal());
        }

        public Triptych() { }

        public Triptych(Cipher.Triptych proof)
        {
            J = proof.J;
            K = proof.K;
            A = proof.A;
            B = proof.B;
            C = proof.C;
            D = proof.D;

            X = proof.X;
            Y = proof.Y;
            f = proof.f;

            zA = proof.zA;
            zC = proof.zC;
            z = proof.z;
        }

        public byte[] Marshal()
        {
            byte[] bytes = new byte[Size()];

            Array.Copy(J.bytes, 0, bytes, 0, 32);
            Array.Copy(K.bytes, 0, bytes, 32, 32);
            Array.Copy(A.bytes, 0, bytes, 32 * 2, 32);
            Array.Copy(B.bytes, 0, bytes, 32 * 3, 32);
            Array.Copy(C.bytes, 0, bytes, 32 * 4, 32);
            Array.Copy(D.bytes, 0, bytes, 32 * 5, 32);

            for (int i = 0; i < 6; i++)
            {
                Array.Copy(X[i].bytes, 0, bytes, 32 * 6 + 32 * i, 32);
            }

            for (int i = 0; i < 6; i++)
            {
                Array.Copy(Y[i].bytes, 0, bytes, 32 * 12 + 32 * i, 32);
            }

            for (int i = 0; i < 6; i++)
            {
                Array.Copy(f[i].bytes, 0, bytes, 32 * 18 + 32 * i, 32);
            }

            Array.Copy(zA.bytes, 0, bytes, 24 * 32, 32);
            Array.Copy(zC.bytes, 0, bytes, 25 * 32, 32);
            Array.Copy(z.bytes, 0, bytes, 26 * 32, 32);

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
            rv += $"\"J\":\"{J.ToHex()}\",";
            rv += $"\"K\":\"{K.ToHex()}\",";
            rv += $"\"A\":\"{A.ToHex()}\",";
            rv += $"\"B\":\"{B.ToHex()}\",";
            rv += $"\"C\":\"{C.ToHex()}\",";
            rv += $"\"D\":\"{D.ToHex()}\",";
            rv += "\"X\":[";

            for (int i = 0; i < 6; i++)
            {
                rv += $"\"{X[i].ToHex()}\"";
                if (i < 5)
                {
                    rv += ",";
                }
            }

            rv += "],\"Y\":[";

            for (int i = 0; i < 6; i++)
            {
                rv += $"\"{Y[i].ToHex()}\"";
                if (i < 5)
                {
                    rv += ",";
                }
            }

            rv += "],\"f\":[";

            for (int i = 0; i < 6; i++)
            {
                rv += $"\"{f[i].ToHex()}\"";
                if (i < 5)
                {
                    rv += ",";
                }
            }

            rv += $"],\"zA\":\"{zA.ToHex()}\",";
            rv += $"\"zC\":\"{zC.ToHex()}\",";
            rv += $"\"z\":\"{z.ToHex()}\"}}";

            return rv;
        }

        public void Unmarshal(byte[] bytes)
        {
            J = new Key(new byte[32]);
            K = new Key(new byte[32]);
            A = new Key(new byte[32]);
            B = new Key(new byte[32]);
            C = new Key(new byte[32]);
            D = new Key(new byte[32]);

            Array.Copy(bytes, 0, J.bytes, 0, 32);
            Array.Copy(bytes, 32, K.bytes, 0, 32);
            Array.Copy(bytes, 32 * 2, A.bytes, 0, 32);
            Array.Copy(bytes, 32 * 3, B.bytes, 0, 32);
            Array.Copy(bytes, 32 * 4, C.bytes, 0, 32);
            Array.Copy(bytes, 32 * 5, D.bytes, 0, 32);

            X = new Key[6];
            Y = new Key[6];
            f = new Key[6];

            for (int i = 0; i < 6; i++)
            {
                X[i] = new Key(new byte[32]);

                Array.Copy(bytes, 32 * 6 + 32 * i, X[i].bytes, 0, 32);
            }

            for (int i = 0; i < 6; i++)
            {
                Y[i] = new Key(new byte[32]);
                Array.Copy(bytes, 32 * 12 + 32 * i, Y[i].bytes, 0, 32);
            }

            for (int i = 0; i < 6; i++)
            {
                f[i] = new Key(new byte[32]);
                Array.Copy(bytes, 32 * 18 + 32 * i, f[i].bytes, 0, 32);
            }

            zA = new Key(new byte[32]);
            zC = new Key(new byte[32]);
            z = new Key(new byte[32]);

            Array.Copy(bytes, 24 * 32, zA.bytes, 0, 32);
            Array.Copy(bytes, 25 * 32, zC.bytes, 0, 32);
            Array.Copy(bytes, 26 * 32, z.bytes, 0, 32);
        }

        public uint Unmarshal(byte[] bytes, uint offset)
        {
            J = new Key(new byte[32]);
            K = new Key(new byte[32]);
            A = new Key(new byte[32]);
            B = new Key(new byte[32]);
            C = new Key(new byte[32]);
            D = new Key(new byte[32]);

            Array.Copy(bytes, offset, J.bytes, 0, 32);
            Array.Copy(bytes, offset + 32, K.bytes, 0, 32);
            Array.Copy(bytes, offset + 32 * 2, A.bytes, 0, 32);
            Array.Copy(bytes, offset + 32 * 3, B.bytes, 0, 32);
            Array.Copy(bytes, offset + 32 * 4, C.bytes, 0, 32);
            Array.Copy(bytes, offset + 32 * 5, D.bytes, 0, 32);

            X = new Key[6];
            Y = new Key[6];
            f = new Key[6];

            for (int i = 0; i < 6; i++)
            {
                X[i] = new Key(new byte[32]);

                Array.Copy(bytes, offset + 32 * 6 + 32 * i, X[i].bytes, 0, 32);
            }

            for (int i = 0; i < 6; i++)
            {
                Y[i] = new Key(new byte[32]);
                Array.Copy(bytes, offset + 32 * 12 + 32 * i, Y[i].bytes, 0, 32);
            }

            for (int i = 0; i < 6; i++)
            {
                f[i] = new Key(new byte[32]);
                Array.Copy(bytes, offset + 32 * 18 + 32 * i, f[i].bytes, 0, 32);
            }

            zA = new Key(new byte[32]);
            zC = new Key(new byte[32]);
            z = new Key(new byte[32]);

            Array.Copy(bytes, offset + 24 * 32, zA.bytes, 0, 32);
            Array.Copy(bytes, offset + 25 * 32, zC.bytes, 0, 32);
            Array.Copy(bytes, offset + 26 * 32, z.bytes, 0, 32);

            return offset + Size();
        }

        public static uint Size()
        {
            return 18*32 + 9*32;
        }

        public static Triptych GenerateMock()
        {
            Triptych proof = new Triptych();

            proof.J = Cipher.KeyOps.GeneratePubkey();
            proof.K = Cipher.KeyOps.GeneratePubkey();
            proof.A = Cipher.KeyOps.GeneratePubkey();
            proof.B = Cipher.KeyOps.GeneratePubkey();
            proof.C = Cipher.KeyOps.GeneratePubkey();
            proof.D = Cipher.KeyOps.GeneratePubkey();

            proof.X = new Key[6];
            proof.Y = new Key[6];
            proof.f = new Key[6];

            for (int i = 0; i < 6; i++)
            {
                proof.X[i] = Cipher.KeyOps.GeneratePubkey();
                proof.Y[i] = Cipher.KeyOps.GeneratePubkey();
                proof.f[i] = Cipher.KeyOps.GenerateSeckey();
            }

            proof.zA = Cipher.KeyOps.GenerateSeckey();
            proof.zC = Cipher.KeyOps.GenerateSeckey();
            proof.z = Cipher.KeyOps.GenerateSeckey();

            return proof;
        }

        public VerifyException Verify(Key[] M, Key[] P, Key C_offset, Key message)
        {
            Cipher.Triptych proof = new Cipher.Triptych(this);

            if (!Cipher.Triptych.Verify(proof, M, P, C_offset, message))
            {
                throw new VerifyException("Triptych", "Triptych proof is invalid!");
            }

            return null;
        }

        /* UNUSED. Use Verify(Key[] M, Key[] P, Key C_offset, Key message) instead */
        public VerifyException Verify()
        {
            return null;
        }
    }
}
