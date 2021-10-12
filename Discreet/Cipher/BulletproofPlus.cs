using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BulletproofPlus
    {
        [MarshalAs(UnmanagedType.U8)]
        public ulong size;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = UnmanagedType.Struct)]
        public Key[] V;

        [MarshalAs(UnmanagedType.Struct)]
        public Key A;
        [MarshalAs(UnmanagedType.Struct)]
        public Key A1;
        [MarshalAs(UnmanagedType.Struct)]
        public Key B;
        [MarshalAs(UnmanagedType.Struct)]
        public Key r1;
        [MarshalAs(UnmanagedType.Struct)]
        public Key s1;
        [MarshalAs(UnmanagedType.Struct)]
        public Key d1;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10, ArraySubType = UnmanagedType.Struct)]
        public Key[] L;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10, ArraySubType = UnmanagedType.Struct)]
        public Key[] R;

        public int Size()
        {
            int i = 0;

            while (i < 16 && V[i].bytes != null && !V[i].Equals(Key.Z)) i++;

            return i;
        }

        public BulletproofPlus(ulong sz)
        {
            size = sz;

            V = new Key[16];

            for (int i = 0; i < 16; i++)
            {
                V[i] = new(new byte[32]);
            }

            A = new(new byte[32]);
            A1 = new(new byte[32]);
            B = new(new byte[32]);
            r1 = new(new byte[32]);
            s1 = new(new byte[32]);
            d1 = new(new byte[32]);

            L = new Key[10];
            R = new Key[10];

            for (int i = 0; i < 10; i++)
            {
                L[i] = new(new byte[32]);
                R[i] = new(new byte[32]);
            }
        }

        [DllImport(@"DiscreetCore.dll", EntryPoint = "bulletproof_plus_prove", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Struct)]
        private static extern BulletproofPlus bulletproof_plus_PROVE([MarshalAs(UnmanagedType.LPArray, SizeConst = 16, ArraySubType = UnmanagedType.U8)] ulong[] v, [MarshalAs(UnmanagedType.LPArray, SizeConst = 16, ArraySubType = UnmanagedType.Struct)] Key[] gamma, [MarshalAs(UnmanagedType.U8)] ulong size);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "GetLastException", CallingConvention = CallingConvention.StdCall)]
        private static extern void get_last_exception([In, Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 4096)] byte[] data);

        public static BulletproofPlus Prove(ulong[] v, Key[] gamma)
        {
            BulletproofPlus bp = new BulletproofPlus((ulong)v.Length);

            ulong[] vArg = new ulong[16];
            Key[] gammaArg = new Key[16];
            for (int i = 0; i < v.Length; i++)
            {
                vArg[i] = v[i];
                gammaArg[i] = gamma[i];
            }

            for (int i = v.Length; i < 16; i++)
            {
                vArg[i] = 0;
                gammaArg[i] = new(new byte[32]);
            }

            try
            {
                bp = bulletproof_plus_PROVE(vArg, gammaArg, (ulong)v.Length);
            }
            catch (Exception e)
            {
                if (e is SEHException)
                {
                    byte[] dat = new byte[4096];
                    get_last_exception(dat);

                    string s_Excp = Encoding.ASCII.GetString(dat);

                    throw new Exception(s_Excp);
                }
            }

            return bp;
        }

        public static BulletproofPlus Prove(ulong v, Key gamma)
        {
            ulong[] vArg = new ulong[] { v };
            Key[] gammaArg = new Key[] { gamma };

            return Prove(vArg, gammaArg);
        }

        [DllImport(@"DiscreetCore.dll", EntryPoint = "bulletproof_plus_verify", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool bulletproof_plus_VERIFY(BulletproofPlus bp);

        public static bool Verify(BulletproofPlus bp)
        {
            bool rv = false;

            try
            {
                rv = bulletproof_plus_VERIFY(bp);
            }
            catch (Exception e)
            {
                if (e is SEHException)
                {
                    byte[] dat = new byte[4096];
                    get_last_exception(dat);

                    string s_Excp = Encoding.ASCII.GetString(dat);

                    throw new Exception(s_Excp);
                }
            }

            return rv;
        }

        public BulletproofPlus(Coin.BulletproofPlus bp, Key[] comms)
        {
            A = bp.A;
            A1 = bp.A1;
            B = bp.B;
            r1 = bp.r1;
            s1 = bp.s1;
            d1 = bp.d1;

            L = new Key[10];
            R = new Key[10];

            for (int i = 0; i < bp.L.Length; i++)
            {
                L[i] = bp.L[i];
                R[i] = bp.R[i];
            }

            for (int i = bp.L.Length; i < 10; i++)
            {
                L[i] = new(new byte[32]);
                R[i] = new(new byte[32]);
            }

            V = new Key[16];

            for (int i = 0; i < comms.Length; i++)
            {
                V[i] = comms[i];
            }

            for (int i = comms.Length; i < 16; i++)
            {
                V[i] = new(new byte[32]);
            }

            size = (ulong)comms.Length;
        }
    }
}
