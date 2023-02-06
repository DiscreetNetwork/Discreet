using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Wallets.Extensions
{
    public static class StringEx
    {
        private static readonly string hexdigits = "0123456789abcdef";

        public static bool IsHex(this string s)
        {
            return s.ToLower().Any(c => !((('0' <= c) && (c <= '9')) || (('a' <= c) && (c <= 'f'))));
        }

        public static byte[] HexToBytes(this string s)
        {
            if (s.Length % 2 != 0 || !s.IsHex())
            {
                throw new ArgumentException(nameof(s));
            }

            s = s.ToLower();
            byte[] bytes = new byte[s.Length / 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)((hexdigits.IndexOf(s[2 * i]) << 4) | hexdigits.IndexOf(s[2 * i + 1]));
            }

            return bytes;
        }
    }
}
