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

        public Triptych(Coin.Triptych proof)
        {
            J = proof.J;
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

        [DllImport(@"DiscreetCore.dll", EntryPoint = "triptych_prove", CallingConvention = CallingConvention.StdCall)]
        private static extern void triptych_prove(
                [In, Out] Triptych bp,
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 64, ArraySubType = UnmanagedType.Struct)] Key[] M,
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 64, ArraySubType = UnmanagedType.Struct)] Key[] P,
                [MarshalAs(UnmanagedType.Struct)] Key C_offset,
                [MarshalAs(UnmanagedType.U4)] uint l,
                [MarshalAs(UnmanagedType.Struct)] Key r,
                [MarshalAs(UnmanagedType.Struct)] Key s,
                [MarshalAs(UnmanagedType.Struct)] Key message);

        public static Triptych Prove(Key[] M, Key[] P, Key C_offset, uint l, Key r, Key s, Key message)
        {
            Triptych proof = new Triptych();

            triptych_prove(proof, M, P, C_offset, l, r, s, message);

            return proof;
        }

        [DllImport(@"DiscreetCore.dll", EntryPoint = "triptych_verify", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Verify(
                [In, Out] Triptych bp,
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 64, ArraySubType = UnmanagedType.Struct)] Key[] M,
                [MarshalAs(UnmanagedType.LPArray, SizeConst = 64, ArraySubType = UnmanagedType.Struct)] Key[] P,
                [MarshalAs(UnmanagedType.Struct)] Key C_offset,
                [MarshalAs(UnmanagedType.Struct)] Key message);
    }
}
