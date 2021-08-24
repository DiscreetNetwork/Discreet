using System;
using System.Collections.Generic;
using System.Text;

namespace Discreet.Cipher
{
    public class TestCipher
    {
        public static void Main(string[] args)
        {
            /* test KeyOps first */
            Key pk0, sk0;
            pk0 = new Key();
            sk0 = new Key();

            KeyOps.GeneratePubkey(ref pk0);
            KeyOps.GenerateSeckey(ref sk0);

            Key pkOfSk0 = KeyOps.ScalarmultBase(ref sk0);

        }
    }
}
