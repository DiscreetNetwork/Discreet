using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Triptych
    {
        [MarshalAs(UnmanagedType.Struct)]
        public Key J;

        [MarshalAs(UnmanagedType.Struct)]
        public Key K;

        [MarshalAs(UnmanagedType.Struct)]
        public Key A, B, C, D;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.Struct)]
        public Key[] X, Y, f;

        [MarshalAs(UnmanagedType.Struct)]
        public Key zA, zC, z;

        public Triptych(Coin.Triptych proof, Key linkingTag)
        {
            J = linkingTag;
            K = proof.K;
            A = proof.A;
            B = proof.B;
            C = proof.C;
            D = proof.D;

            X = proof.X;
            Y = proof.Y;
            f = proof.f;

            zA = proof.zA;
            zC = proof.zC;
            z = proof.z;
        }

        public Triptych(bool ignored_param)
        {
            J = new(new byte[32]);
            K = new(new byte[32]);
            A = new(new byte[32]);
            B = new(new byte[32]);
            C = new(new byte[32]);
            D = new(new byte[32]);

            X = new Key[6];
            Y = new Key[6];
            f = new Key[6];

            for (int i = 0; i < 6; i++)
            {
                X[i] = new(new byte[32]);
                Y[i] = new(new byte[32]);
                f[i] = new(new byte[32]);
            }

            zA = new(new byte[32]);
            zC = new(new byte[32]);
            z = new(new byte[32]);
        }

        [DllImport(@"DiscreetCore.dll", EntryPoint = "triptych_PROVE", CallingConvention = CallingConvention.StdCall)]
        private static extern Triptych triptych_prove(
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 64, ArraySubType = UnmanagedType.Struct)] Key[] M,
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 64, ArraySubType = UnmanagedType.Struct)] Key[] P,
                [MarshalAs(UnmanagedType.Struct)] Key C_offset,
                [MarshalAs(UnmanagedType.U4)] uint l,
                [MarshalAs(UnmanagedType.Struct)] Key r,
                [MarshalAs(UnmanagedType.Struct)] Key s,
                [MarshalAs(UnmanagedType.Struct)] Key message);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "GetLastException", CallingConvention = CallingConvention.StdCall)]
        private static extern void get_last_exception([In, Out] [MarshalAs(UnmanagedType.LPArray, SizeConst = 4096)] byte[] data);

        public static Triptych Prove(Key[] M, Key[] P, Key C_offset, uint l, Key r, Key s, Key message)
        {
            Triptych proof = new Triptych(false);

            try
            {
                proof = triptych_prove(M, P, C_offset, l, r, s, message);
            }
            catch(Exception e)
            {
                if (e is SEHException)
                {
                    byte[] dat = new byte[4096];
                    get_last_exception(dat);

                    string s_Excp = Encoding.ASCII.GetString(dat);

                    throw new Exception(s_Excp);
                }
            }
            return proof;
        }

        [DllImport(@"DiscreetCore.dll", EntryPoint = "triptych_VERIFY", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool triptych_VERIFY(
                [In, Out] Triptych bp,
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 64, ArraySubType = UnmanagedType.Struct)] Key[] M,
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 64, ArraySubType = UnmanagedType.Struct)] Key[] P,
                [MarshalAs(UnmanagedType.Struct)] Key C_offset,
                [MarshalAs(UnmanagedType.Struct)] Key message);

        public static bool Verify(Triptych bp, Key[] M, Key[] P, Key C_offset, Key message)
        {
            bool rv = false;

            try
            {
                rv = triptych_VERIFY(bp, M, P, C_offset, message);
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
    }
}
