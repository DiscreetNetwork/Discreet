﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    /// <summary>
    /// Represents a 256-bit group of information, which can be an ed25519 scalar, group element, sha256 hash, or keccak hash.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Key
    {
        /// <summary>
        /// The 256 bit information.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] bytes;

        /// <summary>
        /// Constructs a Key with the specified byte information.
        /// </summary>
        /// <param name="_bytes"></param>
        public Key(byte[] _bytes)
        {
            bytes = _bytes;
        }

        /// <summary>
        /// Returns the zero Key.
        /// </summary>
        /// <returns></returns>
        public static Key Zero()
        {
            return Z;
        }

        /// <summary>
        /// Copies the zero Key to the specified Key.
        /// </summary>
        /// <param name="k"></param>
        public static void Zero(Key k)
        {
            Array.Copy(Key.Zero().bytes, k.bytes, 32);
        }

        /// <summary>
        /// Returns the identity Key.
        /// </summary>
        /// <returns></returns>
        public static Key Identity()
        {
            return I;
        }

        /// <summary>
        /// Copies the identity Key to the specified Key.
        /// </summary>
        /// <param name="k"></param>
        public static void Identity(Key k)
        {
            Array.Copy(Key.Identity().bytes, k.bytes, 32);
        }

        /// <summary>
        /// Returns the curve order for ed25519.
        /// </summary>
        /// <returns></returns>
        public static Key CurveOrder()
        {
            return L;
        }

        /// <summary>
        /// Copies the curve order for ed25519 to the specified Key.
        /// </summary>
        /// <returns></returns>
        public static void CurveOrder(Key k)
        {
            Array.Copy(Key.CurveOrder().bytes, k.bytes, 32);
        }

        /// <summary>
        /// copies the contents of Key a to Key b 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static void Copy(Key a, Key b)
        {
            Array.Copy(a.bytes, b.bytes, 32);
        }

        /// <summary>
        /// Copies the contents of the specified Key and returns a new Key.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Key Copy(Key b)
        {
            return new Key((byte[])b.bytes.Clone());
        }

        /// <summary>
        /// The zero element in the ed25519 scalar field.
        /// </summary>
        public static readonly Key Z = new Key(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        
        /// <summary>
        /// The identity element in the ed25519 group.
        /// </summary>
        public static readonly Key I = new Key(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        
        /// <summary>
        /// The order of the ed25519 curve.
        /// </summary>
        public static readonly Key L = new Key(new byte[] { 0xed, 0xd3, 0xf5, 0x5c, 0x1a, 0x63, 0x12, 0x58, 0xd6, 0x9c, 0xf7, 0xa2, 0xde, 0xf9, 0xde, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10 });
        
        /// <summary>
        /// The ed25519 group generator.
        /// </summary>
        public static readonly Key G = new Key(new byte[] { 0x58, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66 });
        
        /// <summary>
        /// The value 8 in the ed25519 scalar field.
        /// </summary>
        public static readonly Key EIGHT = new Key(new byte[] { 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        
        /// <summary>
        /// The inverse of 8 in the ed25519 scalar field.
        /// </summary>
        public static readonly Key INV_EIGHT = new Key(new byte[] { 0x79, 0x2f, 0xdc, 0xe2, 0x29, 0xe5, 0x06, 0x61, 0xd0, 0xda, 0x1c, 0x7d, 0xb3, 0x9d, 0xd3, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x06 });
        
        /// <summary>
        /// Another ed25519 generator used for the Discreet cryptographic algorithms.
        /// </summary>
        public static readonly Key H = new Key(new byte[] { 0x8b, 0x65, 0x59, 0x70, 0x15, 0x37, 0x99, 0xaf, 0x2a, 0xea, 0xdc, 0x9f, 0xf1, 0xad, 0xd0, 0xea, 0x6c, 0x72, 0x51, 0xd5, 0x41, 0x54, 0xcf, 0xa9, 0x2c, 0x17, 0x3a, 0x0d, 0xd3, 0x9c, 0x1f, 0x94 });

        /// <summary>
        /// Compares the two keys. Returns 1 if a > b, -1 if a < b, and 0 if they're equal.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int Compare(Key a, Key b)
        {
            if (a.bytes == null && b.bytes == null) return 0;

            if (a.bytes == null) return -1;
            if (b.bytes == null) return 1;

            int i;

            for (i = 0; i < 32; i++)
            {
                if (a.bytes[i] != b.bytes[i])
                {
                    break;
                }
            }

            if (i == 32)
            {
                return 0;
            }
            else if (a.bytes[i] > b.bytes[i])
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Returns true if the key is equal to b, and false otherwise.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public bool Equals(Key b)
        {
            return Compare(this, b) == 0;
        }

        /// <summary>
        /// Returns a hexadecimal string of the Key.
        /// </summary>
        /// <returns></returns>
        public string ToHex()
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLower();
        }

        /// <summary>
        /// Returns a shortened version of the hexadecimal string of the Key. Used for Exceptions.
        /// </summary>
        /// <returns></returns>
        public string ToHexShort()
        {
            return ToHex().Substring(0, 8) + "...";
        }

        /// <summary>
        /// Creates a key from the hexadecimal string.
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static Key FromHex(string hex)
        {
            return new Key(Common.Printable.Byteify(hex));
        }
    }

    /// <summary>
    /// Represents a tuple of ECDH information, or used for creating a commitment C = aG + bH.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ECDHTuple
    {
        /// <summary>
        /// The masking key used to blind the amount, or the value a.
        /// </summary>
        [MarshalAs(UnmanagedType.Struct)]
        public Key mask;

        /// <summary>
        /// The amount, or the value b.
        /// </summary>
        [MarshalAs(UnmanagedType.Struct)]
        public Key amount;
    }
}
