using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Discreet.Network.Peerbloom.Extensions
{
    public static class BigInteger
    {
        public static Cipher.Key ToKey(this System.Numerics.BigInteger i)
        {
            byte[] bytes = i.ToByteArray();
            Cipher.Key rv = new Cipher.Key(new byte[32]);
            Array.Copy(bytes, rv.bytes, (bytes.Length > 32) ? 32 : bytes.Length);

            return rv;
        }
    }
}
