using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom
{
    public class Utility
    {
        private static Random _random = new Random();

        public static byte[] RandomByteArray(int byteLength)
        {
            byte[] buffer = new byte[byteLength];
            _random.NextBytes(buffer);
            return buffer;
        }

        public static BitArray GetSharedBits(BitArray xBits, byte[] y)
        {
            BitArray yBits = new BitArray(y);

            List<bool> shared = new List<bool>();
            int count = xBits.Length - 1;
            int index = yBits.Length - 1;

            while (count >= 0 && xBits[count] == yBits[index])
            {
                shared.Insert(0, xBits[count]);
                --count;
                --index;
            }

            return new BitArray(shared.ToArray());
        }

        /// <summary>
        /// Returns a new BigInteger randomly chosen between a minimum and maximum. The BigInteger has a 0 byte appended, to force the sign to be positive
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static BigInteger GetRandomPositiveBigInteger(BigInteger min, BigInteger max)
        {
            // shift to 0...max-min
            BigInteger max2 = max - min;

            long bits = max2.GetBitLength();

            // 1 bit for sign (that we will ignore, we only want positive numbers!)
            bits++;

            // we round to the next byte
            long bytes = (bits + 7) / 8;

            long uselessBits = bytes * 8 - bits;

            var bytes2 = new byte[bytes];

            Random r = new Random();
            while (true)
            {
                r.NextBytes(bytes2);

                // The maximum number of useless bits is 1 (sign) + 7 (rounding) == 8
                if (uselessBits == 8)
                {
                    // and it is exactly one byte!
                    bytes2[0] = 0;
                }
                else
                {
                    // Remove the sign and the useless bits
                    for (int i = 0; i < uselessBits; i++)
                    {
                        //Equivalent to
                        //byte bit = (byte)(1 << (7 - (i % 8)));
                        byte bit = (byte)(1 << (7 & (~i)));

                        //Equivalent to
                        //bytes2[i / 8] &= (byte)~bit;
                        bytes2[i >> 3] &= (byte)~bit;
                    }
                }

                var bi = new BigInteger(bytes2.Concat(new byte[] { 0 }).ToArray()); // Force positive

                // If it is too much big, then retry!
                if (bi >= max2)
                {
                    continue;
                }

                // unshift the number
                bi += min;
                return bi;
            }
        }
    }
}
