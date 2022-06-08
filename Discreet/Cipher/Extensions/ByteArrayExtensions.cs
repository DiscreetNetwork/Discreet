using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Cipher.Extensions
{
    public static class ByteArrayExtensions
    {
        public static int Compare(this byte[] a, byte[] b)
        {
            if (a == null || b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            if (a.Length > b.Length) return 1;
            if (a.Length < b.Length) return -1;

            int i;

            for (i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    break;
                }
            }

            if (i == 32)
            {
                return 0;
            }
            else if (a[i] > b[i])
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        public static bool BEquals(this byte[] a, byte[] b) => Compare(a, b) == 0;
    }
}
