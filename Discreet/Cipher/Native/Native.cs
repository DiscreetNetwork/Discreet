using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Discreet.Cipher.Native
{
    public class Native: IDisposable
    {
        public static Native Instance;

        private static IntPtr _handle = IntPtr.Zero;

        public delegate void GenerateKeypairDelegate(ref Key sk, ref Key pk);
        public delegate void ScalarmultKeyDelegate(ref Key ap, ref Key p, ref Key a);
        public delegate void DKSAPDelegate(ref Key R, ref Key T, ref Key pv, ref Key ps, int index);
        public delegate void DKSAPRecoverDelegate(ref Key t, ref Key r, ref Key sv, ref Key ss, int index);
        public delegate Key GenerateSeckey1Delegate();
        public delegate void GenerateSeckeyDelegate(ref Key sk);
        public delegate Key GeneratePubkeyDelegate();
        public delegate void ScalarmultBaseDelegate(ref Key ag, ref Key a);
        public delegate void GenCommitmentDelegate(ref Key c, ref Key a, [MarshalAs(UnmanagedType.U8)] ulong amount);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate Key CommitDelegate(ref Key a, [MarshalAs(UnmanagedType.U8)] ulong amount);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate Key CommitToZeroDelegate([MarshalAs(UnmanagedType.U8)] ulong amount);
        [return: MarshalAs(UnmanagedType.U8)]
        public delegate ulong RandomDisAmountDelegate([MarshalAs(UnmanagedType.U8)] ulong limit);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate Key ScalarmultHDelegate(ref Key a);
        public delegate void Scalarmult8Delegate(ref GEP3 res, ref Key p);
        public delegate void scalarmult_8_correctDelegate(ref Key res, ref Key p);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate Key Scalarmult81Delegate(ref Key p);
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool InMainSubgroupDelegate(ref Key a);
        public delegate void AddKeysDelegate(ref Key ab, ref Key a, ref Key b);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate Key AddKeys_1Delegate(ref Key a, ref Key b);
        public delegate void AddKeys1Delegate(ref Key agb, ref Key a, ref Key b);
        public delegate void AddKeys2Delegate(ref Key agbp, ref Key a, ref Key b, ref Key p);
        public delegate void PrecompDelegate([In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] rv, ref Key b);
        public delegate void AddKeys3Delegate(ref Key apbq, ref Key a, ref Key p, ref Key b, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] q);
        public delegate void AddKeys3_1Delegate(ref Key apbq, ref Key a, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] p, ref Key b, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] q);
        public delegate void AddKeys4Delegate(ref Key agbpcq, ref Key a, ref Key b, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] p, ref Key c, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] q);
        public delegate void AddKeys5Delegate(ref Key agbpcq, ref Key a, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] p, ref Key b, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] q, ref Key c, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 8, ArraySubType = UnmanagedType.Struct)] GEPrecomp[] r);
        public delegate void SubKeysDelegate(ref Key ab, ref Key a, ref Key b);
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool EqualKeysDelegate(ref Key a, ref Key b);
        public delegate void GenCommitmentMaskDelegate(ref Key rv, ref Key sk);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate Key ECDHEncodeDelegate(ref ECDHTuple unmasked, ref Key secret, [MarshalAs(UnmanagedType.Bool)] bool v2);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate Key ECDHDecodeDelegate(ref ECDHTuple masked, ref Key secret, [MarshalAs(UnmanagedType.Bool)] bool v2);
        public delegate void EdDSASignDelegate(ref Key s, ref Key e, ref Key y, ref Key p, ref Key x, ref Key m);
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool EdDSAVerifyDelegate(ref Key s, ref Key e, ref Key y, ref Key m);
        public delegate void GenerateLinkingTagDelegate(ref Key J, ref Key r);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate Key GenerateLinkingTag1Delegate(ref Key r);
        public delegate void ScalarAddDelegate(ref Key res, ref Key a, ref Key b);
        public delegate void ScalarSubDelegate(ref Key res, ref Key a, ref Key b);
        public delegate void pbkdf2Delegate([In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] output,
                                         [MarshalAs(UnmanagedType.LPArray)] byte[] password,
                                         [MarshalAs(UnmanagedType.U4)] uint password_len,
                                         [MarshalAs(UnmanagedType.LPArray)] byte[] salt,
                                         [MarshalAs(UnmanagedType.U4)] uint salt_len,
                                         [MarshalAs(UnmanagedType.U4)] uint num_iterations,
                                         [MarshalAs(UnmanagedType.U4)] uint key_len);
        public delegate void generate_random_bytes_thread_safeDelegate([In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] data, uint len);
        public delegate void ripemd160_initDelegate(RIPEMD160Ctx state);
        public delegate void ripemd160_updateDelegate(RIPEMD160Ctx state, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] _in, ulong inlen);
        public delegate void ripemd160_finalDelegate(RIPEMD160Ctx state, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 64)] byte[] _out);
        public delegate void ripemd160Delegate([In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] datain, ulong inlen, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 20)] byte[] dataout);
        public delegate void HashDataDelegate(ref Key hash, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] data, [MarshalAs(UnmanagedType.U4)] uint l);
        public delegate void HashToScalarDelegate(ref Key hash, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] data, [MarshalAs(UnmanagedType.U4)] uint l);
        public delegate void HashKeyDelegate(ref Key hash, ref Key data);
        public delegate void HashKeyToScalarDelegate(ref Key hash, ref Key data);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate Key HashKey1Delegate(ref Key data);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate Key HashKeyToScalar1Delegate(ref Key data);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate Key HashData128Delegate([In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] data);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate Key HashToScalar128Delegate([In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] data);
        public delegate void HashToP3Delegate(ref GEP3 hash8_p3, ref Key k);
        public delegate int keccak_initDelegate(KeccakCtx state);
        public delegate int keccak_updateDelegate(KeccakCtx state, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] _in, ulong inlen);
        public delegate int keccak_finalDelegate(KeccakCtx state, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 32)] byte[] _out);
        public delegate void keccakDelegate([In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] datain, uint inlen, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] dataout, uint dlen);
        [return: MarshalAs(UnmanagedType.I4)]
        public delegate int sha256_initDelegate(SHA256Ctx state);
        [return: MarshalAs(UnmanagedType.I4)]
        public delegate int sha256_updateDelegate(SHA256Ctx state, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] _in, ulong inlen);
        [return: MarshalAs(UnmanagedType.I4)]
        public delegate int sha256_finalDelegate(SHA256Ctx state, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 32)] byte[] _out);
        [return: MarshalAs(UnmanagedType.I4)]
        public delegate int sha256Delegate([In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 32)] byte[] dataout, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] datain, ulong len);
        [return: MarshalAs(UnmanagedType.I4)]
        public delegate int sha512_initDelegate(SHA512Ctx state);
        [return: MarshalAs(UnmanagedType.I4)]
        public delegate int sha512_updateDelegate(SHA512Ctx state, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] _in, ulong inlen);
        [return: MarshalAs(UnmanagedType.I4)]
        public delegate int sha512_finalDelegate(SHA512Ctx state, [In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 64)] byte[] _out);
        [return: MarshalAs(UnmanagedType.I4)]
        public delegate int sha512Delegate([In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 64)] byte[] dataout, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] datain, ulong len);
        public delegate void GetLastExceptionDelegate([In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 4096)] byte[] data);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate Bulletproof bulletproof_proveDelegate([MarshalAs(UnmanagedType.LPArray, SizeConst = 16, ArraySubType = UnmanagedType.U8)] ulong[] v, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16, ArraySubType = UnmanagedType.Struct)] Key[] gamma, [MarshalAs(UnmanagedType.U8)] ulong size);
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool bulletproof_verifyDelegate(Bulletproof bp);
        [return: MarshalAs(UnmanagedType.Struct)]
        public delegate BulletproofPlus bulletproof_plus_proveDelegate([MarshalAs(UnmanagedType.LPArray, SizeConst = 16, ArraySubType = UnmanagedType.U8)] ulong[] v, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16, ArraySubType = UnmanagedType.Struct)] Key[] gamma, [MarshalAs(UnmanagedType.U8)] ulong size);
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool bulletproof_plus_verifyDelegate(BulletproofPlus bp);
        public delegate Triptych triptych_PROVEDelegate(
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 64, ArraySubType = UnmanagedType.Struct)] Key[] M,
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 64, ArraySubType = UnmanagedType.Struct)] Key[] P,
                [MarshalAs(UnmanagedType.Struct)] Key C_offset,
                [MarshalAs(UnmanagedType.U4)] uint l,
                [MarshalAs(UnmanagedType.Struct)] Key r,
                [MarshalAs(UnmanagedType.Struct)] Key s,
                [MarshalAs(UnmanagedType.Struct)] Key message);
        [return: MarshalAs(UnmanagedType.U1)]
        public delegate byte triptych_VERIFYDelegate(
                [In, Out] Triptych bp,
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 64, ArraySubType = UnmanagedType.Struct)] Key[] M,
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 64, ArraySubType = UnmanagedType.Struct)] Key[] P,
                [MarshalAs(UnmanagedType.Struct)] Key C_offset,
                [MarshalAs(UnmanagedType.Struct)] Key message);

        public GenerateKeypairDelegate GenerateKeypair;
        public ScalarmultKeyDelegate ScalarmultKey;
        public DKSAPDelegate DKSAP;
        public DKSAPRecoverDelegate DKSAPRecover;
        public GenerateSeckey1Delegate GenerateSeckey1;
        public GenerateSeckeyDelegate GenerateSeckey;
        public GeneratePubkeyDelegate GeneratePubkey;
        public ScalarmultBaseDelegate ScalarmultBase;
        public GenCommitmentDelegate GenCommitment;
        public CommitDelegate Commit;
        public CommitToZeroDelegate CommitToZero;
        public RandomDisAmountDelegate RandomDisAmount;
        public ScalarmultHDelegate ScalarmultH;
        public Scalarmult8Delegate Scalarmult8;
        public scalarmult_8_correctDelegate scalarmult_8_correct;
        public Scalarmult81Delegate Scalarmult81;
        public InMainSubgroupDelegate InMainSubgroup;
        public AddKeysDelegate AddKeys;
        public AddKeys_1Delegate AddKeys_1;
        public AddKeys1Delegate AddKeys1;
        public AddKeys2Delegate AddKeys2;
        public PrecompDelegate Precomp;
        public AddKeys3Delegate AddKeys3;
        public AddKeys3_1Delegate AddKeys3_1;
        public AddKeys4Delegate AddKeys4;
        public AddKeys5Delegate AddKeys5;
        public SubKeysDelegate SubKeys;
        public EqualKeysDelegate EqualKeys;
        public GenCommitmentMaskDelegate GenCommitmentMask;
        public ECDHEncodeDelegate ECDHEncode;
        public ECDHDecodeDelegate ECDHDecode;
        public EdDSASignDelegate EdDSASign;
        public EdDSAVerifyDelegate EdDSAVerify;
        public GenerateLinkingTagDelegate GenerateLinkingTag;
        public GenerateLinkingTag1Delegate GenerateLinkingTag1;
        public ScalarAddDelegate ScalarAdd;
        public ScalarSubDelegate ScalarSub;
        public pbkdf2Delegate pbkdf2;
        public generate_random_bytes_thread_safeDelegate generate_random_bytes_thread_safe;
        public ripemd160_initDelegate ripemd160_init;
        public ripemd160_updateDelegate ripemd160_update;
        public ripemd160_finalDelegate ripemd160_final;
        public ripemd160Delegate ripemd160;
        public HashDataDelegate HashData;
        public HashToScalarDelegate HashToScalar;
        public HashKeyDelegate HashKey;
        public HashKeyToScalarDelegate HashKeyToScalar;
        public HashKey1Delegate HashKey1;
        public HashKeyToScalar1Delegate HashKeyToScalar1;
        public HashData128Delegate HashData128;
        public HashToScalar128Delegate HashToScalar128;
        public HashToP3Delegate HashToP3;
        public keccak_initDelegate keccak_init;
        public keccak_updateDelegate keccak_update;
        public keccak_finalDelegate keccak_final;
        public keccakDelegate keccak;
        public sha256_initDelegate sha256_init;
        public sha256_updateDelegate sha256_update;
        public sha256_finalDelegate sha256_final;
        public sha256Delegate sha256;
        public sha512_initDelegate sha512_init;
        public sha512_updateDelegate sha512_update;
        public sha512_finalDelegate sha512_final;
        public sha512Delegate sha512;
        public GetLastExceptionDelegate GetLastException;
        public bulletproof_proveDelegate bulletproof_prove;
        public bulletproof_verifyDelegate bulletproof_verify;
        public bulletproof_plus_proveDelegate bulletproof_plus_prove;
        public bulletproof_plus_verifyDelegate bulletproof_plus_verify;
        public triptych_PROVEDelegate triptych_PROVE;
        public triptych_VERIFYDelegate triptych_VERIFY;

        static Native()
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.X86 && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException("Discreet cannot support 32 bit windows");
            }

            bool success;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                success = NativeLibrary.TryLoad("DiscreetCore.dll", typeof(DiscreetCore).Assembly, DllImportSearchPath.AssemblyDirectory, out _handle);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                success = NativeLibrary.TryLoad("DiscreetCore.so", typeof(DiscreetCore).Assembly, DllImportSearchPath.AssemblyDirectory, out _handle);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                success = NativeLibrary.TryLoad("DiscreetCore.dylib", typeof(DiscreetCore).Assembly, DllImportSearchPath.AssemblyDirectory, out _handle);
            }
            else
            {
                success = NativeLibrary.TryLoad("DiscreetCore.dll", typeof(DiscreetCore).Assembly, DllImportSearchPath.AssemblyDirectory, out _handle);
            }

            if (!success)
            {
                throw new PlatformNotSupportedException("Failed to load \"DiscreetCore\" on this platform");
            }

            Instance = new Native();

            // begin loading endpoints
            if (NativeLibrary.TryGetExport(_handle, "GenerateKeypair", out IntPtr _GenerateKeypairHandle))
            {
                Instance.GenerateKeypair = Marshal.GetDelegateForFunctionPointer<GenerateKeypairDelegate>(_GenerateKeypairHandle);
            }
            else
            {
                Instance.GenerateKeypair = (ref Key sk, ref Key pk) => { throw new EntryPointNotFoundException("failed to find endpoint \"GenerateKeypair\" in library \"DiscreetCore\""); };
            }

			if (NativeLibrary.TryGetExport(_handle, "ScalarmultKey", out IntPtr _ScalarmultKeyHandle))
			{
				Instance.ScalarmultKey = Marshal.GetDelegateForFunctionPointer<ScalarmultKeyDelegate>(_ScalarmultKeyHandle);
			}
			else
			{
				Instance.ScalarmultKey = (ref Key ap, ref Key p, ref Key a) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarmultKey\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "DKSAP", out IntPtr _DKSAPHandle))
			{
				Instance.DKSAP = Marshal.GetDelegateForFunctionPointer<DKSAPDelegate>(_DKSAPHandle);
			}
			else
			{
				Instance.DKSAP = (ref Key R, ref Key T, ref Key pv, ref Key ps, int index) => { throw new EntryPointNotFoundException("failed to find endpoint \"DKSAP\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "DKSAPRecover", out IntPtr _DKSAPRecoverHandle))
			{
				Instance.DKSAPRecover = Marshal.GetDelegateForFunctionPointer<DKSAPRecoverDelegate>(_DKSAPRecoverHandle);
			}
			else
			{
				Instance.DKSAPRecover = (ref Key t, ref Key r, ref Key sv, ref Key ss, int index) => { throw new EntryPointNotFoundException("failed to find endpoint \"DKSAPRecover\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "GenerateSeckey1", out IntPtr _GenerateSeckey1Handle))
			{
				Instance.GenerateSeckey1 = Marshal.GetDelegateForFunctionPointer<GenerateSeckey1Delegate>(_GenerateSeckey1Handle);
			}
			else
			{
				Instance.GenerateSeckey1 = () => { throw new EntryPointNotFoundException("failed to find endpoint \"GenerateSeckey1\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "GenerateSeckey", out IntPtr _GenerateSeckeyHandle))
			{
				Instance.GenerateSeckey = Marshal.GetDelegateForFunctionPointer<GenerateSeckeyDelegate>(_GenerateSeckeyHandle);
			}
			else
			{
				Instance.GenerateSeckey = (ref Key sk) => { throw new EntryPointNotFoundException("failed to find endpoint \"GenerateSeckey\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "GeneratePubkey", out IntPtr _GeneratePubkeyHandle))
			{
				Instance.GeneratePubkey = Marshal.GetDelegateForFunctionPointer<GeneratePubkeyDelegate>(_GeneratePubkeyHandle);
			}
			else
			{
				Instance.GeneratePubkey = () => { throw new EntryPointNotFoundException("failed to find endpoint \"GeneratePubkey\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "ScalarmultBase", out IntPtr _ScalarmultBaseHandle))
			{
				Instance.ScalarmultBase = Marshal.GetDelegateForFunctionPointer<ScalarmultBaseDelegate>(_ScalarmultBaseHandle);
			}
			else
			{
				Instance.ScalarmultBase = (ref Key ag, ref Key a) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarmultBase\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "GenCommitment", out IntPtr _GenCommitmentHandle))
			{
				Instance.GenCommitment = Marshal.GetDelegateForFunctionPointer<GenCommitmentDelegate>(_GenCommitmentHandle);
			}
			else
			{
				Instance.GenCommitment = (ref Key c, ref Key a, ulong amount) => { throw new EntryPointNotFoundException("failed to find endpoint \"GenCommitment\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "Commit", out IntPtr _CommitHandle))
			{
				Instance.Commit = Marshal.GetDelegateForFunctionPointer<CommitDelegate>(_CommitHandle);
			}
			else
			{
				Instance.Commit = (ref Key a, ulong amount) => { throw new EntryPointNotFoundException("failed to find endpoint \"Commit\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "CommitToZero", out IntPtr _CommitToZeroHandle))
			{
				Instance.CommitToZero = Marshal.GetDelegateForFunctionPointer<CommitToZeroDelegate>(_CommitToZeroHandle);
			}
			else
			{
				Instance.CommitToZero = (ulong amount) => { throw new EntryPointNotFoundException("failed to find endpoint \"CommitToZero\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "RandomDisAmount", out IntPtr _RandomDisAmountHandle))
			{
				Instance.RandomDisAmount = Marshal.GetDelegateForFunctionPointer<RandomDisAmountDelegate>(_RandomDisAmountHandle);
			}
			else
			{
				Instance.RandomDisAmount = (ulong limit) => { throw new EntryPointNotFoundException("failed to find endpoint \"RandomDisAmount\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "ScalarmultH", out IntPtr _ScalarmultHHandle))
			{
				Instance.ScalarmultH = Marshal.GetDelegateForFunctionPointer<ScalarmultHDelegate>(_ScalarmultHHandle);
			}
			else
			{
				Instance.ScalarmultH = (ref Key a) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarmultH\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "Scalarmult8", out IntPtr _Scalarmult8Handle))
			{
				Instance.Scalarmult8 = Marshal.GetDelegateForFunctionPointer<Scalarmult8Delegate>(_Scalarmult8Handle);
			}
			else
			{
				Instance.Scalarmult8 = (ref GEP3 res, ref Key p) => { throw new EntryPointNotFoundException("failed to find endpoint \"Scalarmult8\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "scalarmult_8_correct", out IntPtr _scalarmult_8_correctHandle))
			{
				Instance.scalarmult_8_correct = Marshal.GetDelegateForFunctionPointer<scalarmult_8_correctDelegate>(_scalarmult_8_correctHandle);
			}
			else
			{
				Instance.scalarmult_8_correct = (ref Key res, ref Key p) => { throw new EntryPointNotFoundException("failed to find endpoint \"scalarmult_8_correct\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "Scalarmult81", out IntPtr _Scalarmult81Handle))
			{
				Instance.Scalarmult81 = Marshal.GetDelegateForFunctionPointer<Scalarmult81Delegate>(_Scalarmult81Handle);
			}
			else
			{
				Instance.Scalarmult81 = (ref Key p) => { throw new EntryPointNotFoundException("failed to find endpoint \"Scalarmult81\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "InMainSubgroup", out IntPtr _InMainSubgroupHandle))
			{
				Instance.InMainSubgroup = Marshal.GetDelegateForFunctionPointer<InMainSubgroupDelegate>(_InMainSubgroupHandle);
			}
			else
			{
				Instance.InMainSubgroup = (ref Key a) => { throw new EntryPointNotFoundException("failed to find endpoint \"InMainSubgroup\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "AddKeys", out IntPtr _AddKeysHandle))
			{
				Instance.AddKeys = Marshal.GetDelegateForFunctionPointer<AddKeysDelegate>(_AddKeysHandle);
			}
			else
			{
				Instance.AddKeys = (ref Key ab, ref Key a, ref Key b) => { throw new EntryPointNotFoundException("failed to find endpoint \"AddKeys\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "AddKeys_1", out IntPtr _AddKeys_1Handle))
			{
				Instance.AddKeys_1 = Marshal.GetDelegateForFunctionPointer<AddKeys_1Delegate>(_AddKeys_1Handle);
			}
			else
			{
				Instance.AddKeys_1 = (ref Key a, ref Key b) => { throw new EntryPointNotFoundException("failed to find endpoint \"AddKeys_1\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "AddKeys1", out IntPtr _AddKeys1Handle))
			{
				Instance.AddKeys1 = Marshal.GetDelegateForFunctionPointer<AddKeys1Delegate>(_AddKeys1Handle);
			}
			else
			{
				Instance.AddKeys1 = (ref Key agb, ref Key a, ref Key b) => { throw new EntryPointNotFoundException("failed to find endpoint \"AddKeys1\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "AddKeys2", out IntPtr _AddKeys2Handle))
			{
				Instance.AddKeys2 = Marshal.GetDelegateForFunctionPointer<AddKeys2Delegate>(_AddKeys2Handle);
			}
			else
			{
				Instance.AddKeys2 = (ref Key agbp, ref Key a, ref Key b, ref Key p) => { throw new EntryPointNotFoundException("failed to find endpoint \"AddKeys2\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "Precomp", out IntPtr _PrecompHandle))
			{
				Instance.Precomp = Marshal.GetDelegateForFunctionPointer<PrecompDelegate>(_PrecompHandle);
			}
			else
			{
				Instance.Precomp = (GEPrecomp[] rv, ref Key b) => { throw new EntryPointNotFoundException("failed to find endpoint \"Precomp\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "AddKeys3", out IntPtr _AddKeys3Handle))
			{
				Instance.AddKeys3 = Marshal.GetDelegateForFunctionPointer<AddKeys3Delegate>(_AddKeys3Handle);
			}
			else
			{
				Instance.AddKeys3 = (ref Key apbq, ref Key a, ref Key p, ref Key b, GEPrecomp[] q) => { throw new EntryPointNotFoundException("failed to find endpoint \"AddKeys3\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "AddKeys3_1", out IntPtr _AddKeys3_1Handle))
			{
				Instance.AddKeys3_1 = Marshal.GetDelegateForFunctionPointer<AddKeys3_1Delegate>(_AddKeys3_1Handle);
			}
			else
			{
				Instance.AddKeys3_1 = (ref Key apbq, ref Key a, GEPrecomp[] p, ref Key b, GEPrecomp[] q) => { throw new EntryPointNotFoundException("failed to find endpoint \"AddKeys3_1\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "AddKeys4", out IntPtr _AddKeys4Handle))
			{
				Instance.AddKeys4 = Marshal.GetDelegateForFunctionPointer<AddKeys4Delegate>(_AddKeys4Handle);
			}
			else
			{
				Instance.AddKeys4 = (ref Key agbpcq, ref Key a, ref Key b, GEPrecomp[] p, ref Key c, GEPrecomp[] q) => { throw new EntryPointNotFoundException("failed to find endpoint \"AddKeys4\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "AddKeys5", out IntPtr _AddKeys5Handle))
			{
				Instance.AddKeys5 = Marshal.GetDelegateForFunctionPointer<AddKeys5Delegate>(_AddKeys5Handle);
			}
			else
			{
				Instance.AddKeys5 = (ref Key agbpcq, ref Key a, GEPrecomp[] p, ref Key b, GEPrecomp[] q, ref Key c, GEPrecomp[] r) => { throw new EntryPointNotFoundException("failed to find endpoint \"AddKeys5\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "SubKeys", out IntPtr _SubKeysHandle))
			{
				Instance.SubKeys = Marshal.GetDelegateForFunctionPointer<SubKeysDelegate>(_SubKeysHandle);
			}
			else
			{
				Instance.SubKeys = (ref Key ab, ref Key a, ref Key b) => { throw new EntryPointNotFoundException("failed to find endpoint \"SubKeys\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "EqualKeys", out IntPtr _EqualKeysHandle))
			{
				Instance.EqualKeys = Marshal.GetDelegateForFunctionPointer<EqualKeysDelegate>(_EqualKeysHandle);
			}
			else
			{
				Instance.EqualKeys = (ref Key a, ref Key b) => { throw new EntryPointNotFoundException("failed to find endpoint \"EqualKeys\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "GenCommitmentMask", out IntPtr _GenCommitmentMaskHandle))
			{
				Instance.GenCommitmentMask = Marshal.GetDelegateForFunctionPointer<GenCommitmentMaskDelegate>(_GenCommitmentMaskHandle);
			}
			else
			{
				Instance.GenCommitmentMask = (ref Key rv, ref Key sk) => { throw new EntryPointNotFoundException("failed to find endpoint \"GenCommitmentMask\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "ECDHEncode", out IntPtr _ECDHEncodeHandle))
			{
				Instance.ECDHEncode = Marshal.GetDelegateForFunctionPointer<ECDHEncodeDelegate>(_ECDHEncodeHandle);
			}
			else
			{
				Instance.ECDHEncode = (ref ECDHTuple unmasked, ref Key secret, bool v2) => { throw new EntryPointNotFoundException("failed to find endpoint \"ECDHEncode\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "ECDHDecode", out IntPtr _ECDHDecodeHandle))
			{
				Instance.ECDHDecode = Marshal.GetDelegateForFunctionPointer<ECDHDecodeDelegate>(_ECDHDecodeHandle);
			}
			else
			{
				Instance.ECDHDecode = (ref ECDHTuple masked, ref Key secret, bool v2) => { throw new EntryPointNotFoundException("failed to find endpoint \"ECDHDecode\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "EdDSASign", out IntPtr _EdDSASignHandle))
			{
				Instance.EdDSASign = Marshal.GetDelegateForFunctionPointer<EdDSASignDelegate>(_EdDSASignHandle);
			}
			else
			{
				Instance.EdDSASign = (ref Key s, ref Key e, ref Key y, ref Key p, ref Key x, ref Key m) => { throw new EntryPointNotFoundException("failed to find endpoint \"EdDSASign\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "EdDSAVerify", out IntPtr _EdDSAVerifyHandle))
			{
				Instance.EdDSAVerify = Marshal.GetDelegateForFunctionPointer<EdDSAVerifyDelegate>(_EdDSAVerifyHandle);
			}
			else
			{
				Instance.EdDSAVerify = (ref Key s, ref Key e, ref Key y, ref Key m) => { throw new EntryPointNotFoundException("failed to find endpoint \"EdDSAVerify\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "GenerateLinkingTag", out IntPtr _GenerateLinkingTagHandle))
			{
				Instance.GenerateLinkingTag = Marshal.GetDelegateForFunctionPointer<GenerateLinkingTagDelegate>(_GenerateLinkingTagHandle);
			}
			else
			{
				Instance.GenerateLinkingTag = (ref Key J, ref Key r) => { throw new EntryPointNotFoundException("failed to find endpoint \"GenerateLinkingTag\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "GenerateLinkingTag1", out IntPtr _GenerateLinkingTag1Handle))
			{
				Instance.GenerateLinkingTag1 = Marshal.GetDelegateForFunctionPointer<GenerateLinkingTag1Delegate>(_GenerateLinkingTag1Handle);
			}
			else
			{
				Instance.GenerateLinkingTag1 = (ref Key r) => { throw new EntryPointNotFoundException("failed to find endpoint \"GenerateLinkingTag1\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "ScalarAdd", out IntPtr _ScalarAddHandle))
			{
				Instance.ScalarAdd = Marshal.GetDelegateForFunctionPointer<ScalarAddDelegate>(_ScalarAddHandle);
			}
			else
			{
				Instance.ScalarAdd = (ref Key res, ref Key a, ref Key b) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarAdd\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "ScalarSub", out IntPtr _ScalarSubHandle))
			{
				Instance.ScalarSub = Marshal.GetDelegateForFunctionPointer<ScalarSubDelegate>(_ScalarSubHandle);
			}
			else
			{
				Instance.ScalarSub = (ref Key res, ref Key a, ref Key b) => { throw new EntryPointNotFoundException("failed to find endpoint \"ScalarSub\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "pbkdf2", out IntPtr _pbkdf2Handle))
			{
				Instance.pbkdf2 = Marshal.GetDelegateForFunctionPointer<pbkdf2Delegate>(_pbkdf2Handle);
			}
			else
			{
				Instance.pbkdf2 = (byte[] output, byte[] password, uint password_len, byte[] salt, uint salt_len, uint num_iterations, uint key_len) => { throw new EntryPointNotFoundException("failed to find endpoint \"pbkdf2\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "generate_random_bytes_thread_safe", out IntPtr _generate_random_bytes_thread_safeHandle))
			{
				Instance.generate_random_bytes_thread_safe = Marshal.GetDelegateForFunctionPointer<generate_random_bytes_thread_safeDelegate>(_generate_random_bytes_thread_safeHandle);
			}
			else
			{
				Instance.generate_random_bytes_thread_safe = (byte[] data, uint len) => { throw new EntryPointNotFoundException("failed to find endpoint \"generate_random_bytes_thread_safe\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "ripemd160_init", out IntPtr _ripemd160_initHandle))
			{
				Instance.ripemd160_init = Marshal.GetDelegateForFunctionPointer<ripemd160_initDelegate>(_ripemd160_initHandle);
			}
			else
			{
				Instance.ripemd160_init = (RIPEMD160Ctx state) => { throw new EntryPointNotFoundException("failed to find endpoint \"ripemd160_init\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "ripemd160_update", out IntPtr _ripemd160_updateHandle))
			{
				Instance.ripemd160_update = Marshal.GetDelegateForFunctionPointer<ripemd160_updateDelegate>(_ripemd160_updateHandle);
			}
			else
			{
				Instance.ripemd160_update = (RIPEMD160Ctx state, byte[] _in, ulong inlen) => { throw new EntryPointNotFoundException("failed to find endpoint \"ripemd160_update\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "ripemd160_final", out IntPtr _ripemd160_finalHandle))
			{
				Instance.ripemd160_final = Marshal.GetDelegateForFunctionPointer<ripemd160_finalDelegate>(_ripemd160_finalHandle);
			}
			else
			{
				Instance.ripemd160_final = (RIPEMD160Ctx state, byte[] _out) => { throw new EntryPointNotFoundException("failed to find endpoint \"ripemd160_final\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "ripemd160", out IntPtr _ripemd160Handle))
			{
				Instance.ripemd160 = Marshal.GetDelegateForFunctionPointer<ripemd160Delegate>(_ripemd160Handle);
			}
			else
			{
				Instance.ripemd160 = (byte[] datain, ulong inlen, byte[] dataout) => { throw new EntryPointNotFoundException("failed to find endpoint \"ripemd160\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "HashData", out IntPtr _HashDataHandle))
			{
				Instance.HashData = Marshal.GetDelegateForFunctionPointer<HashDataDelegate>(_HashDataHandle);
			}
			else
			{
				Instance.HashData = (ref Key hash, byte[] data, uint l) => { throw new EntryPointNotFoundException("failed to find endpoint \"HashData\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "HashToScalar", out IntPtr _HashToScalarHandle))
			{
				Instance.HashToScalar = Marshal.GetDelegateForFunctionPointer<HashToScalarDelegate>(_HashToScalarHandle);
			}
			else
			{
				Instance.HashToScalar = (ref Key hash, byte[] data, uint l) => { throw new EntryPointNotFoundException("failed to find endpoint \"HashToScalar\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "HashKey", out IntPtr _HashKeyHandle))
			{
				Instance.HashKey = Marshal.GetDelegateForFunctionPointer<HashKeyDelegate>(_HashKeyHandle);
			}
			else
			{
				Instance.HashKey = (ref Key hash, ref Key data) => { throw new EntryPointNotFoundException("failed to find endpoint \"HashKey\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "HashKeyToScalar", out IntPtr _HashKeyToScalarHandle))
			{
				Instance.HashKeyToScalar = Marshal.GetDelegateForFunctionPointer<HashKeyToScalarDelegate>(_HashKeyToScalarHandle);
			}
			else
			{
				Instance.HashKeyToScalar = (ref Key hash, ref Key data) => { throw new EntryPointNotFoundException("failed to find endpoint \"HashKeyToScalar\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "HashKey1", out IntPtr _HashKey1Handle))
			{
				Instance.HashKey1 = Marshal.GetDelegateForFunctionPointer<HashKey1Delegate>(_HashKey1Handle);
			}
			else
			{
				Instance.HashKey1 = (ref Key data) => { throw new EntryPointNotFoundException("failed to find endpoint \"HashKey1\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "HashKeyToScalar1", out IntPtr _HashKeyToScalar1Handle))
			{
				Instance.HashKeyToScalar1 = Marshal.GetDelegateForFunctionPointer<HashKeyToScalar1Delegate>(_HashKeyToScalar1Handle);
			}
			else
			{
				Instance.HashKeyToScalar1 = (ref Key data) => { throw new EntryPointNotFoundException("failed to find endpoint \"HashKeyToScalar1\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "HashData128", out IntPtr _HashData128Handle))
			{
				Instance.HashData128 = Marshal.GetDelegateForFunctionPointer<HashData128Delegate>(_HashData128Handle);
			}
			else
			{
				Instance.HashData128 = (byte[] data) => { throw new EntryPointNotFoundException("failed to find endpoint \"HashData128\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "HashToScalar128", out IntPtr _HashToScalar128Handle))
			{
				Instance.HashToScalar128 = Marshal.GetDelegateForFunctionPointer<HashToScalar128Delegate>(_HashToScalar128Handle);
			}
			else
			{
				Instance.HashToScalar128 = (byte[] data) => { throw new EntryPointNotFoundException("failed to find endpoint \"HashToScalar128\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "HashToP3", out IntPtr _HashToP3Handle))
			{
				Instance.HashToP3 = Marshal.GetDelegateForFunctionPointer<HashToP3Delegate>(_HashToP3Handle);
			}
			else
			{
				Instance.HashToP3 = (ref GEP3 hash8_p3, ref Key k) => { throw new EntryPointNotFoundException("failed to find endpoint \"HashToP3\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "keccak_init", out IntPtr _keccak_initHandle))
			{
				Instance.keccak_init = Marshal.GetDelegateForFunctionPointer<keccak_initDelegate>(_keccak_initHandle);
			}
			else
			{
				Instance.keccak_init = (KeccakCtx state) => { throw new EntryPointNotFoundException("failed to find endpoint \"keccak_init\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "keccak_update", out IntPtr _keccak_updateHandle))
			{
				Instance.keccak_update = Marshal.GetDelegateForFunctionPointer<keccak_updateDelegate>(_keccak_updateHandle);
			}
			else
			{
				Instance.keccak_update = (KeccakCtx state, byte[] _in, ulong inlen) => { throw new EntryPointNotFoundException("failed to find endpoint \"keccak_update\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "keccak_final", out IntPtr _keccak_finalHandle))
			{
				Instance.keccak_final = Marshal.GetDelegateForFunctionPointer<keccak_finalDelegate>(_keccak_finalHandle);
			}
			else
			{
				Instance.keccak_final = (KeccakCtx state, byte[] _out) => { throw new EntryPointNotFoundException("failed to find endpoint \"keccak_final\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "keccak", out IntPtr _keccakHandle))
			{
				Instance.keccak = Marshal.GetDelegateForFunctionPointer<keccakDelegate>(_keccakHandle);
			}
			else
			{
				Instance.keccak = (byte[] datain, uint inlen, byte[] dataout, uint dlen) => { throw new EntryPointNotFoundException("failed to find endpoint \"keccak\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "sha256_init", out IntPtr _sha256_initHandle))
			{
				Instance.sha256_init = Marshal.GetDelegateForFunctionPointer<sha256_initDelegate>(_sha256_initHandle);
			}
			else
			{
				Instance.sha256_init = (SHA256Ctx state) => { throw new EntryPointNotFoundException("failed to find endpoint \"sha256_init\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "sha256_update", out IntPtr _sha256_updateHandle))
			{
				Instance.sha256_update = Marshal.GetDelegateForFunctionPointer<sha256_updateDelegate>(_sha256_updateHandle);
			}
			else
			{
				Instance.sha256_update = (SHA256Ctx state, byte[] _in, ulong inlen) => { throw new EntryPointNotFoundException("failed to find endpoint \"sha256_update\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "sha256_final", out IntPtr _sha256_finalHandle))
			{
				Instance.sha256_final = Marshal.GetDelegateForFunctionPointer<sha256_finalDelegate>(_sha256_finalHandle);
			}
			else
			{
				Instance.sha256_final = (SHA256Ctx state, byte[] _out) => { throw new EntryPointNotFoundException("failed to find endpoint \"sha256_final\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "sha256", out IntPtr _sha256Handle))
			{
				Instance.sha256 = Marshal.GetDelegateForFunctionPointer<sha256Delegate>(_sha256Handle);
			}
			else
			{
				Instance.sha256 = (byte[] dataout, byte[] datain, ulong len) => { throw new EntryPointNotFoundException("failed to find endpoint \"sha256\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "sha512_init", out IntPtr _sha512_initHandle))
			{
				Instance.sha512_init = Marshal.GetDelegateForFunctionPointer<sha512_initDelegate>(_sha512_initHandle);
			}
			else
			{
				Instance.sha512_init = (SHA512Ctx state) => { throw new EntryPointNotFoundException("failed to find endpoint \"sha512_init\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "sha512_update", out IntPtr _sha512_updateHandle))
			{
				Instance.sha512_update = Marshal.GetDelegateForFunctionPointer<sha512_updateDelegate>(_sha512_updateHandle);
			}
			else
			{
				Instance.sha512_update = (SHA512Ctx state, byte[] _in, ulong inlen) => { throw new EntryPointNotFoundException("failed to find endpoint \"sha512_update\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "sha512_final", out IntPtr _sha512_finalHandle))
			{
				Instance.sha512_final = Marshal.GetDelegateForFunctionPointer<sha512_finalDelegate>(_sha512_finalHandle);
			}
			else
			{
				Instance.sha512_final = (SHA512Ctx state, byte[] _out) => { throw new EntryPointNotFoundException("failed to find endpoint \"sha512_final\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "sha512", out IntPtr _sha512Handle))
			{
				Instance.sha512 = Marshal.GetDelegateForFunctionPointer<sha512Delegate>(_sha512Handle);
			}
			else
			{
				Instance.sha512 = (byte[] dataout, byte[] datain, ulong len) => { throw new EntryPointNotFoundException("failed to find endpoint \"sha512\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "GetLastException", out IntPtr _GetLastExceptionHandle))
			{
				Instance.GetLastException = Marshal.GetDelegateForFunctionPointer<GetLastExceptionDelegate>(_GetLastExceptionHandle);
			}
			else
			{
				Instance.GetLastException = (byte[] data) => { throw new EntryPointNotFoundException("failed to find endpoint \"GetLastException\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "bulletproof_prove", out IntPtr _bulletproof_proveHandle))
			{
				Instance.bulletproof_prove = Marshal.GetDelegateForFunctionPointer<bulletproof_proveDelegate>(_bulletproof_proveHandle);
			}
			else
			{
				Instance.bulletproof_prove = (ulong[] v, Key[] gamma, ulong size) => { throw new EntryPointNotFoundException("failed to find endpoint \"bulletproof_prove\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "bulletproof_verify", out IntPtr _bulletproof_verifyHandle))
			{
				Instance.bulletproof_verify = Marshal.GetDelegateForFunctionPointer<bulletproof_verifyDelegate>(_bulletproof_verifyHandle);
			}
			else
			{
				Instance.bulletproof_verify = (Bulletproof bp) => { throw new EntryPointNotFoundException("failed to find endpoint \"bulletproof_verify\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "bulletproof_plus_prove", out IntPtr _bulletproof_plus_proveHandle))
			{
				Instance.bulletproof_plus_prove = Marshal.GetDelegateForFunctionPointer<bulletproof_plus_proveDelegate>(_bulletproof_plus_proveHandle);
			}
			else
			{
				Instance.bulletproof_plus_prove = (ulong[] v, Key[] gamma, ulong size) => { throw new EntryPointNotFoundException("failed to find endpoint \"bulletproof_plus_prove\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "bulletproof_plus_verify", out IntPtr _bulletproof_plus_verifyHandle))
			{
				Instance.bulletproof_plus_verify = Marshal.GetDelegateForFunctionPointer<bulletproof_plus_verifyDelegate>(_bulletproof_plus_verifyHandle);
			}
			else
			{
				Instance.bulletproof_plus_verify = (BulletproofPlus bp) => { throw new EntryPointNotFoundException("failed to find endpoint \"bulletproof_plus_verify\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "triptych_PROVE", out IntPtr _triptych_PROVEHandle))
			{
				Instance.triptych_PROVE = Marshal.GetDelegateForFunctionPointer<triptych_PROVEDelegate>(_triptych_PROVEHandle);
			}
			else
			{
				Instance.triptych_PROVE = (Key[] M, Key[] P, Key C_offset, uint l, Key r, Key s, Key message) => { throw new EntryPointNotFoundException("failed to find endpoint \"triptych_PROVE\" in library \"DiscreetCore\""); };
			}

			if (NativeLibrary.TryGetExport(_handle, "triptych_VERIFY", out IntPtr _triptych_VERIFYHandle))
			{
				Instance.triptych_VERIFY = Marshal.GetDelegateForFunctionPointer<triptych_VERIFYDelegate>(_triptych_VERIFYHandle);
			}
			else
			{
				Instance.triptych_VERIFY = (Triptych bp, Key[] M, Key[] P, Key C_offset, Key message) => { throw new EntryPointNotFoundException("failed to find endpoint \"triptych_VERIFY\" in library \"DiscreetCore\""); };
			}
		}

		private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                NativeLibrary.Free(_handle);

                disposedValue = true;
            }
        }
        ~Native()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
