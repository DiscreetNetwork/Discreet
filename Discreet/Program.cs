using System;
using Discreet.Cipher;
using Discreet.Network.Helpers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Discreet
{
    class Program
    {
        static async Task<List<string>> DoWork()
        {
            List<string> properName = new List<string>();
            return await Task.Run(() =>
            {
                for (int i = 0; i < 2500; i++)
                {
                    Key vw = KeyOps.GeneratePubkey();
                    Key sp = KeyOps.GeneratePubkey();

                    Discreet.Coin.IAddress addr = new Discreet.Coin.StealthAddress(vw, sp);

                    //Console.WriteLine(i.ToString() + ": " + addr.String());

                    properName.Add(addr.String());

                    //Console.WriteLine($"Public key:  {BitConverter.ToString(pk.bytes).Replace("-", string.Empty).ToLower()}");
                }

                //Console.WriteLine("done.");

                return properName;
            });
        }

        /*static void Main(string[] args)
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

            /*Key sk2 = KeyOps.GenerateSeckey(); 


            Console.WriteLine($"Secret key:  {"0x" + BitConverter.ToString(sk2.bytes).Replace("-", ", 0x").ToLower()}");

            //Key pk2 = KeyOps.ScalarmultBase(ref sk2);
            Key pk2 = KeyOps.ScalarmultBase(ref sk2);

            Console.WriteLine($"Public key:  {"0x" + BitConverter.ToString(pk2.bytes).Replace("-", ", 0x").ToLower()}");

            Key pk3 = new Key();
            Key sk3 = KeyOps.GenerateSeckey();

            KeyOps.ScalarmultBase(ref pk3, ref sk3);

            Console.WriteLine($"Public key:  {BitConverter.ToString(pk3.bytes).Replace("-", string.Empty).ToLower()}");
            Console.WriteLine($"Secret key:  {BitConverter.ToString(sk3.bytes).Replace("-", string.Empty).ToLower()}");

            string m = "wrap";

            SHA256 hash = SHA256.HashData(UTF8Encoding.UTF8.GetBytes(m));

            Console.WriteLine($"SHA256:  {BitConverter.ToString(hash.GetBytes()).Replace("-", string.Empty).ToLower()}");

            Console.WriteLine($"Checksum: {BitConverter.ToString(PacketHelper.Checksum(UTF8Encoding.UTF8.GetBytes(m))).Replace("-", string.Empty).ToLower()}");
            */

            /*Console.ForegroundColor = ConsoleColor.Green;

            for (int i = 0; i < 10000; i++)
            {
                Key vw = KeyOps.GeneratePubkey();
                Key sp = KeyOps.GeneratePubkey();

                Discreet.Coin.IAddress addr = new Discreet.Coin.StealthAddress(vw, sp);

                Console.WriteLine(i.ToString() + ": " + addr.String());

                //Console.WriteLine($"Public key:  {BitConverter.ToString(pk.bytes).Replace("-", string.Empty).ToLower()}");
            }

            Console.WriteLine("done.");

            Console.ReadLine();
        }*/

        static async Task Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            Console.ForegroundColor = ConsoleColor.Green;

            List<Task<List<string>>> tasks = new List<Task<List<string>>>() { DoWork(), DoWork(), DoWork(), DoWork(), DoWork(), DoWork(), DoWork(), DoWork() };
            await Task.WhenAll(tasks);

            /*List<string> checker = new List<string>();

            foreach (Task<List<string>> t in tasks)
            {
                checker.AddRange(t.Result);
            }

            List<string> checker2 = (List<string>)checker.Distinct().ToList();

            if (checker2.Count() != checker.Count())
            {
                Console.WriteLine("brandon is no longer cool and does not win at video games");
            }

            Console.WriteLine(checker2.Count() + " " + checker.Count());
            */
            stopWatch.Stop();

            Stopwatch stopWatch2 = new Stopwatch();
            stopWatch2.Start();
            for (int i = 0; i < 2500 * 8; i++)
            {
                Key vw = KeyOps.GeneratePubkey();
                Key sp = KeyOps.GeneratePubkey();

                Discreet.Coin.IAddress addr = new Discreet.Coin.StealthAddress(vw, sp);

                //Console.WriteLine(i.ToString() + ": " + addr.String());

                //Console.WriteLine($"Public key:  {BitConverter.ToString(pk.bytes).Replace("-", string.Empty).ToLower()}");
            }

            stopWatch2.Stop();

            Console.WriteLine(stopWatch.ElapsedMilliseconds);
            Console.WriteLine(stopWatch2.ElapsedMilliseconds);

            Console.WriteLine("All Done.");
        }

    }
}
