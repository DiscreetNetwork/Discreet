using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Key
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] bytes;

        public Key(byte[] _bytes)
        {
            bytes = _bytes;
        }

        public static Key Zero()
        {
            return Z;
        }

        public static void Zero(Key k)
        {
            Array.Copy(Key.Zero().bytes, k.bytes, 32);
        }

        public static Key Identity()
        {
            return I;
        }

        public static void Identity(Key k)
        {
            Array.Copy(Key.Identity().bytes, k.bytes, 32);
        }

        public static Key CurveOrder()
        {
            return L;
        }

        public static void CurveOrder(Key k)
        {
            Array.Copy(Key.CurveOrder().bytes, k.bytes, 32);
        }

        /* copies the contents of key b to key a */
        public static void Copy(Key a, Key b)
        {
            Array.Copy(a.bytes, b.bytes, 32);
        }

        public static Key Copy(Key b)
        {
            return new Key((byte[])b.bytes.Clone());
        }

        public static Key Z = new Key(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        public static Key I = new Key(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        public static Key L = new Key(new byte[] { 0xed, 0xd3, 0xf5, 0x5c, 0x1a, 0x63, 0x12, 0x58, 0xd6, 0x9c, 0xf7, 0xa2, 0xde, 0xf9, 0xde, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10 });
        public static Key G = new Key(new byte[] { 0x58, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66 });
        public static Key EIGHT = new Key(new byte[] { 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        public static Key INV_EIGHT = new Key(new byte[] { 0x79, 0x2f, 0xdc, 0xe2, 0x29, 0xe5, 0x06, 0x61, 0xd0, 0xda, 0x1c, 0x7d, 0xb3, 0x9d, 0xd3, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x06 });
        public static Key H = new Key(new byte[] { 0x8b, 0x65, 0x59, 0x70, 0x15, 0x37, 0x99, 0xaf, 0x2a, 0xea, 0xdc, 0x9f, 0xf1, 0xad, 0xd0, 0xea, 0x6c, 0x72, 0x51, 0xd5, 0x41, 0x54, 0xcf, 0xa9, 0x2c, 0x17, 0x3a, 0x0d, 0xd3, 0x9c, 0x1f, 0x94 });

        /* a > b => 1, a == b => 0, a < b => -1 */
        public static int Compare(Key a, Key b)
        {
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

        public bool Equals(Key b)
        {
            return Compare(this, b) == 0;
        }

        public string ToHex()
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLower();
        }

        public string ToHexShort()
        {
            return ToHex().Substring(0, 8) + "...";
        }

        public static Key FromHex(string hex)
        {
            return new Key(Common.Printable.Byteify(hex));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ECDHTuple
    {
        [MarshalAs(UnmanagedType.Struct)]
        public Key mask;

        [MarshalAs(UnmanagedType.Struct)]
        public Key amount;
    }

    /**
     * This is used in place of the C++-style vector argument for keys and 
     * commitments for proofs like Triptych, CLSAG and 
     *
     * Alice has keys (a, A); Bob has keys (b, B)
     * 
     * alice does aB = Ab = g^(ab)
     * 
     * (a, A), (b, B), (c, C), (d, D)
     * 
     * 1. everybody sends keys. Alice -> Bob, Carol, Dan : (B, C, D) ; etc
     * A. B^a * C * D = A^(b) * C * D = A^(c) * B * D = A^(d) * B * C = (g*g*g)^(abcd)
     * 
     * 
     */
    [StructLayout(LayoutKind.Sequential)]
    public struct Key64
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.Struct)]
        public Key[] keys;
    }
}
