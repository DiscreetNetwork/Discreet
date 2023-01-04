using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;
using System.IO;

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

        /// <summary>
        /// Bulletproof creates a Coin.Bulletproof instance that copies the
        /// values of the fields of a Cipher.Bulletproof instance. This instance
        /// is better suited to be used in the Coin namespace.
        /// </summary>
        /// <param name="bp">An instance of Cipher.Bulletproof.</param>
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

        /// <summary>
        /// Hash creates a SHA-256 hash of the serialization of the bulletproof.
        /// </summary>
        /// <returns>The hash of the bulletproof.</returns>
        public SHA256 Hash()
        {
            return SHA256.HashData(Serialize());
        }

        /// <summary>
        /// Serialize creates a byte array equivalent of the bulletproof
        /// instance.
        /// </summary>
        /// <returns>A byte array representing the serialized
        /// bulletproof.</returns>
        public byte[] Serialize()
        {
            byte[] bytes = new byte[L.Length * 64 + 9 * 32 + 4];

            Serialization.CopyData(bytes, 0, size);

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

        /// <summary>
        /// Serialize copies this Bulletproof's serialized representation to a
        /// given byte array, starting at an offset.
        /// </summary>
        /// <param name="bytes">A byte array that will hold a copy of the
        /// bulletproof serialization.</param>
        /// <param name="offset">The offset at which we will copy the
        /// bulletproof serialization.</param>
        public void Serialize(byte[] bytes, uint offset)
        {
            byte[] _bytes = Serialize();
            Array.Copy(_bytes, 0, bytes, offset, _bytes.Length);
        }

        /// <summary>
        /// Readable encodes the bulletproof to a string containing an
        /// equivalent JSON.
        /// </summary>
        public string Readable()
        {
            return Discreet.Readable.Bulletproof.ToReadable(this);
        }

        /// <summary>
        /// ToReadable converts the bulletproof to an object containing the
        /// bulletproof in JSON format.
        /// </summary>
        /// <returns>A readable bulletproof.</returns>
        public object ToReadable()
        {
            return new Discreet.Readable.Bulletproof(this);
        }

        /// <summary>
        /// FromReadable returns a BlockHeader that is created from a
        /// stringified readable block header.
        /// </summary>
        /// <returns>A Bulletproof instance equivalent to the provided
        /// JSON.</returns>
        public static Bulletproof FromReadable(string json)
        {
            return Discreet.Readable.Bulletproof.FromReadable(json);
        }

        /// <summary>
        /// Deserialize initializes the bulletproof from the deserialization
        /// of a byte array contaning the data of a bulletproof.
        /// </summary>
        /// <param name="bytes">A byte array that will hold a copy of the
        /// bulletproof serialization.</param>
        public void Deserialize(byte[] bytes)
        {
            Deserialize(bytes, 0);
        }

        /// <summary>
        /// Deserialize initializes the bulletproof instance from the
        /// deserialization of a byte array, starting from an offset, which
        /// contains the data of a bulletproof.
        /// </summary>
        /// <param name="bytes">A byte array that will hold a copy of the
        /// bulletproof serialization.</param>
        /// <param name="offset">An offset that works as the starting index for
        /// the byte array to start the deserialization.</param>
        /// <returns>An offset equal to the original offset, plus the length of
        /// the deserialized bulletproof.</returns>
        public uint Deserialize(byte[] bytes, uint offset)
        {
            size = Serialization.GetUInt32(bytes, offset);

            A = new Key(new byte[32]);
            S = new Key(new byte[32]);
            T1 = new Key(new byte[32]);
            T2 = new Key(new byte[32]);
            taux = new Key(new byte[32]);
            mu = new Key(new byte[32]);

            A = new Key(bytes, offset + 4);
            S = new Key(bytes, offset + 4 + 32);
            T1 = new Key(bytes, offset + 4 + 32 * 2);
            T2 = new Key(bytes, offset + 4 + 32 * 3);
            taux = new Key(bytes, offset + 4 + 32 * 4);
            mu = new Key(bytes, offset + 4 + 32 * 5);

            int _size = (size > 8 ? 10 : (size > 4 ? 9 : (size > 2 ? 8 : (size > 1 ? 7 : 6))));

            L = new Key[_size];
            R = new Key[_size];

            for (int i = 0; i < _size; i++)
            {
                L[i] = new Key(bytes, offset + 4 + 32 * 6 + 32 * (uint)i);
            }

            for (int i = 0; i < _size; i++)
            {
                R[i] = new Key(bytes, offset + 4 + 32 * 6 + (uint)L.Length * 32 + 32 * (uint)i);
            }

            a = new Key(bytes, offset + 4 + 32 * 6 + 2 * (uint)L.Length * 32);
            b = new Key(bytes, offset + 4 + 32 * 6 + 2 * (uint)L.Length * 32 + 32);
            t = new Key(bytes, offset + 4 + 32 * 6 + 2 * (uint)L.Length * 32 + 64);

            return offset + Size();
        }

        /// <summary> Serialize populates a byte stream that contains all the
        /// data in a Bulletproof transformed to bytes.
        /// </summary>
        /// <param name="s">A byte stream.</param>
        public void Serialize(Stream s)
        {
            Serialization.CopyData(s, size);
            s.Write(A.bytes);
            s.Write(S.bytes);
            s.Write(T1.bytes);
            s.Write(T2.bytes);
            s.Write(taux.bytes);
            s.Write(mu.bytes);

            for (int i = 0; i < L.Length; i++)
            {
                s.Write(L[i].bytes);
            }

            for (int i = 0; i < R.Length; i++)
            {
                s.Write(R[i].bytes);
            }

            s.Write(a.bytes);
            s.Write(b.bytes);
            s.Write(t.bytes);
        }

        /// <summary>
        /// Deserialize initializes the bulletproof instance from the
        /// deserialization of bytes contained in a stream, which represent the
        /// data of a bulletproof.
        /// </summary>
        /// <param name="s">A byte stream.</param>
        public void Deserialize(Stream s)
        {
            size = Serialization.GetUInt32(s);
            A = new Key(s);
            S = new Key(s);
            T1 = new Key(s);
            T2 = new Key(s);
            taux = new Key(s);
            mu = new Key(s);

            int _size = (size > 8 ? 10 : (size > 4 ? 9 : (size > 2 ? 8 : (size > 1 ? 7 : 6))));

            L = new Key[_size];
            R = new Key[_size];

            for (int i = 0; i < size; i++)
            {
                L[i] = new Key(s);
            }

            for (int i = 0; i < size; i++)
            {
                R[i] = new Key(s);
            }

            a = new Key(s);
            b = new Key(s);
            t = new Key(s);
        }

        /// <summary>
        /// Size calculates the size of the bulletproof.
        /// </summary>
        /// <returns>The size of the bulletproof.</returns>
        public uint Size()
        {
            return (uint)L.Length * 64 + 9 * 32 + 4;
        }

        /// <summary>
        /// GenerateMock creates a mock instance of a bulletproof.
        /// </summary>
        /// <returns>A mock instance of a bulletproof.</returns>
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
        /// <summary>
        /// Verify verifies that a Transaction is valid by using a bulletproof.
        /// </summary>
        /// <param name="tx">The transaction on which we will apply the bulletproof.</param>
        /// <returns>An exception, in case of an error, null otherwise.</returns>
        public VerifyException Verify(Transaction tx)
        {
            Cipher.Bulletproof bp = new Cipher.Bulletproof(this, tx.GetCommitments());

            if (!Cipher.Bulletproof.Verify(bp))
            {
                return new VerifyException("Bulletproof", "Bulletproof is invalid!");
            }

            return null;
        }

        /// <summary>
        /// Verify verifies that a FullTransaction is valid by using a bulletproof.
        /// </summary>
        /// <param name="tx">The transaction on which we will apply the bulletproof.</param>
        /// <returns>An exception, in case of an error, null otherwise.</returns>
        public VerifyException Verify(FullTransaction tx)
        {
            Cipher.Bulletproof bp = new Cipher.Bulletproof(this, tx.GetCommitments());

            if (!Cipher.Bulletproof.Verify(bp))
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
