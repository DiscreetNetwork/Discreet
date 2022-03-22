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
        public Key A;
        [MarshalAs(UnmanagedType.Struct)]
        public Key S;
        [MarshalAs(UnmanagedType.Struct)]
        public Key T1;
        [MarshalAs(UnmanagedType.Struct)]
        public Key T2;
        [MarshalAs(UnmanagedType.Struct)]
        public Key taux;
        [MarshalAs(UnmanagedType.Struct)]
        public Key mu;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10, ArraySubType = UnmanagedType.Struct)]
        public Key[] L;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10, ArraySubType = UnmanagedType.Struct)]
        public Key[] R;

        [MarshalAs(UnmanagedType.Struct)]
        public Key a;
        [MarshalAs(UnmanagedType.Struct)]
        public Key b;
        [MarshalAs(UnmanagedType.Struct)]
        public Key t;

        public int Size()
        {
            int i = 0;

            while (i < 16 && V[i].bytes != null && !V[i].Equals(Key.Z)) i++;

            return i;
        }

        public Bulletproof(ulong sz)
        {
            size = sz;

            V = new Key[16];

            for (int i = 0; i < 16; i++)
            {
                V[i] = new(new byte[32]);
            }

            A = new(new byte[32]);
            S = new(new byte[32]);
            T1 = new(new byte[32]);
            T2 = new(new byte[32]);
            taux = new(new byte[32]);
            mu = new(new byte[32]);

            L = new Key[10];
            R = new Key[10];

            for (int i = 0; i < 10; i++)
            {
                L[i] = new(new byte[32]);
                R[i] = new(new byte[32]);
            }

            a = new(new byte[32]);
            b = new(new byte[32]);
            t = new(new byte[32]);
        }

        private static Bulletproof bulletproof_PROVE(ulong[] v, Key[] gamma, ulong size) => Native.Native.Instance.bulletproof_prove(v, gamma, size);
        private static void get_last_exception(byte[] data) => Native.Native.Instance.GetLastException(data);

        public static Bulletproof Prove(ulong[] v, Key[] gamma)
        {
            Bulletproof bp = new Bulletproof((ulong)v.Length);

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
                bp = bulletproof_PROVE(vArg, gammaArg, (ulong)v.Length);
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

        public static Bulletproof Prove(ulong v, Key gamma)
        {
            ulong[] vArg = new ulong[] { v };
            Key[] gammaArg = new Key[] { gamma };

            return Prove(vArg, gammaArg);
        }

        private static bool bulletproof_VERIFY(Bulletproof bp) => Native.Native.Instance.bulletproof_verify(bp);

        public static bool Verify(Bulletproof bp)
        {
            bool rv = false;
            
            try
            {
                rv = bulletproof_VERIFY(bp);
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

        public Bulletproof(Coin.Bulletproof bp, Key[] comms)
        {
            A = bp.A;
            S = bp.S;
            T1 = bp.T1;
            T2 = bp.T2;
            taux = bp.taux;
            mu = bp.mu;

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

            a = bp.a;
            b = bp.b;
            t = bp.t;

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
