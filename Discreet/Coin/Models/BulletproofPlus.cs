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
    public class BulletproofPlus : IHashable
    {
        public uint size;

        public Key A, A1, B, r1, s1, d1;
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

        public void Serialize(BEBinaryWriter writer)
        {
            writer.Write(size);
            writer.WriteKey(A);
            writer.WriteKey(A1);
            writer.WriteKey(B);
            writer.WriteKey(r1);
            writer.WriteKey(s1);
            writer.WriteKey(d1);
            writer.WriteKeyArray(L, false);
            writer.WriteKeyArray(R, false);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            size = reader.ReadUInt32();
            int _size = size > 8 ? 10 : size > 4 ? 9 : size > 2 ? 8 : size > 1 ? 7 : 6;
            A = reader.ReadKey();
            A1 = reader.ReadKey();
            B = reader.ReadKey();
            r1 = reader.ReadKey();
            s1 = reader.ReadKey();
            d1 = reader.ReadKey();
            L = reader.ReadKeyArray(_size);
            R = reader.ReadKeyArray(_size);
        }

        public string Readable()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new Converters.BulletproofPlusConverter());

            return JsonSerializer.Serialize(this, typeof(BulletproofPlus), options);
        }

        public uint GetSize()
        {
            return (uint)L.Length * 64 + 6 * 32 + 4;
        }

        public int Size => (int)GetSize();

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

        public VerifyException Verify(FullTransaction tx)
        {
            Cipher.BulletproofPlus bp = new Cipher.BulletproofPlus(this, tx.GetCommitments());

            if (!Cipher.BulletproofPlus.Verify(bp))
            {
                return new VerifyException("BulletproofPlus", "BulletproofPlus is invalid!");
            }

            return null;
        }
    }
}
