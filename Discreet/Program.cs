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

        /*static async Task Main(string[] args)
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
        /*stopWatch.Stop();

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
    }*/

        public static void Main(string[] args)
        {
            /*Key bv = new Key();
            Key BV = new Key();
            Key bs = new Key();
            Key BS = new Key();

            KeyOps.GenerateKeypair(ref bv, ref BV);
            KeyOps.GenerateKeypair(ref bs, ref BS);

            // DKSAP 
            Key R = new Key();
            Key T = new Key();
            try
            {
                KeyOps.DKSAP(ref R, ref T, ref BV, ref BS);
                Console.WriteLine($"R:  {BitConverter.ToString(R.bytes).Replace("-", string.Empty).ToLower()}");
                Console.WriteLine($"T:  {BitConverter.ToString(T.bytes).Replace("-", string.Empty).ToLower()}");
            }
            catch (System.Runtime.InteropServices.SEHException e)
            {
                Console.WriteLine(e.Message);
                return;
            }
            Key ecdh = new Key();
            KeyOps.ScalarmultKey(ref ecdh, ref R, ref bv);
            Console.WriteLine($"ecdh:  {BitConverter.ToString(ecdh.bytes).Replace("-", string.Empty).ToLower()}");
            Key c = new Key();
            HashOps.HashToScalar(ref c, ecdh.bytes, 32);
            Console.WriteLine($"c:  {BitConverter.ToString(c.bytes).Replace("-", string.Empty).ToLower()}");

            Key y = new Key();
            KeyOps.GenCommitmentMask(ref y, ref c);
            Console.WriteLine($"y:  {BitConverter.ToString(y.bytes).Replace("-", string.Empty).ToLower()}");

            byte[] buf = new byte[38];
            buf[0] = (byte)'a';
            buf[1] = (byte)'m';
            buf[2] = (byte)'o';
            buf[3] = (byte)'u';
            buf[4] = (byte)'n';
            buf[5] = (byte)'t';
            Array.Copy(c.bytes, 0, buf, 6, 32);

            Key g = new Key();
            HashOps.HashData(ref g, buf, 38);

            Console.WriteLine($"g:  {BitConverter.ToString(g.bytes).Replace("-", string.Empty).ToLower()}");

            ulong amount = KeyOps.RandomDisAmount(UInt64.MaxValue - 1);

            Key b = new Key();
            b.bytes = (byte[])g.bytes.Clone();

            byte[] amountBytes = BitConverter.GetBytes(amount);
            if (BitConverter.IsLittleEndian)
            {
                amountBytes.Reverse();
            }

            Console.WriteLine($"amt:  {BitConverter.ToString(amountBytes).Replace("-", string.Empty).ToLower()}");

            for (int i = 0; i < 8; i++)
            {
                b.bytes[i] ^= amountBytes[i];
            }

            Key amtKey = new Key(new byte[32]);
            for (int i = 0; i < 8; i++)
            {
                amtKey.bytes[i] = amountBytes[i];
            }

            Key amtKeyH = new Key();
            KeyOps.ScalarmultKey(ref amtKeyH, ref Key.H, ref amtKey);

            Console.WriteLine($"AH:  {BitConverter.ToString(amtKeyH.bytes).Replace("-", string.Empty).ToLower()}");


            Console.WriteLine($"b:  {BitConverter.ToString(b.bytes).Replace("-", string.Empty).ToLower()}");

            Key recoverAmount = new Key(new byte[32]);
            for (int i = 0; i < 32; i++)
            {
                recoverAmount.bytes[i] = (byte)((int)g.bytes[i] ^ (int)b.bytes[i]);
            }

            Console.WriteLine($"recAMT:  {BitConverter.ToString(recoverAmount.bytes).Replace("-", string.Empty).ToLower()}");


            Key P = new Key();
            KeyOps.AGBP(ref P, ref y, ref b, ref Key.H);

            Console.WriteLine($"Commitment:  {BitConverter.ToString(P.bytes).Replace("-", string.Empty).ToLower()}");

            // P - yG - gH = amount???????
            Key yg = new Key();
            KeyOps.ScalarmultBase(ref yg, ref y);

            Key bh = new Key();
            KeyOps.ScalarmultKey(ref bh, ref Key.H, ref b);

            Console.WriteLine($"BH:  {BitConverter.ToString(bh.bytes).Replace("-", string.Empty).ToLower()}");

            Key gh = new Key();
            KeyOps.ScalarmultKey(ref gh, ref Key.H, ref g);

            Console.WriteLine($"GH:  {BitConverter.ToString(gh.bytes).Replace("-", string.Empty).ToLower()}");

            Key testquestionmark = new Key(new byte[32]);

            for (int i = 0; i < 32; i++)
            {
                testquestionmark.bytes[i] = (byte)((int)amtKeyH.bytes[i] ^ (int)gh.bytes[i]);
            }

            Console.WriteLine($"???:  {BitConverter.ToString(testquestionmark.bytes).Replace("-", string.Empty).ToLower()}");


            Key pmyg = new Key();
            KeyOps.SubKeys(ref pmyg, ref P, ref yg);

            Console.WriteLine($"P minus yG:  {BitConverter.ToString(pmyg.bytes).Replace("-", string.Empty).ToLower()}");
            Key amt = new Key();
            KeyOps.SubKeys(ref amt, ref pmyg, ref gh);

            Console.WriteLine($"Public key:  {BitConverter.ToString(amt.bytes).Replace("-", string.Empty).ToLower()}");

            Key trueb = new Key(new byte[32]);

            byte[] bbytes = new byte[] { 0xf4, 0xaf, 0xee, 0x25, 0x50, 0x66, 0x67, 0x5a };

            for (int i = 0; i < bbytes.Length; i++)
            {
                trueb.bytes[i] = bbytes[i];
            }

            Key truebh = new Key();

            KeyOps.ScalarmultKey(ref truebh, ref Key.H, ref trueb);

            Console.WriteLine($"Public key:  {BitConverter.ToString(truebh.bytes).Replace("-", string.Empty).ToLower()}");*/

            Coin.Transaction tx = Coin.Transaction.GenerateMock();

            Console.WriteLine(Coin.Printable.Prettify(tx.Readable()));

            Console.WriteLine(tx.Readable().Length);
            Console.WriteLine(tx.Marshal().Length);

            Console.WriteLine(tx.Size());
            Console.WriteLine("\n\n" + Coin.Printable.Hexify(tx.Marshal()));
            Console.WriteLine("\n\n" + Coin.Printable.Hexify(tx.Outputs[0].TXMarshal()));
            Console.WriteLine("\n\n" + Coin.Printable.Hexify(tx.Outputs[1].TXMarshal()));
        }

    }
}
