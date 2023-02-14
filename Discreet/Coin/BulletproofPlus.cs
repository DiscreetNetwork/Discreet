using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Discreet.Cipher;
using System.IO;

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

        /// <summary>
        /// Bulletproof creates a Coin.BulletproofPlus instance that copies the
        /// values of the fields of a Cipher.BulletproofPlus instance. This
        /// instance is better suited to be used in the Coin namespace.
        /// </summary>
        /// <param name="bp">An instance of Cipher.BulletproofPlus.</param>
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

        /// <summary>
        /// Hash creates a SHA-256 hash of the serialization of the bulletproof+.
        /// </summary>
        /// <returns>The hash of the bulletproof+.</returns>
        public SHA256 Hash()
        {
            return SHA256.HashData(Serialize());
        }

        /// <summary>
        /// Serialize creates a byte array equivalent of the bulletproof+
        /// instance.
        /// </summary>
        /// <returns>A byte array representing the serialized
        /// bulletproof+.</returns>
        public byte[] Serialize()
        {
            byte[] bytes = new byte[L.Length * 64 + 6 * 32 + 4];
            Serialization.CopyData(bytes, 0, size);

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

        /// <summary>
        /// Serialize copies this BulletproofPlus's serialized representation to
        /// a given byte array, starting at an offset.
        /// </summary>
        /// <param name="bytes">A byte array that will hold a copy of the
        /// bulletproof+ serialization.</param>
        /// <param name="offset">The offset at which we will copy the
        /// bulletproof+ serialization.</param>
        public void Serialize(byte[] bytes, uint offset)
        {
            byte[] _bytes = Serialize();
            Array.Copy(_bytes, 0, bytes, offset, _bytes.Length);
        }

        /// <summary>
        /// Readable encodes the bulletproof+ to a string containing an
        /// equivalent JSON.
        /// </summary>
        public string Readable()
        {
            return Discreet.Readable.BulletproofPlus.ToReadable(this);
        }

        /// <summary>
        /// ToReadable converts the bulletproof+ to an object containing the
        /// bulletproof+ in JSON format.
        /// </summary>
        /// <returns>A readable bulletproof+.</returns>
        public object ToReadable()
        {
            return new Discreet.Readable.BulletproofPlus(this);
        }

        /// <summary>
        /// FromReadable returns a BulletproofPlus that is created from a
        /// stringified readable bulletproof.
        /// </summary>
        /// <returns>A BulletproofPlus instance equivalent to the provided
        /// JSON.</returns>
        public static BulletproofPlus FromReadable(string json)
        {
            return Discreet.Readable.BulletproofPlus.FromReadable(json);
        }

        /// <summary>
        /// Deserialize initializes the bulletproof+ from the deserialization of
        /// a byte array contaning the data of a bulletproof+.
        /// </summary>
        /// <param name="bytes">A byte array that will hold a copy of the
        /// bulletproof+ serialization.</param>
        public void Deserialize(byte[] bytes)
        {
            Deserialize(bytes, 0);
        }

        /// <summary>
        /// Deserialize initializes the bulletproof+ instance from the
        /// deserialization of a byte array, starting from an offset, which
        /// contains the data of a bulletproof+.
        /// </summary>
        /// <param name="bytes">A byte array that will hold a copy of the
        /// bulletproof+ serialization.</param>
        /// <param name="offset">An offset that works as the starting index for
        /// the byte array to start the deserialization.</param>
        /// <returns>An offset equal to the original offset, plus the length of
        /// the deserialized bulletproof+.</returns>
        public uint Deserialize(byte[] bytes, uint offset)
        {
            size = Serialization.GetUInt32(bytes, offset);

            A = new Key(bytes, offset + 4);
            A1 = new Key(bytes, offset + 4 + 32);
            B = new Key(bytes, offset + 4 + 32 * 2);
            r1 = new Key(bytes, offset + 4 + 32 * 3);
            s1 = new Key(bytes, offset + 4 + 32 * 4);
            d1 = new Key(bytes, offset + 4 + 32 * 5);

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

            return offset + Size();
        }

        /// <summary> Serialize populates a byte stream that contains all the
        /// data in a BulletproofPlus transformed to bytes.
        /// </summary>
        /// <param name="s">A byte stream.</param>
        public void Serialize(Stream s)
        {
            Serialization.CopyData(s, size);

            s.Write(A.bytes);
            s.Write(A1.bytes);
            s.Write(B.bytes);
            s.Write(r1.bytes);
            s.Write(s1.bytes);
            s.Write(d1.bytes);

            for (int i = 0; i < L.Length; i++)
            {
                s.Write(L[i].bytes);
            }

            for (int i = 0; i < R.Length; i++)
            {
                s.Write(R[i].bytes);
            }
        }

        /// <summary>
        /// Deserialize initializes the bulletproof+ instance from the
        /// deserialization of bytes contained in a stream, which represent the
        /// data of a bulletproof+.
        /// </summary>
        /// <param name="s">A byte stream.</param>
        public void Deserialize(Stream s)
        {
            size = Serialization.GetUInt32(s);

            A = new Key(s);
            A1 = new Key(s);
            B = new Key(s);
            r1 = new Key(s);
            s1 = new Key(s);
            d1 = new Key(s);

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
        }

        /// <summary>
        /// Size calculates the size of the bulletproof+.
        /// </summary>
        /// <returns>The size of the bulletproof+.</returns>
        public uint Size()
        {
            return (uint)L.Length * 64 + 6 * 32 + 4;
        }

        /* needs to recover the commitments from tx's outputs */
        /// <summary>
        /// Verify verifies that a Transaction is valid by using a bulletproof+.
        /// </summary>
        /// <param name="tx">The transaction on which we will apply the bulletproof+.</param>
        /// <returns>An exception, in case of an error, null otherwise.</returns>
        public VerifyException Verify(Transaction tx)
        {
            Cipher.BulletproofPlus bp = new Cipher.BulletproofPlus(this, tx.GetCommitments());

            if (!Cipher.BulletproofPlus.Verify(bp))
            {
                return new VerifyException("BulletproofPlus", "BulletproofPlus is invalid!");
            }

            return null;
        }

        /// <summary>
        /// Verify verifies that a FullTransaction is valid by using a bulletproof+.
        /// </summary>
        /// <param name="tx">The transaction on which we will apply the bulletproof+.</param>
        /// <returns>An exception, in case of an error, null otherwise.</returns>
        public VerifyException Verify(FullTransaction tx)
        {
            Cipher.BulletproofPlus bp = new Cipher.BulletproofPlus(this, tx.GetCommitments());

            if (!Cipher.BulletproofPlus.Verify(bp))
            {
                return new VerifyException("BulletproofPlus", "BulletproofPlus is invalid!");
            }

            return null;
        }

        /// <summary>
        /// Verify verifies that a MixedTransaction is valid by using a bulletproof+.
        /// </summary>
        /// <param name="tx">The transaction on which we will apply the bulletproof+.</param>
        /// <returns>An exception, in case of an error, null otherwise.</returns>
        public VerifyException Verify(MixedTransaction tx)
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
