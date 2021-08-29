using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Bulletproof
    {
        [MarshalAs(UnmanagedType.U8)]
        public ulong size;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = UnmanagedType.Struct)]
        public Key[] V;

        [MarshalAs(UnmanagedType.Struct)]
        public Key A, S, T1, T2, taux, mu;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10, ArraySubType = UnmanagedType.Struct)]
        public Key[] L, R;

        [MarshalAs(UnmanagedType.Struct)]
        public Key a, b, t;

        [DllImport(@"DiscreetCore.dll", EntryPoint = "bulletproof_PROVE", CallingConvention = CallingConvention.StdCall)]
        private static extern void bulletproof_PROVE([In, Out] Bulletproof bp, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16, ArraySubType = UnmanagedType.U8)] ulong[] v, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16, ArraySubType = UnmanagedType.Struct)] Key[] gamma, [MarshalAs(UnmanagedType.U4)] uint size);

        public static Bulletproof Prove(ulong[] v, Key[] gamma)
        {
            Bulletproof bp = new Bulletproof();

            ulong[] vArg = new ulong[16];
            Key[] gammaArg = new Key[16];
            for (int i = 0; i < v.Length; i++)
            {
                vArg[i] = v[i];
                gammaArg[i] = gamma[i];
            }

            bulletproof_PROVE(bp, vArg, gammaArg, (uint)v.Length);
            return bp;
        }

        public static Bulletproof Prove(ulong v, Key gamma)
        {
            ulong[] vArg = new ulong[] { v };
            Key[] gammaArg = new Key[] { gamma };

            return Prove(vArg, gammaArg);
        }
    }
}
