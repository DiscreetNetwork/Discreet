using System;
using System.Collections.Generic;
using System.Text;

namespace Discreet.Cipher
{
    public static class TestCipher
    {
        public static void Test(string[] args)
        {
            /* test KeyOps first */
            Key pk0, sk0;
            pk0 = new Key();
            sk0 = new Key();

            KeyOps.GeneratePubkey(ref pk0);
            KeyOps.GenerateSeckey(ref sk0);

            Key pkOfSk0 = KeyOps.ScalarmultBase(ref sk0);

            /* test signature first */
            string testMessage = "this is a test message for signature";
            Signature s = new Signature(sk0, pkOfSk0, testMessage);

            if (!s.Verify(pkOfSk0, testMessage))
            {
                Console.Error.WriteLine("Could not verify signature from keypair");
            }
        }
    }
}
