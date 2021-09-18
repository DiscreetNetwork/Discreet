using System;
using Discreet.Cipher;
using Discreet.Network.Helpers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Cryptography.ECDSA;

namespace Discreet
{
	class Program
	{
		/*public class TaskData
		/*public class TaskData
		{
			public Key vw, sp, VW, SP;
			public Coin.StealthAddress addr;
			public ulong i;

			public TaskData(Key _vw, Key _sp, Key _VW, Key _SP, Coin.StealthAddress _addr, ulong _i)
			{
				vw = _vw;
				sp = _sp;
				VW = _VW;
				SP = _SP;
				addr = _addr;
				i = _i;
			}
		}

		static async Task<TaskData> DoWork()
		{
			return await Task.Run(() =>
			{
				Key vw = new Key(), VW = new Key(), sp = new Key(), SP = new Key();
				KeyOps.GenerateKeypair(ref vw, ref VW);
				KeyOps.GenerateKeypair(ref sp, ref SP);
				Discreet.Coin.StealthAddress addr = new Discreet.Coin.StealthAddress(VW, SP);

				ulong i = 0;

				while (eval(addr.String()))
				{
					KeyOps.GenerateKeypair(ref vw, ref VW);
					KeyOps.GenerateKeypair(ref sp, ref SP);
					addr = new Discreet.Coin.StealthAddress(VW, SP);
					i++;
					if (i % 1000 == 0)
					{
						Console.WriteLine($"Private view key:  {BitConverter.ToString(vw.bytes).Replace("-", string.Empty).ToLower()}");
						Console.WriteLine($"Private spend key:  {BitConverter.ToString(sp.bytes).Replace("-", string.Empty).ToLower()}");
						Console.WriteLine($"Public view key:  {BitConverter.ToString(VW.bytes).Replace("-", string.Empty).ToLower()}");
						Console.WriteLine($"Public spend key:  {BitConverter.ToString(SP.bytes).Replace("-", string.Empty).ToLower()}");
						Console.WriteLine($"qqq???: {addr.String()}\nppp???: {addr.String().Length}");

						Console.WriteLine($"{i} addresses checked.");
					}
				}

				return new TaskData(vw, sp, VW, SP, addr, i);
			});
		}*/

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

			List<Task<TaskData>> tasks = new List<Task<TaskData>>() { DoWork(), DoWork(), DoWork(), DoWork(), DoWork(), DoWork(), DoWork(), DoWork(), DoWork(), DoWork(), DoWork(), DoWork(), DoWork(), DoWork(), DoWork(), DoWork() };
			
			Task<TaskData> res = await Task.WhenAny(tasks);

			TaskData rv = res.Result;

			Console.WriteLine($"Private view key:  {BitConverter.ToString(rv.vw.bytes).Replace("-", string.Empty).ToLower()}");
			Console.WriteLine($"Private spend key:  {BitConverter.ToString(rv.sp.bytes).Replace("-", string.Empty).ToLower()}");
			Console.WriteLine($"Public view key:  {BitConverter.ToString(rv.VW.bytes).Replace("-", string.Empty).ToLower()}");
			Console.WriteLine($"Public spend key:  {BitConverter.ToString(rv.SP.bytes).Replace("-", string.Empty).ToLower()}");
			Console.WriteLine($"Address: {rv.addr.String()}");
			Console.WriteLine($"Estimated # of attempts: {rv.i} * 16 = {rv.i * 16}");
		}
		public static bool eval(string arg)
		{
			return true;
		}*/

		/*public struct BTCAddress
        {
			public byte[] bytes;
			public byte[] privkey;

			public string addr;
        }

		public static BTCAddress GenerateBTCwallet()
        {
			bytes[0] = 0;
			byte[] bytes = new byte[25];
			byte[] privkey = Secp256K1Manager.GenerateRandomKey();
			byte[] pubkey = Secp256K1Manager.GetPublicKey(privkey, true);

			byte[] hash1 = SHA256.HashData(pubkey).Bytes;
			byte[] hash2 = SHA256.HashData(hash1).Bytes;

			byte[] ripemd160 = RIPEMD160.HashData(hash2).Bytes;

			Array.Copy(ripemd160, 0, bytes, 1, 20);

			

			byte[] checksumBytes = new byte[21];
			Array.Copy(bytes, checksumBytes, 21);

			byte[] checksum = SHA256.HashData(checksumBytes).Bytes;

			Array.Copy(checksum, 0, bytes, 21, 4);

			BTCAddress rv = new BTCAddress();
			rv.bytes = bytes;
			rv.privkey = privkey;
			rv.addr = Cipher.Base58.EncodeWhole(bytes);

			return rv;
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

			/*Coin.Transaction tx = Coin.Transaction.GenerateMock();

			Console.WriteLine(Coin.Printable.Prettify(tx.Readable()));

			Console.WriteLine("\n\n\n\n\n" + tx.Readable() + "\n\n\n");

			Console.WriteLine(Coin.Printable.Prettify(tx.Readable()).Length);
			Console.WriteLine(tx.Readable().Length);
			Console.WriteLine(tx.Marshal().Length);
			Console.WriteLine(Coin.Printable.Hexify(tx.Marshal()).Length);

			Console.WriteLine(tx.Size());
			Console.WriteLine("\n\n" + Coin.Printable.Hexify(tx.Marshal()));
			Console.WriteLine("\n\n" + Coin.Printable.Hexify(tx.Outputs[0].TXMarshal()));
			Console.WriteLine("\n\n" + Coin.Printable.Hexify(tx.Outputs[1].TXMarshal()));*/

			/*Key vw = new Key(), VW = new Key(), sp = new Key(), SP = new Key();
			KeyOps.GenerateKeypair(ref vw, ref VW);
			KeyOps.GenerateKeypair(ref sp, ref SP);
			Discreet.Coin.StealthAddress addr = new Discreet.Coin.StealthAddress(VW, SP);

			int i = 0;

			for (; i < 1; i++)
			{
				KeyOps.GenerateKeypair(ref vw, ref VW);
				KeyOps.GenerateKeypair(ref sp, ref SP);
				addr = new Discreet.Coin.StealthAddress(VW, SP);
				i++;
				Console.WriteLine($"Private view key:  {BitConverter.ToString(vw.bytes).Replace("-", string.Empty).ToLower()}");
				Console.WriteLine($"Private spend key:  {BitConverter.ToString(sp.bytes).Replace("-", string.Empty).ToLower()}");
				Console.WriteLine($"Public view key:  {BitConverter.ToString(VW.bytes).Replace("-", string.Empty).ToLower()}");
				Console.WriteLine($"Public spend key:  {BitConverter.ToString(SP.bytes).Replace("-", string.Empty).ToLower()}");
				Console.WriteLine($"Address: {addr.String()}");
			}*/
			
			Key specvw = new Key(Coin.Printable.Byteify("0f3fe9c20b24a11bf4d6d1acd335c6a80543f1f0380590d7323caf1390c78e88"));
			Key specsp = new Key(Coin.Printable.Byteify("0f3fe9c20b24a11bf4d6d1acd335c6a80543f1f0380590d7323caf1390c78e88"));
			Key specVW = KeyOps.ScalarmultBase(ref specvw);
			Key specSP = KeyOps.ScalarmultBase(ref specsp);

			Coin.StealthAddress specaddr = new Coin.StealthAddress(specVW, specSP);

			//Console.WriteLine($"Private view key:  {BitConverter.ToString(specvw.bytes).Replace("-", string.Empty).ToLower()}");
			//Console.WriteLine($"Private spend key:  {BitConverter.ToString(specsp.bytes).Replace("-", string.Empty).ToLower()}");
			Console.WriteLine($"Public view key:  {BitConverter.ToString(specVW.bytes).Replace("-", string.Empty).ToLower()}");
			Console.WriteLine($"Public spend key:  {BitConverter.ToString(specSP.bytes).Replace("-", string.Empty).ToLower()}");
			Console.WriteLine($"checksum:  {BitConverter.ToString(specaddr.checksum).Replace("-", string.Empty).ToLower()}");
			Console.WriteLine($"Version: {specaddr.version}");
			Console.WriteLine($"Special Address: {specaddr.String()}");

			//Console.WriteLine($"Special time: {BitConverter.ToString(Keccak.HashData(Coin.Printable.Byteify("0f3fe9c20b24a11bf4d6d1acd335c6a80543f1f0380590d7323caf1390c78e88")).Bytes).Replace("-", string.Empty).ToLower()}");

			Coin.StealthAddress addr = new Coin.StealthAddress("1BbrapYiiMkSD8F7TZBG1cPzk3Wkim2uxWXJRa197FRMFCfhMoCPxaLV2YHXFMUxyAcJjbfXJXQ3f6wMJh8WNWuvS8sePJH");
			Console.WriteLine($"Public view key:  {BitConverter.ToString(addr.view.bytes).Replace("-", string.Empty).ToLower()}");
			Console.WriteLine($"Public spend key:  {BitConverter.ToString(addr.spend.bytes).Replace("-", string.Empty).ToLower()}");

			Console.WriteLine($"checksum:  {BitConverter.ToString(addr.checksum).Replace("-", string.Empty).ToLower()}");

			var newchecksum = new byte[4];

			byte[] dstr = new byte[65];
			dstr[0] = addr.version;
			Array.Copy(addr.spend.bytes, 0, dstr, 1, 32);
			Array.Copy(addr.view.bytes, 0, dstr, 33, 32);

			newchecksum = Cipher.Base58.GetCheckSum(dstr);

			Console.WriteLine($"recomputed checksum: {BitConverter.ToString(newchecksum).Replace("-", string.Empty).ToLower()}");
			Console.WriteLine($"Version: {addr.version}");
			Console.WriteLine($"Special Address: {addr.String()}");

			/*

			for (int i = 0; i < 100; i++)
            {
				Console.WriteLine($"Addr: {GenerateBTCwallet().addr}");
            }*/

            //Console.WriteLine();

			//Console.WriteLine($"Special time: {BitConverter.ToString(Keccak.HashData(new byte[32]).Bytes).Replace("-", string.Empty).ToLower()}");
		}

	}
}
