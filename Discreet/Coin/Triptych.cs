using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;
using System.IO;

namespace Discreet.Coin
{
    [StructLayout(LayoutKind.Sequential)]
    public class Triptych : ICoin
    {
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
            return SHA256.HashData(Serialize());
        }

        public Triptych() { }

        public Triptych(Cipher.Triptych proof)
        {
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

        public byte[] Serialize()
        {
            byte[] bytes = new byte[Size()];

            Array.Copy(K.bytes, 0, bytes, 0, 32);
            Array.Copy(A.bytes, 0, bytes, 32, 32);
            Array.Copy(B.bytes, 0, bytes, 32 * 2, 32);
            Array.Copy(C.bytes, 0, bytes, 32 * 3, 32);
            Array.Copy(D.bytes, 0, bytes, 32 * 4, 32);

            for (int i = 0; i < 6; i++)
            {
                Array.Copy(X[i].bytes, 0, bytes, 32 * 5 + 32 * i, 32);
            }

            for (int i = 0; i < 6; i++)
            {
                Array.Copy(Y[i].bytes, 0, bytes, 32 * 11 + 32 * i, 32);
            }

            for (int i = 0; i < 6; i++)
            {
                Array.Copy(f[i].bytes, 0, bytes, 32 * 17 + 32 * i, 32);
            }

            Array.Copy(zA.bytes, 0, bytes, 23 * 32, 32);
            Array.Copy(zC.bytes, 0, bytes, 24 * 32, 32);
            Array.Copy(z.bytes, 0, bytes, 25 * 32, 32);

            return bytes;
        }

        public void Serialize(byte[] bytes, uint offset)
        {
            byte[] _bytes = Serialize();
            Array.Copy(_bytes, 0, bytes, offset, _bytes.Length);
        }

        public string Readable()
        {
            return Discreet.Readable.Triptych.ToReadable(this);
        }

        public static Triptych FromReadable(string json)
        {
            return Discreet.Readable.Triptych.FromReadable(json);
        }

        public void Deserialize(byte[] bytes)
        {
            Deserialize(bytes, 0);
        }

        public uint Deserialize(byte[] bytes, uint offset)
        {
            K = new Key(bytes, offset);
            A = new Key(bytes, offset + 32);
            B = new Key(bytes, offset + 32 * 2);
            C = new Key(bytes, offset + 32 * 3);
            D = new Key(bytes, offset + 32 * 4);

            X = new Key[6];
            Y = new Key[6];
            f = new Key[6];

            for (int i = 0; i < 6; i++)
            {
                X[i] = new Key(bytes, offset + 32 * 5 + 32 * (uint)i);
            }

            for (int i = 0; i < 6; i++)
            {
                Y[i] = new Key(bytes, offset + 32 * 11 + 32 * (uint)i);
            }

            for (int i = 0; i < 6; i++)
            {
                f[i] = new Key(bytes, offset + 32 * 17 + 32 * (uint)i);
            }

            zA = new Key(bytes, offset + 23 * 32);
            zC = new Key(bytes, offset + 24 * 32);
            z = new Key(bytes, offset + 25 * 32);

            return offset + Size();
        }

        public void Serialize(Stream s)
        {
            s.Write(K.bytes);
            s.Write(A.bytes);
            s.Write(B.bytes);
            s.Write(C.bytes);
            s.Write(D.bytes);

            for (int i = 0; i < 6; i++)
            {
                s.Write(X[i].bytes);
            }

            for (int i = 0; i < 6; i++)
            {
                s.Write(Y[i].bytes);
            }

            for (int i = 0; i < 6; i++)
            {
                s.Write(f[i].bytes);
            }

            s.Write(zA.bytes);
            s.Write(zC.bytes);
            s.Write(z.bytes);
        }

        public void Deserialize(Stream s)
        {
            K = new Key(s);
            A = new Key(s);
            B = new Key(s);
            C = new Key(s);
            D = new Key(s);

            X = new Key[6];
            Y = new Key[6];
            f = new Key[6];

            for (int i = 0; i < 6; i++)
            {
                X[i] = new Key(s);
            }

            for (int i = 0; i < 6; i++)
            {
                Y[i] = new Key(s);
            }

            for (int i = 0; i < 6; i++)
            {
                f[i] = new Key(s);
            }

            zA = new Key(s);
            zC = new Key(s);
            z = new Key(s);
        }

        public static uint Size()
        {
            return 18*32 + 8*32;
        }

        public static Triptych GenerateMock()
        {
            Triptych proof = new Triptych();

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

        public VerifyException Verify(Key[] M, Key[] P, Key C_offset, Key message, Key linkingTag)
        {
            Cipher.Triptych proof = new Cipher.Triptych(this, linkingTag);

            if (!Cipher.Triptych.Verify(proof, M, P, C_offset, message))
            {
                return new VerifyException("Triptych", "Triptych proof is invalid!");
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
