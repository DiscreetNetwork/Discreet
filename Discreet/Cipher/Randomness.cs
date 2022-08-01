using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Discreet.Cipher
{
    public static class Randomness
    {
        public static void Random([In, Out] byte[] data, uint len) => Native.Native.Instance.generate_random_bytes_thread_safe(data, len);

        public static byte[] Random(uint len)
        {
            byte[] bytes = new byte[len];

            Random(bytes, len);

            return bytes;
        }
    }
}
