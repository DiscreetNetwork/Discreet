using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    public static class KeyOps
    {
        public static void GenerateKeypair(ref Key sk, ref Key pk) => Native.Native.Instance.GenerateKeypair(ref sk, ref pk);
        public static void ScalarmultKey(ref Key ap, ref Key p, ref Key a) => Native.Native.Instance.ScalarmultKey(ref ap, ref p, ref a);
        public static void DKSAP(ref Key R, ref Key T, ref Key pv, ref Key ps, int index) => Native.Native.Instance.DKSAP(ref R, ref T, ref pv, ref ps, index);
        public static void DKSAPRecover(ref Key t, ref Key R, ref Key sv, ref Key ss, int index) => Native.Native.Instance.DKSAPRecover(ref t, ref R, ref sv, ref ss, index);
        public static Key GenerateSeckey() => Native.Native.Instance.GenerateSeckey1();
        public static void GenerateSeckey(ref Key sk) => Native.Native.Instance.GenerateSeckey(ref sk);
        public static Key GeneratePubkey() => Native.Native.Instance.GeneratePubkey();

        public static void GeneratePubkey(ref Key pk)
        {
            Key.Copy(GeneratePubkey(), pk);
        }

        public static void ScalarmultBase(ref Key ag, ref Key a) => Native.Native.Instance.ScalarmultBase(ref ag, ref a);

        public static Key ScalarmultBase(ref Key a)
        {
            Key ag = new Key(new byte[32]);
            ScalarmultBase(ref ag, ref a);
            return ag;
        }

        public static void GenCommitment(ref Key c, ref Key a, ulong amount) => Native.Native.Instance.GenCommitment(ref c, ref a, amount);
        public static Key Commit(ref Key a, ulong amount) => Native.Native.Instance.Commit(ref a, amount);
        public static Key CommitToZero(ulong amount) => Native.Native.Instance.CommitToZero(amount);
        public static ulong RandomDisAmount(ulong limit) => Native.Native.Instance.RandomDisAmount(limit);

        public static Key ScalarmultKey(ref Key p, ref Key a)
        {
            Key ap = new Key(new byte[32]);
            ScalarmultKey(ref ap, ref p, ref a);
            return ap;
        }

        public static Key ScalarmultH(ref Key a) => Native.Native.Instance.ScalarmultH(ref a);
        public static void Scalarmult8(ref GEP3 res, ref Key p) => Native.Native.Instance.Scalarmult8(ref res, ref p);
        public static void Scalarmult8(ref Key res, ref Key p) => Native.Native.Instance.scalarmult_8_correct(ref res, ref p);
        public static Key Scalarmult8(ref Key p) => Native.Native.Instance.Scalarmult81(ref p);

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

        public static bool InMainSubgroup(ref Key a) => Native.Native.Instance.InMainSubgroup(ref a);
        public static void AddKeys(ref Key ab, ref Key a, ref Key b) => Native.Native.Instance.AddKeys(ref ab, ref a, ref b);
        public static Key AddKeys(ref Key a, ref Key b) => Native.Native.Instance.AddKeys_1(ref a, ref b);
        public static void AGB(ref Key agb, ref Key a, ref Key b) => Native.Native.Instance.AddKeys1(ref agb, ref a, ref b);
        public static void AGBP(ref Key agbp, ref Key a, ref Key b, ref Key p) => Native.Native.Instance.AddKeys2(ref agbp, ref a, ref b, ref p);
        public static void Precomp(GEPrecomp[] rv, ref Key b) => Native.Native.Instance.Precomp(rv, ref b);
        public static void APBQ(ref Key apbq, ref Key a, ref Key p, ref Key b, GEPrecomp[] q) => Native.Native.Instance.AddKeys3(ref apbq, ref a, ref p, ref b, q);
        public static void APBQ(ref Key apbq, ref Key a, GEPrecomp[] p, ref Key b, GEPrecomp[] q) => Native.Native.Instance.AddKeys3_1(ref apbq, ref a, p, ref b, q);
        public static void AGBPCQ(ref Key agbpcq, ref Key a, ref Key b, GEPrecomp[] p, ref Key c, GEPrecomp[] q) => Native.Native.Instance.AddKeys4(ref agbpcq, ref a, ref b, p, ref c, q);
        public static void APBQCR(ref Key agbpcq, ref Key a, GEPrecomp[] p, ref Key b, GEPrecomp[] q, ref Key c, GEPrecomp[] r) => Native.Native.Instance.AddKeys5(ref agbpcq, ref a, p, ref b, q, ref c, r);
        public static void SubKeys(ref Key ab, ref Key a, ref Key b) => Native.Native.Instance.SubKeys(ref ab, ref a, ref b);
        public static bool EqualKeys(ref Key a, ref Key b) => Native.Native.Instance.EqualKeys(ref a, ref b);
        public static void GenCommitmentMask(ref Key rv, ref Key sk) => Native.Native.Instance.GenCommitmentMask(ref rv, ref sk);

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

        public static Key ECDHEncode(ref ECDHTuple unmasked, ref Key secret, bool v2) => Native.Native.Instance.ECDHEncode(ref unmasked, ref secret, v2);
        public static Key ECDHDecode(ref ECDHTuple masked, ref Key secret, [MarshalAs(UnmanagedType.Bool)] bool v2) => Native.Native.Instance.ECDHDecode(ref masked, ref secret, v2);
        public static void EdDSASign(ref Key s, ref Key e, ref Key y, ref Key p, ref Key x, ref Key m) => Native.Native.Instance.EdDSASign(ref s, ref e, ref y, ref p, ref x, ref m);
        public static bool EdDSAVerify(ref Key s, ref Key e, ref Key y, ref Key m) => Native.Native.Instance.EdDSAVerify(ref s, ref e, ref y, ref m);
        public static void GenerateLinkingTag(ref Key J, ref Key r) => Native.Native.Instance.GenerateLinkingTag(ref J, ref r);
        public static Key GenerateLinkingTag(ref Key r) => Native.Native.Instance.GenerateLinkingTag1(ref r);

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
            sig.y = new Key();
            EdDSASign(ref sig.s, ref sig.e, ref sig.y, ref p, ref x, ref mk);
            return sig;
        }

        public static Signature Sign(ref Key x, ref Key p, Hash m)
        {
            byte[] bytes = m.GetBytes();
            Key mk = SHA256ToKey(SHA256.HashData(bytes));

            Signature sig = new Signature();
            sig.s = new Key();
            sig.e = new Key();
            sig.y = new Key();
            EdDSASign(ref sig.s, ref sig.e, ref sig.y, ref p, ref x, ref mk);
            return sig;
        }

        public static Signature Sign(ref Key x, ref Key p, SHA256 m)
        {
            Key mk = SHA256ToKey(m);

            Signature sig = new Signature();
            sig.s = new Key();
            sig.e = new Key();
            sig.y = new Key();
            EdDSASign(ref sig.s, ref sig.e, ref sig.y, ref p, ref x, ref mk);
            return sig;
        }

        public static Signature Sign(ref Key x, SHA256 m)
        {
            Key mk = SHA256ToKey(m);

            Key p = ScalarmultBase(ref x);

            Signature sig = new Signature();
            sig.s = new Key();
            sig.e = new Key();
            sig.y = new Key();
            EdDSASign(ref sig.s, ref sig.e, ref sig.y, ref p, ref x, ref mk);
            return sig;
        }

        public static Signature Sign(ref Key x, ref Key p, byte[] m)
        {
            Key mk = SHA256ToKey(SHA256.HashData(m));

            Signature sig = new Signature();
            sig.s = new Key();
            sig.e = new Key();
            sig.y = new Key();
            EdDSASign(ref sig.s, ref sig.e, ref sig.y, ref p, ref x, ref mk);
            return sig;
        }

        public static Signature Sign(ref Key x, ref Key p, Key m)
        {
            Signature sig = new Signature();
            sig.s = new Key();
            sig.e = new Key();
            sig.y = new Key();
            EdDSASign(ref sig.s, ref sig.e, ref sig.y, ref p, ref x, ref m);
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

        public static bool CheckForBalance(ref Key cscalar, ref Key ps, ref Key UXKey, int i)
        {
            byte[] txbytes = new byte[36];
            Array.Copy(cscalar.bytes, txbytes, 32);
            txbytes[32] = (byte)(i >> 24);
            txbytes[33] = (byte)((i >> 16) & 0xFF);
            txbytes[34] = (byte)((i >> 8) & 0xFF);
            txbytes[35] = (byte)(i & 0xFF);

            Key c = new Key(new byte[32]);
            HashOps.HashToScalar(ref c, txbytes, 36);

            Key cg = ScalarmultBase(ref c);
            Key psq = new Key(new byte[32]);
            SubKeys(ref psq, ref UXKey, ref cg);

            return psq.Equals(ps);
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

        public static void ScalarAdd(ref Key res, ref Key a, ref Key b) => Native.Native.Instance.ScalarAdd(ref res, ref a, ref b);
        public static void ScalarSub(ref Key res, ref Key a, ref Key b) => Native.Native.Instance.ScalarSub(ref res, ref a, ref b);

        public static int KeyArrayLength(Key[] k)
        {
            int i = 0;

            while (i < k.Length && k[i].bytes != null && !k[i].Equals(Key.Z)) i++;

            return i;
        }
    }
}
