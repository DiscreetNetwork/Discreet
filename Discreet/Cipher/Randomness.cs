using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Discreet.Cipher
{
    public static class Randomness
    {
        [DllImport(@"DiscreetCore.dll", EntryPoint = "generate_random_bytes_thread_safe", CallingConvention = CallingConvention.StdCall)]
        public static extern void Random([In, Out] [MarshalAs(UnmanagedType.LPArray)] byte[] data, uint len);

        public static byte[] Random(uint len)
        {
            byte[] bytes = new byte[len];

            Random(bytes, len);

            return bytes;
        }
    }
}
