using System;
using Discreet.Cipher;
using System.Text;

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

            /* Key pk = new Key();
            Key sk = new Key();

            KeyOps.GenerateKeypair(ref sk, ref pk);

            Console.WriteLine($"Public key:  {BitConverter.ToString(pk.bytes).Replace("-", string.Empty).ToLower()}");
            Console.WriteLine($"Secret key:  {BitConverter.ToString(sk.bytes).Replace("-", string.Empty).ToLower()}");

            Key pk1 = KeyOps.GeneratePubkey();

            Console.WriteLine($"Secret key:  {BitConverter.ToString(pk1.bytes).Replace("-", string.Empty).ToLower()}");*/

            Key sk2 = KeyOps.GenerateSeckey(); 


            Console.WriteLine($"Secret key:  {"0x" + BitConverter.ToString(sk2.bytes).Replace("-", ", 0x").ToLower()}");

            //Key pk2 = KeyOps.ScalarmultBase(ref sk2);
            Key pk2 = KeyOps.ScalarmultBase(ref sk2);

            Console.WriteLine($"Public key:  {"0x" + BitConverter.ToString(pk2.bytes).Replace("-", ", 0x").ToLower()}");

            Key pk3 = new Key();
            Key sk3 = KeyOps.GenerateSeckey();

            KeyOps.ScalarmultBase(ref pk3, ref sk3);

            Console.WriteLine($"Public key:  {BitConverter.ToString(pk3.bytes).Replace("-", string.Empty).ToLower()}");
            Console.WriteLine($"Secret key:  {BitConverter.ToString(sk3.bytes).Replace("-", string.Empty).ToLower()}");

            string m = "brap";

            SHA256 hash = SHA256.HashData(UTF8Encoding.UTF8.GetBytes(m));

            Console.WriteLine($"SHA256:  {BitConverter.ToString(hash.GetBytes()).Replace("-", string.Empty).ToLower()}");

            Console.ReadLine();
        }
    }
}
