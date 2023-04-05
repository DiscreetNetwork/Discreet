using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;
using System.IO;
using Discreet.Common;
using Discreet.Common.Exceptions;
using Discreet.Common.Serialize;
using System.Text.Json;

namespace Discreet.Coin.Models
{
    public class Bulletproof : IHashable
    {
        public uint size;

        public Key A, S, T1, T2, taux, mu;
        public Key[] L, R;
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

        public void Serialize(BEBinaryWriter writer)
        {
            writer.Write(size);
            writer.WriteKey(A);
            writer.WriteKey(S);
            writer.WriteKey(T1);
            writer.WriteKey(T2);
            writer.WriteKey(taux);
            writer.WriteKey(mu);
            writer.WriteKeyArray(L);
            writer.WriteKeyArray(R);
            writer.WriteKey(a);
            writer.WriteKey(b);
            writer.WriteKey(t);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            size = reader.ReadUInt32();
            int _size = size > 8 ? 10 : size > 4 ? 9 : size > 2 ? 8 : size > 1 ? 7 : 6;
            A = reader.ReadKey();
            S = reader.ReadKey();
            T1 = reader.ReadKey();
            T2 = reader.ReadKey();
            taux = reader.ReadKey();
            mu = reader.ReadKey();
            L = reader.ReadKeyArray(_size);
            R = reader.ReadKeyArray(_size);
            a = reader.ReadKey();
            b = reader.ReadKey();
            t = reader.ReadKey();
        }

        public string Readable()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new Converters.BulletproofConverter());

            return JsonSerializer.Serialize(this, typeof(Bulletproof), options);
        }

        public uint GetSize()
        {
            return (uint)L.Length * 64 + 9 * 32 + 4;
        }

        public int Size => (int)GetSize();

        /* needs to recover the commitments from tx's outputs */
        public VerifyException Verify(Transaction tx)
        {
            Cipher.Bulletproof bp = new Cipher.Bulletproof(this, tx.GetCommitments());

            if (!Cipher.Bulletproof.Verify(bp))
            {
                return new VerifyException("Bulletproof", "Bulletproof is invalid!");
            }

            return null;
        }

        public VerifyException Verify(FullTransaction tx)
        {
            Cipher.Bulletproof bp = new Cipher.Bulletproof(this, tx.GetCommitments());

            if (!Cipher.Bulletproof.Verify(bp))
            {
                return new VerifyException("Bulletproof", "Bulletproof is invalid!");
            }

            return null;
        }
    }
}
