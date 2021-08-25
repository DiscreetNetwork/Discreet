using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    public static class HashOps
    {
        [DllImport(@"DiscreetCore.dll", EntryPoint = "HashData", CallingConvention = CallingConvention.StdCall)]
        public static extern void HashData(ref Key hash, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] data, [MarshalAs(UnmanagedType.U4)] uint l);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "HashToScalar", CallingConvention = CallingConvention.StdCall)]
        public static extern void HashToScalar(ref Key hash, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] data, [MarshalAs(UnmanagedType.U4)] uint l);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "HashKey", CallingConvention = CallingConvention.StdCall)]
        public static extern void HashKey(ref Key hash, ref Key data);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "HashKeyToScalar", CallingConvention = CallingConvention.StdCall)]
        public static extern void HashKeyToScalar(ref Key hash, ref Key data);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "HashKey1", CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key HashKey(ref Key data);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "HashKeyToScalar1", CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key HashToScalar(ref Key data);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "HashData128", CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key HashData128([In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] data);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "HashToScalar128", CallingConvention = CallingConvention.StdCall)]
            [return: MarshalAs(UnmanagedType.Struct)]
        public static extern Key HashToScalar128([In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] data);

        [DllImport(@"DiscreetCore.dll", EntryPoint = "HashToP3", CallingConvention = CallingConvention.StdCall)]
        public static extern void HashToP3(ref GEP3 hash8_p3, ref Key k);
    }
}
