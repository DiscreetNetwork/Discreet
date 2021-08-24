using System;
using Discreet.Cipher;

namespace Discreet
{
    class Program
    {
        static void Main(string[] args)
        {


            /* Development playground.
             * An idiot admires complexity; a genius admires simplicity.
             * 
             * 
             */

            Key pk = new Key();
            Key sk = new Key();

            KeyOps.GenerateKeypair(ref sk, ref pk);

            Console.WriteLine($"Public key:  {BitConverter.ToString(pk.bytes).Replace("-", string.Empty).ToLower()}");
            Console.WriteLine($"Secret key:  {BitConverter.ToString(sk.bytes).Replace("-", string.Empty).ToLower()}");


            //TestCipher.Test(args);

            Key pk1 = KeyOps.GeneratePubkey();

            Console.WriteLine($"Secret key:  {BitConverter.ToString(pk1.bytes).Replace("-", string.Empty).ToLower()}");

            Console.ReadLine();
        }
    }
}
