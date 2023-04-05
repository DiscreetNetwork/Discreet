using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;
using System.IO;
using Discreet.Common.Exceptions;
using Discreet.Common.Serialize;

namespace Discreet.Coin.Models
{
    public class Triptych : IHashable
    {
        public Key K;

        public Key A, B, C, D;
        public Key[] X, Y, f;
        public Key zA, zC, z;

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

        public void Serialize(BEBinaryWriter writer)
        {
            writer.WriteKey(K);
            writer.WriteKey(A);
            writer.WriteKey(B);
            writer.WriteKey(C);
            writer.WriteKey(D);
            writer.WriteKeyArray(X, false);
            writer.WriteKeyArray(Y, false);
            writer.WriteKeyArray(f, false);
            writer.WriteKey(zA);
            writer.WriteKey(zC);
            writer.WriteKey(z);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            K = reader.ReadKey();
            A = reader.ReadKey();
            B = reader.ReadKey();
            C = reader.ReadKey();
            D = reader.ReadKey();
            X = reader.ReadKeyArray(6);
            Y = reader.ReadKeyArray(6);
            f = reader.ReadKeyArray(6);
            zA = reader.ReadKey();
            zC = reader.ReadKey();
            z = reader.ReadKey();
        }

        public string Readable()
        {
            return Discreet.Readable.Triptych.ToReadable(this);
        }

        public object ToReadable()
        {
            return new Readable.Triptych(this);
        }

        public static Triptych FromReadable(string json)
        {
            return Discreet.Readable.Triptych.FromReadable(json);
        }

        public static uint GetSize()
        {
            return 18 * 32 + 8 * 32;
        }

        public int Size => (int)GetSize();
 
        public VerifyException Verify(Key[] M, Key[] P, Key C_offset, Key message, Key linkingTag)
        {
            Cipher.Triptych proof = new Cipher.Triptych(this, linkingTag);

            if (!Cipher.Triptych.Verify(proof, M, P, C_offset, message))
            {
                return new VerifyException("Triptych", "Triptych proof is invalid!");
            }

            return null;
        }
    }
}
