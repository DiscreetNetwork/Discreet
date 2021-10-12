using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    public static class KeyOps
    {
        [DllImport(@"DiscreetCore.dll", EntryPoint = "GenerateKeypair", CallingConvention = CallingConvention.StdCall)]
        public static extern void GenerateKeypair(ref Key sk, ref Key pk);

        /* ap = a*P, a is a group scalar, P is a group element */
        [DllImport(@"DiscreetCore.dll", EntryPoint = "ScalarmultKey", CallingConvention = CallingConvention.StdCall)]
        public static extern void ScalarmultKey(ref Key ap, ref Key p, ref Key a);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "DKSAP", CallingConvention = CallingConvention.StdCall)]
        public static extern void DKSAP(ref Key R, ref Key T, ref Key pv, ref Key ps, int index);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "DKSAPRecover", CallingConvention = CallingConvention.StdCall)]
        public static extern void DKSAPRecover(ref Key t, ref Key R, ref Key sv, ref Key ss, int index);

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

        public static Key ScalarmultBase(ref Key a)
        {
            Key ag = new Key();
            ScalarmultBase(ref ag, ref a);
            return ag;
        }

        [DllImport(@"DiscreetCore.dll", EntryPoint = "GenCommitment", CallingConvention = CallingConvention.StdCall)]
        public static extern void GenCommitment(ref Key c, ref Key a, [MarshalAs(UnmanagedType.U8)] ulong amount);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "Commit", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key Commit(ref Key a, [MarshalAs(UnmanagedType.U8)] ulong amount);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "CommitToZero", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key CommitToZero([MarshalAs(UnmanagedType.U8)] ulong amount);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "RandomDisAmount", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.U8)]
        public static extern ulong RandomDisAmount([MarshalAs(UnmanagedType.U8)] ulong limit);

        public static Key ScalarmultKey(ref Key p, ref Key a)
        {
            Key ap = new Key(new byte[32]);
            ScalarmultKey(ref ap, ref p, ref a);
            return ap;
        }

        [DllImport(@"DiscreetCore.dll", EntryPoint = "ScalarmultH", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key ScalarmultH(ref Key a);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "Scalarmult8", CallingConvention = CallingConvention.StdCall)]
        public static extern void Scalarmult8(ref GEP3 res, ref Key p);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "scalarmult_8_correct", CallingConvention = CallingConvention.StdCall)]
        public static extern void Scalarmult8(ref Key res, ref Key p);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "Scalarmult81", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key Scalarmult8(ref Key p);

        public static ulong XOR8(ref Key g, ulong amount)
        {
            byte[] amountBytes = Coin.Serialization.UInt64(amount);

            for (int i = 0; i < 8; i++)
            {
                amountBytes[i] ^= g.bytes[i];
            }

            return Coin.Serialization.GetUInt64(amountBytes, 0);
        }

        public static ulong GenAmountMask(ref Key r, ref Key pv, int i, ulong amount)
        {
            byte[] cdata = new byte[36];
            Array.Copy(ScalarmultKey(ref pv, ref r).bytes, cdata, 32);
            Coin.Serialization.CopyData(cdata, 32, i);

            Key c = new Key(new byte[32]);
            HashOps.HashToScalar(ref c, cdata, 36);

            byte[] gdata = new byte[38];
            gdata[0] = (byte)'a';
            gdata[1] = (byte)'m';
            gdata[2] = (byte)'o';
            gdata[3] = (byte)'u';
            gdata[4] = (byte)'n';
            gdata[5] = (byte)'t';
            Array.Copy(c.bytes, 0, gdata, 6, 32);

            Key g = new Key(new byte[32]);
            HashOps.HashData(ref g, gdata, 38);

            return XOR8(ref g, amount);
        }

        [DllImport(@"DiscreetCore.dll", EntryPoint = "InMainSubgroup", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool InMainSubgroup(ref Key a);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "AddKeys", CallingConvention = CallingConvention.StdCall)]
        public static extern void AddKeys(ref Key ab, ref Key a, ref Key b);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "AddKeys_1", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key AddKeys(ref Key a, ref Key b);

        /* computes agb = aG + B */
        [DllImport(@"DiscreetCore.dll", EntryPoint = "AddKeys1", CallingConvention = CallingConvention.StdCall)]
        public static extern void AGB(ref Key agb, ref Key a, ref Key b);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "AddKeys2", CallingConvention = CallingConvention.StdCall)]
        public static extern void AGBP(ref Key agbp, ref Key a, ref Key b, ref Key p);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "Precomp", CallingConvention = CallingConvention.StdCall)]
        public static extern void Precomp([In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] rv, ref Key b);

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
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EqualKeys(ref Key a, ref Key b);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "GenCommitmentMask", CallingConvention = CallingConvention.StdCall)]
        public static extern void GenCommitmentMask(ref Key rv, ref Key sk);

        public static Key GenCommitmentMask(ref Key r, ref Key pv, int i)
        {
            byte[] cdata = new byte[36];
            Array.Copy(ScalarmultKey(ref pv, ref r).bytes, cdata, 32);
            Coin.Serialization.CopyData(cdata, 32, i);

            Key c = new Key(new byte[32]);
            HashOps.HashToScalar(ref c, cdata, 36);

            Key rv = new Key(new byte[32]);

            GenCommitmentMask(ref rv, ref c);

            return rv;
        }

        public static Key GenCommitmentMaskRecover(ref Key R, ref Key sv, int i)
        {
            byte[] cdata = new byte[36];
            Array.Copy(ScalarmultKey(ref R, ref sv).bytes, cdata, 32);
            Coin.Serialization.CopyData(cdata, 32, i);

            Key c = new Key(new byte[32]);
            HashOps.HashToScalar(ref c, cdata, 36);

            Key rv = new Key(new byte[32]);

            GenCommitmentMask(ref rv, ref c);

            return rv;
        }

        [DllImport(@"DiscreetCore.dll", EntryPoint = "ECDHEncode", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key ECDHEncode(ref ECDHTuple unmasked, ref Key secret, [MarshalAs(UnmanagedType.Bool)] bool v2);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "ECDHDecode", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key ECDHDecode(ref ECDHTuple masked, ref Key secret, [MarshalAs(UnmanagedType.Bool)] bool v2);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "SchnorrSign", CallingConvention = CallingConvention.StdCall)]
        public static extern void SchnorrSign(ref Key s, ref Key e, ref Key p, ref Key x, ref Key m);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "SchnorrVerify", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SchnorrVerify(ref Key s, ref Key e, ref Key p, ref Key m);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "GenerateLinkingTag", CallingConvention = CallingConvention.StdCall)]
        public static extern void GenerateLinkingTag(ref Key J, ref Key r);

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

        public static void DKSAP(ref Key r, ref Key R, Key[] T, Key[] pv, Key[] ps)
        {
            r = new Key(new byte[32]);
            R = new Key(new byte[32]);

            GenerateKeypair(ref r, ref R);

            for (uint i = 0; i < ps.Length; i++)
            {
                Key cscalar = ScalarmultKey(ref pv[i], ref r);

                byte[] txbytes = new byte[36];
                Array.Copy(cscalar.bytes, txbytes, 32);
                txbytes[32] = (byte)(i >> 24);
                txbytes[33] = (byte)((i >> 16) & 0xFF);
                txbytes[34] = (byte)((i >> 8) & 0xFF);
                txbytes[35] = (byte)(i & 0xFF);

                Key c = new Key(new byte[32]);
                HashOps.HashToScalar(ref c, txbytes, 36);

                T[i] = new Key(new byte[32]);
                AGB(ref T[i], ref c, ref ps[i]);
            }
        }

        public static Key DKSAPRecover(ref Key R, ref Key sv, ref Key ss, int i)
        {
            Key cscalar = ScalarmultKey(ref R, ref sv);

            byte[] txbytes = new byte[36];
            Array.Copy(cscalar.bytes, txbytes, 32);
            txbytes[32] = (byte)(i >> 24);
            txbytes[33] = (byte)((i >> 16) & 0xFF);
            txbytes[34] = (byte)((i >> 8) & 0xFF);
            txbytes[35] = (byte)(i & 0xFF);

            Key c = new Key(new byte[32]);
            HashOps.HashToScalar(ref c, txbytes, 36);

            Key t = new Key(new byte[32]);
            ScalarAdd(ref t, ref c, ref ss);

            return t;
        }

        public static Key[] DKSAP(ref Key r, ref Key R, Key[] pv, Key[] ps)
        {
            Key[] T = new Key[pv.Length];

            DKSAP(ref r, ref R, T, pv, ps);

            return T;
        }

        public static Key DKSAP(ref Key r, Key pv, Key ps, int i)
        {
            Key cscalar = ScalarmultKey(ref pv, ref r);

            byte[] txbytes = new byte[36];
            Array.Copy(cscalar.bytes, txbytes, 32);
            txbytes[32] = (byte)(i >> 24);
            txbytes[33] = (byte)((i >> 16) & 0xFF);
            txbytes[34] = (byte)((i >> 8) & 0xFF);
            txbytes[35] = (byte)(i & 0xFF);

            Key c = new Key(new byte[32]);
            HashOps.HashToScalar(ref c, txbytes, 36);

            Key T = new Key(new byte[32]);
            AGB(ref T, ref c, ref ps);

            return T;
        }

        [DllImport(@"DiscreetCore.dll", EntryPoint = "ScalarAdd", CallingConvention = CallingConvention.StdCall)]
        public static extern void ScalarAdd(ref Key res, ref Key a, ref Key b);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "ScalarSub", CallingConvention = CallingConvention.StdCall)]
        public static extern void ScalarSub(ref Key res, ref Key a, ref Key b);

        public static int KeyArrayLength(Key[] k)
        {
            int i = 0;

            while (i < k.Length && k[i].bytes != null && !k[i].Equals(Key.Z)) i++;

            return i;
        }
    }
}
