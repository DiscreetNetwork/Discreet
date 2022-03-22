using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Cipher.Native
{
    public static class DiscreetCore
    {
        [DllImport("DiscreetCore")]
        public static extern void GenerateKeypair(ref Key sk, ref Key pk);

        [DllImport("DiscreetCore")]
        public static extern void ScalarmultKey(ref Key ap, ref Key p, ref Key a);

        [DllImport("DiscreetCore")]
        public static extern void DKSAP(ref Key R, ref Key T, ref Key pv, ref Key ps, int index);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key DKSAPRecover(ref Key t, ref Key R, ref Key sv, ref Key ss, int index);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key GenerateSeckey1();

        [DllImport("DiscreetCore")]
        public static extern void GenerateSeckey(ref Key sk);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key GeneratePubkey();

        [DllImport("DiscreetCore")]
        public static extern void ScalarmultBase(ref Key ag, ref Key a);

        [DllImport("DiscreetCore")]
        public static extern void GenCommitment(ref Key c, ref Key a, [MarshalAs(UnmanagedType.U8)] ulong amount);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key Commit(ref Key a, [MarshalAs(UnmanagedType.U8)] ulong amount);

        [DllImport("DiscreetCore")]
        [return : MarshalAs(UnmanagedType.Struct)]
        public static extern Key CommitToZero([MarshalAs(UnmanagedType.U8)] ulong amount);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.U8)]
        public static extern ulong RandomDisAmount([MarshalAs(UnmanagedType.U8)] ulong limit);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key ScalarmultH(ref Key a);

        [DllImport("DiscreetCore")]
        public static extern void Scalarmult8(ref GEP3 res, ref Key p);

        [DllImport("DiscreetCore")]
        public static extern void scalarmult_8_correct(ref Key res, ref Key p);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key Scalarmult81(ref Key p);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool InMainSubgroup(ref Key a);

        [DllImport("DiscreetCore")]
        public static extern void AddKeys(ref Key ab, ref Key a, ref Key b);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key AddKeys_1(ref Key a, ref Key b);

        /* computes agb = aG + B */
        [DllImport("DiscreetCore")]
        public static extern void AddKeys1(ref Key agb, ref Key a, ref Key b);

        [DllImport("DiscreetCore")]
        public static extern void AddKeys2(ref Key agbp, ref Key a, ref Key b, ref Key p);

        [DllImport("DiscreetCore")]
        public static extern void Precomp([In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] rv, ref Key b);

        [DllImport("DiscreetCore")]
        public static extern void AddKeys3(ref Key apbq, ref Key a, ref Key p, ref Key b, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] q);

        [DllImport("DiscreetCore")]
        public static extern void AddKeys3_1(ref Key apbq, ref Key a, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] p, ref Key b, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] q);

        [DllImport("DiscreetCore")]
        public static extern void AddKeys4(ref Key agbpcq, ref Key a, ref Key b, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] p, ref Key c, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] q);

        [DllImport("DiscreetCore")]
        public static extern void AddKeys5(ref Key agbpcq, ref Key a, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] p, ref Key b, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] q, ref Key c, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] r);

        [DllImport("DiscreetCore")]
        public static extern void SubKeys(ref Key ab, ref Key a, ref Key b);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EqualKeys(ref Key a, ref Key b);

        [DllImport("DiscreetCore")]
        public static extern void GenCommitmentMask(ref Key rv, ref Key sk);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key ECDHEncode(ref ECDHTuple unmasked, ref Key secret, [MarshalAs(UnmanagedType.Bool)] bool v2);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key ECDHDecode(ref ECDHTuple masked, ref Key secret, [MarshalAs(UnmanagedType.Bool)] bool v2);

        [DllImport("DiscreetCore")]
        public static extern void EdDSASign(ref Key s, ref Key e, ref Key y, ref Key p, ref Key x, ref Key m);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EdDSAVerify(ref Key s, ref Key e, ref Key y, ref Key m);

        [DllImport("DiscreetCore")]
        public static extern void GenerateLinkingTag(ref Key J, ref Key r);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key GenerateLinkingTag1(ref Key r);

        [DllImport("DiscreetCore")]
        public static extern void ScalarAdd(ref Key res, ref Key a, ref Key b);

        [DllImport("DiscreetCore")]
        public static extern void ScalarSub(ref Key res, ref Key a, ref Key b);

        [DllImport("DiscreetCore")]
        public static extern void pbkdf2([In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] output,
                                         [MarshalAs(UnmanagedType.LPArray)] byte[] password,
                                         [MarshalAs(UnmanagedType.U4)] uint password_len,
                                         [MarshalAs(UnmanagedType.LPArray)] byte[] salt,
                                         [MarshalAs(UnmanagedType.U4)] uint salt_len,
                                         [MarshalAs(UnmanagedType.U4)] uint num_iterations,
                                         [MarshalAs(UnmanagedType.U4)] uint key_len);

        [DllImport("DiscreetCore")]
        public static extern void generate_random_bytes_thread_safe([In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] data, uint len);

        [DllImport("DiscreetCore")]
        public static extern void ripemd160_init(RIPEMD160Ctx state);

        [DllImport("DiscreetCore")]
        public static extern void ripemd160_update(RIPEMD160Ctx state, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] _in, ulong inlen);

        [DllImport("DiscreetCore")]
        public static extern void ripemd160_final(RIPEMD160Ctx state, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 64)] byte[] _out);

        [DllImport("DiscreetCore")]
        public static extern void ripemd160([In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] datain, ulong inlen, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 20)] byte[] dataout);

        [DllImport("DiscreetCore")]
        public static extern void HashData(ref Key hash, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] data, [MarshalAs(UnmanagedType.U4)] uint l);

        [DllImport("DiscreetCore")]
        public static extern void HashToScalar(ref Key hash, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] data, [MarshalAs(UnmanagedType.U4)] uint l);

        [DllImport("DiscreetCore")]
        public static extern void HashKey(ref Key hash, ref Key data);

        [DllImport("DiscreetCore")]
        public static extern void HashKeyToScalar(ref Key hash, ref Key data);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key HashKey1(ref Key data);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key HashKeyToScalar1(ref Key data);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key HashData128([In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] data);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key HashToScalar128([In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] data);

        [DllImport("DiscreetCore")]
        public static extern void HashToP3(ref GEP3 hash8_p3, ref Key k);

        [DllImport("DiscreetCore")]
        public static extern int keccak_init(KeccakCtx state);

        [DllImport("DiscreetCore")]
        public static extern int keccak_update(KeccakCtx state, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] _in, ulong inlen);

        [DllImport("DiscreetCore")]
        public static extern int keccak_final(KeccakCtx state, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 32)] byte[] _out);

        [DllImport("DiscreetCore")]
        public static extern void keccak([In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] datain, uint inlen, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] dataout, uint dlen);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.I4)]
        public static extern int sha256_init(SHA256Ctx state);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.I4)]
        public static extern int sha256_update(SHA256Ctx state, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] _in, ulong inlen);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.I4)]
        public static extern int sha256_final(SHA256Ctx state, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 32)] byte[] _out);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.I4)]
        public static extern int sha256([In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 32)] byte[] dataout, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] datain, ulong len);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.I4)]
        public static extern int sha512_init(SHA512Ctx state);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.I4)]
        public static extern int sha512_update(SHA512Ctx state, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] _in, ulong inlen);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.I4)]
        public static extern int sha512_final(SHA512Ctx state, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 64)] byte[] _out);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.I4)]
        public static extern int sha512([In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 64)] byte[] dataout, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] datain, ulong len);

        [DllImport("DiscreetCore")]
        public static extern void GetLastException([In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 4096)] byte[] data);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Bulletproof bulletproof_prove([MarshalAs(UnmanagedType.LPArray, SizeConst = 16, ArraySubType = UnmanagedType.U8)] ulong[] v, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16, ArraySubType = UnmanagedType.Struct)] Key[] gamma, [MarshalAs(UnmanagedType.U8)] ulong size);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool bulletproof_verify(Bulletproof bp);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern BulletproofPlus bulletproof_plus_prove([MarshalAs(UnmanagedType.LPArray, SizeConst = 16, ArraySubType = UnmanagedType.U8)] ulong[] v, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16, ArraySubType = UnmanagedType.Struct)] Key[] gamma, [MarshalAs(UnmanagedType.U8)] ulong size);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool bulletproof_plus_verify(BulletproofPlus bp);

        [DllImport("DiscreetCore")]
        public static extern Triptych triptych_PROVE(
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 64, ArraySubType = UnmanagedType.Struct)] Key[] M,
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 64, ArraySubType = UnmanagedType.Struct)] Key[] P,
                [MarshalAs(UnmanagedType.Struct)] Key C_offset,
                [MarshalAs(UnmanagedType.U4)] uint l,
                [MarshalAs(UnmanagedType.Struct)] Key r,
                [MarshalAs(UnmanagedType.Struct)] Key s,
                [MarshalAs(UnmanagedType.Struct)] Key message);

        [DllImport("DiscreetCore")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool triptych_VERIFY(
                [In, Out] Triptych bp,
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 64, ArraySubType = UnmanagedType.Struct)] Key[] M,
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 64, ArraySubType = UnmanagedType.Struct)] Key[] P,
                [MarshalAs(UnmanagedType.Struct)] Key C_offset,
                [MarshalAs(UnmanagedType.Struct)] Key message);
    }
}
