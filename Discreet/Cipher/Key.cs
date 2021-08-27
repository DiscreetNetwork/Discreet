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

        /* based on crypto_verify_32 (in DiscreetCore/src/crypto/verify.c) */
        public bool Equals(Key b)
        {
            int diff = 0;
            diff |= (int)bytes[0] ^ (int)b.bytes[0];
            diff |= (int)bytes[1] ^ (int)b.bytes[1];
            diff |= (int)bytes[2] ^ (int)b.bytes[2];
            diff |= (int)bytes[3] ^ (int)b.bytes[3];
            diff |= (int)bytes[4] ^ (int)b.bytes[4];
            diff |= (int)bytes[5] ^ (int)b.bytes[5];
            diff |= (int)bytes[6] ^ (int)b.bytes[6];
            diff |= (int)bytes[7] ^ (int)b.bytes[7];
            diff |= (int)bytes[8] ^ (int)b.bytes[8];
            diff |= (int)bytes[9] ^ (int)b.bytes[9];
            diff |= (int)bytes[10] ^ (int)b.bytes[10];
            diff |= (int)bytes[11] ^ (int)b.bytes[11];
            diff |= (int)bytes[12] ^ (int)b.bytes[12];
            diff |= (int)bytes[13] ^ (int)b.bytes[13];
            diff |= (int)bytes[14] ^ (int)b.bytes[14];
            diff |= (int)bytes[15] ^ (int)b.bytes[15];
            diff |= (int)bytes[16] ^ (int)b.bytes[16];
            diff |= (int)bytes[17] ^ (int)b.bytes[17];
            diff |= (int)bytes[18] ^ (int)b.bytes[18];
            diff |= (int)bytes[19] ^ (int)b.bytes[19];
            diff |= (int)bytes[20] ^ (int)b.bytes[20];
            diff |= (int)bytes[21] ^ (int)b.bytes[21];
            diff |= (int)bytes[22] ^ (int)b.bytes[22];
            diff |= (int)bytes[23] ^ (int)b.bytes[23];
            diff |= (int)bytes[24] ^ (int)b.bytes[24];
            diff |= (int)bytes[25] ^ (int)b.bytes[25];
            diff |= (int)bytes[26] ^ (int)b.bytes[26];
            diff |= (int)bytes[27] ^ (int)b.bytes[27];
            diff |= (int)bytes[28] ^ (int)b.bytes[28];
            diff |= (int)bytes[29] ^ (int)b.bytes[29];
            diff |= (int)bytes[30] ^ (int)b.bytes[30];
            diff |= (int)bytes[31] ^ (int)b.bytes[31];
            return (1 & ((diff - 1) >> 8) - 1) != 0;
        }

        public string ToHex()
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLower();
        }

        public string ToHexShort()
        {
            return ToHex().Substring(0, 8);
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
     *
     *
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
