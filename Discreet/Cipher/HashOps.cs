using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    public static class HashOps
    {
        public static void HashData(ref Key hash, byte[] data, uint l) => Native.Native.Instance.HashData(ref hash, data, l);
        public static void HashToScalar(ref Key hash, byte[] data, uint l) => Native.Native.Instance.HashToScalar(ref hash, data, l);
        public static void HashKey(ref Key hash, ref Key data) => Native.Native.Instance.HashKey(ref hash, ref data);
        public static void HashKeyToScalar(ref Key hash, ref Key data) => Native.Native.Instance.HashKeyToScalar(ref hash, ref data);
        public static Key HashKey(ref Key data) => Native.Native.Instance.HashKey1(ref data);
        public static Key HashToScalar(ref Key data) => Native.Native.Instance.HashKeyToScalar1(ref data);
        public static Key HashData128(byte[] data) => Native.Native.Instance.HashData128(data);
        public static Key HashToScalar128(byte[] data) => Native.Native.Instance.HashToScalar128(data);
        public static void HashToP3(ref GEP3 hash8_p3, ref Key k) => Native.Native.Instance.HashToP3(ref hash8_p3, ref k);
    }
}
