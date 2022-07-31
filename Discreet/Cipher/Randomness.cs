using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Discreet.Cipher
{
    public static class Randomness
    {
        private static RNGCryptoServiceProvider csp;

        static Randomness()
        {
            csp = new RNGCryptoServiceProvider();
        }

        public static byte[] Random(uint len)
        {
            byte[] bytes = new byte[len];

            csp.GetBytes(bytes);

            return bytes;
        }
    }
}
