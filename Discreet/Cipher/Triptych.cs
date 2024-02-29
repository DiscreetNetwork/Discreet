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

        public Triptych(Coin.Models.Triptych proof, Key linkingTag)
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

        private static Triptych triptych_prove(Key[] M, Key[] P, Key C_offset, uint l, Key r, Key s, Key message) => Native.Native.Instance.triptych_PROVE(M, P, C_offset, l, r, s, message);
        private static void get_last_exception(byte[] data) => Native.Native.Instance.GetLastException(data);

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

        public static bool triptych_VERIFY(Triptych bp, Key[] M, Key[] P, Key C_offset, Key message) => Native.Native.Instance.triptych_VERIFY(bp, M, P, C_offset, message) != 0;

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
