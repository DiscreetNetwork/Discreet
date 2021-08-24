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

    [StructLayout(LayoutKind.Sequential)]
    public struct Signature
    {
        [MarshalAs(UnmanagedType.Struct)]
        public Key s;
        [MarshalAs(UnmanagedType.Struct)]
        public Key e;

        public Signature(Key x, Key p, string m)
        {
            this = KeyOps.Sign(ref x, ref p, m);
        }

        public Signature(Key x, Key p, byte[] m)
        {
            this = KeyOps.Sign(ref x, ref p, m);
        }

        public Signature(Key x, Key p, Hash m)
        {
            this = KeyOps.Sign(ref x, ref p, m);
        }

        public Signature(Key x, Key p, SHA256 m)
        {
            this = KeyOps.Sign(ref x, ref p, m);
        }

        public Signature(Key x, Key p, Key m)
        {
            this = KeyOps.Sign(ref x, ref p, m);
        }

        public bool Verify(Key p, byte[] m)
        {
            Key mk = KeyOps.SHA256ToKey(SHA256.HashData(m));

            return KeyOps.SchnorrVerify(ref s, ref e, ref p, ref mk);
        }

        public bool Verify(Key p, SHA256 m)
        {
            Key mk = KeyOps.SHA256ToKey(m);
            return KeyOps.SchnorrVerify(ref s, ref e, ref p, ref mk);
        }

        public bool Verify(Key p, string m)
        {
            byte[] bytes = UTF8Encoding.UTF8.GetBytes(m);
            Key mk = KeyOps.SHA256ToKey(SHA256.HashData(bytes));

            return KeyOps.SchnorrVerify(ref s, ref e, ref p, ref mk);
        }

        public bool Verify(Key p, Hash m)
        {
            byte[] bytes = m.GetBytes();
            Key mk = KeyOps.SHA256ToKey(SHA256.HashData(bytes));

            return KeyOps.SchnorrVerify(ref s, ref e, ref p, ref mk);
        }

        public bool Verify(Key p, Key m)
        {
            return KeyOps.SchnorrVerify(ref s, ref e, ref p, ref m);
        }
    }

    public static class KeyOps
    {
        [DllImport(@"DiscreetCore.dll", EntryPoint = "GenerateKeypair", CallingConvention = CallingConvention.StdCall)]
        public static extern void GenerateKeypair(ref Key sk, ref Key pk);

        /* ap = a*P, a is a group scalar, P is a group element */
        [DllImport(@"DiscreetCore.dll", EntryPoint = "ScalarmultKey", CallingConvention = CallingConvention.StdCall)]
        public static extern void ScalarmultKey(ref Key ap, ref Key p, ref Key a);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "DKSAP", CallingConvention = CallingConvention.StdCall)]
        public static extern void DKSAP(ref Key R, ref Key T, ref Key pv, ref Key ps);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "DKSAPRecover", CallingConvention = CallingConvention.StdCall)]
        public static extern void DKSAPRecover(ref Key t, ref Key R, ref Key sv, ref Key ss);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "GenerateSeckey1", CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key GenerateSeckey();

        [DllImport(@"DiscreetCore.dll", EntryPoint = "GenerateSeckey", CallingConvention = CallingConvention.StdCall)]
        public static extern void GenerateSeckey(ref Key sk);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "GeneratePubkey", CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key GeneratePubkey();

        public static void GeneratePubkey(ref Key pk)
        {
            Key.Copy(GeneratePubkey(), pk);
        }

        [DllImport(@"DiscreetCore.dll", EntryPoint = "ScalarmultBase", CallingConvention = CallingConvention.StdCall)]
        public static extern void ScalarmultBase(ref Key ag, ref Key a);


        [DllImport(@"DiscreetCore.dll", EntryPoint = "ScalarmultBase1", CallingConvention = CallingConvention.StdCall)]
        public static extern Key ScalarmultBase(ref Key a);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "GenCommitment", CallingConvention = CallingConvention.StdCall)]
        public static extern void GenCommitment(ref Key c, ref Key a, [MarshalAs(UnmanagedType.U8)] ulong amount);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "Commit", CallingConvention = CallingConvention.StdCall)]
        public static extern Key Commit(ref Key a, [MarshalAs(UnmanagedType.U8)] ulong amount);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "CommitToZero", CallingConvention = CallingConvention.StdCall)]
        public static extern Key CommitToZero([MarshalAs(UnmanagedType.U8)] ulong amount);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "RandomDisAmount", CallingConvention = CallingConvention.StdCall)]
        public static extern ulong RandomDisAmount([MarshalAs(UnmanagedType.U8)] ulong limit);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "ScalarmultKey1", CallingConvention = CallingConvention.StdCall)]
        public static extern Key ScalarmultKey(ref Key p, ref Key a);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "ScalarmultH", CallingConvention = CallingConvention.StdCall)]
        public static extern Key ScalarmultH(ref Key a);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "Scalarmult8", CallingConvention = CallingConvention.StdCall)]
        public static extern void Scalarmult8(ref GEP3 res, ref Key p);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "Scalarmult81", CallingConvention = CallingConvention.StdCall)]
        public static extern Key Scalarmult8(ref Key p);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "InMainSubgroup", CallingConvention = CallingConvention.StdCall)]
        public static extern bool InMainSubgroup(ref Key a);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "AddKeys", CallingConvention = CallingConvention.StdCall)]
        public static extern void AddKeys(ref Key ab, ref Key a, ref Key b);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "AddKeys_1", CallingConvention = CallingConvention.StdCall)]
        public static extern Key AddKeys(ref Key a, ref Key b);

        /* computes agb = aG + B */
        [DllImport(@"DiscreetCore.dll", EntryPoint = "AddKeys1", CallingConvention = CallingConvention.StdCall)]
        public static extern void AGB(ref Key agb, ref Key a, ref Key b);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "AddKeys2", CallingConvention = CallingConvention.StdCall)]
        public static extern void AGBP(ref Key agbp, ref Key a, ref Key b, ref Key p);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "Precomp", CallingConvention = CallingConvention.StdCall)]
        public static extern void Precomp([In, Out] [MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType =UnmanagedType.Struct)] GEPrecomp[] rv, ref Key b);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "AddKeys3", CallingConvention = CallingConvention.StdCall)]
        public static extern void APBQ(ref Key apbq, ref Key a, ref Key p, ref Key b, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] q);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "AddKeys3_1", CallingConvention = CallingConvention.StdCall)]
        public static extern void APBQ(ref Key apbq, ref Key a, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] p, ref Key b, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] q);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "AddKeys4", CallingConvention = CallingConvention.StdCall)]
        public static extern void AGBPCQ(ref Key agbpcq, ref Key a, ref Key b, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] p, ref Key c, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] q);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "AddKeys5", CallingConvention = CallingConvention.StdCall)]
        public static extern void APBQCR(ref Key agbpcq, ref Key a, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] p, ref Key b, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] q, ref Key c, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] r);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "SubKeys", CallingConvention = CallingConvention.StdCall)]
        public static extern void SubKeys(ref Key ab, ref Key a, ref Key b);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "EqualKeys", CallingConvention = CallingConvention.StdCall)]
        public static extern bool EqualKeys(ref Key a, ref Key b);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "GenCommitmentMask", CallingConvention = CallingConvention.StdCall)]
        public static extern Key GenCommitmentMask(ref Key sk);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "ECDHEncode", CallingConvention = CallingConvention.StdCall)]
        public static extern Key ECDHEncode(ref ECDHTuple unmasked, ref Key secret, [MarshalAs(UnmanagedType.Bool)] bool v2);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "ECDHDecode", CallingConvention = CallingConvention.StdCall)]
        public static extern Key ECDHDecode(ref ECDHTuple masked, ref Key secret, [MarshalAs(UnmanagedType.Bool)] bool v2);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "SchnorrSign", CallingConvention = CallingConvention.StdCall)]
        public static extern void SchnorrSign(ref Key s, ref Key e, ref Key p, ref Key x, ref Key m);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "SchnorrVerify", CallingConvention = CallingConvention.StdCall)]
        public static extern bool SchnorrVerify(ref Key s, ref Key e, ref Key p, ref Key m);

        public static Key SHA256ToKey(SHA256 h)
        {
            return new Key(h.GetBytes());
        }

        public static Signature Sign(ref Key x, ref Key p, string m)
        {
            byte[] bytes = UTF8Encoding.UTF8.GetBytes(m);
            Key mk = SHA256ToKey(SHA256.HashData(bytes));

            Signature sig = new Signature();
            sig.s = new Key();
            sig.e = new Key();
            SchnorrSign(ref sig.s, ref sig.e, ref p, ref x, ref mk);
            return sig;
        }

        public static Signature Sign(ref Key x, ref Key p, Hash m)
        {
            byte[] bytes = m.GetBytes();
            Key mk = SHA256ToKey(SHA256.HashData(bytes));

            Signature sig = new Signature();
            sig.s = new Key();
            sig.e = new Key();
            SchnorrSign(ref sig.s, ref sig.e, ref p, ref x, ref mk);
            return sig;
        }

        public static Signature Sign(ref Key x, ref Key p, SHA256 m)
        {
            Key mk = SHA256ToKey(m);

            Signature sig = new Signature();
            sig.s = new Key();
            sig.e = new Key();
            SchnorrSign(ref sig.s, ref sig.e, ref p, ref x, ref mk);
            return sig;
        }

        public static Signature Sign(ref Key x, ref Key p, byte[] m)
        {
            Key mk = SHA256ToKey(SHA256.HashData(m));

            Signature sig = new Signature();
            sig.s = new Key();
            sig.e = new Key();
            SchnorrSign(ref sig.s, ref sig.e, ref p, ref x, ref mk);
            return sig;
        }

        public static Signature Sign(ref Key x, ref Key p, Key m)
        {
            Signature sig = new Signature();
            sig.s = new Key();
            sig.e = new Key();
            SchnorrSign(ref sig.s, ref sig.e, ref p, ref x, ref m);
            return sig;
        }
    }
}
