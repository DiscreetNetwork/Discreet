using System;
using Discreet.Cipher;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Cryptography.ECDSA;
using Discreet.Cipher.Mnemonics;
using Discreet.Coin;
using Discreet.Wallets;
using System.IO;
using Discreet.RPC;
using Discreet.Common;

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

				while (eval(addr.ToString()))
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
						Console.WriteLine($"qqq???: {addr.ToString()}\nppp???: {addr.ToString().Length}");

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

			Console.WriteLine(i.ToString() + ": " + addr.ToString());

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
			Console.WriteLine($"Address: {rv.addr.ToString()}");
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

		public static void GenerateGenesis()
        {
			DB.DB db = DB.DB.GetDB();

			//db.DropAll();

			//db = DB.DB.GetDB();

			//Console.WriteLine(new Mnemonic(Randomness.Random(32)).GetMnemonic());

			/* generate test wallet */
			int numTestWallets = 256;
			int numTestUTXOs = 16;

			Stopwatch stopwatch = new Stopwatch();

			stopwatch.Start();

			Wallets.Wallet[] wallets  = new Wallets.Wallet[numTestWallets];

			for (int i = 0; i < numTestWallets; i++)
			{
				wallets[i] = new Wallets.Wallet("test" + i, "password123!", new Mnemonic(Randomness.Random(32)).GetMnemonic(), true, true, 24, 1);
				wallets[i].Decrypt("password123!");
                Console.WriteLine("Test wallet " + i + " generated.");
			}

            Console.WriteLine("Wallets generated. ");

			//Console.WriteLine(wallet.Addresses[0].PubSpendKey.ToHex());
			//Console.WriteLine(KeyOps.ScalarmultBase(ref wallet.Addresses[0].SecSpendKey).ToHex());

			Wallets.Wallet myWallet = new Wallets.Wallet("myWallet", "password123!", "shy reason torch similar people ball false shuffle wet pitch inflict hood trash silent legend diary myself field popular loan donor copy own blind");

			StealthAddress[] addresses = new StealthAddress[numTestWallets];
			int[] numOutputs = new int[numTestWallets];

			for (int i = 0; i < numTestWallets; i++)
            {
				addresses[i] = wallets[i].Addresses[0].GetAddress();
				numOutputs[i] = numTestUTXOs;
			}

			//Console.WriteLine(addresses[0].ToString());
			//Console.WriteLine(wallet.Addresses[0].Address);


			//Block genesis = new Block();

			//genesis.UnmarshalFull(Printable.Byteify("0008d98c0b21fad6c0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000499966feda83e8fd282b370385ed920ab50c8462ea6b0ae128a1a60491523c3a3e496d2916ab829985fd925166ed34260156cc592feccf046e24e7f302f8303600000002000001f9000000040000020059df0c54799de6f03730afc59ccb54a881d23a1f2a1ce9b049b821a1ff12674af6f1e05293fce69098f4b32ed334175a26a6f34065bcb612777736fb2381267b2cdfd0430daf7d8032e485269ea91a2b50dbea95467df7ddf880d15161f1b4c278d35bf850bead71c8ba08c8297f48ef267fc56314c64ac1f2084cfe05e65b1fe7106bc27e1879e361c3d5400c24d59b000000220100915a7369011743affd2fe844b0be62874864dcc26abceeed04e40f131377033d00000200a94e3d4e5c8f8107266eaf3bc5a5f86c8fce6156c38855d7961df516099f64a0cde994adf37c1182797edb9ff7f444ee8b7491cee6d3f770dbc1732b53d79e01a3642d69e2a8922f72e2ec0a08d5b9da014d6a1bfbd513ab516ef40d6f3072fb660bcbf966d33985ef79dbcde148ce235d2238aef8c1d909908adbde2eabcf258ed49c1ceeb998b861cbf2363d987f5e000000220100fec30641956c9280b46ba6a62acbe99c7bc06408ff54ed2e805843702c70a0ac"));



			List<Transaction> genesisTXsPlus = new List<Transaction>();
			genesisTXsPlus.Add(Transaction.GenerateTransactionNoSpend(myWallet.Addresses[0].GetAddress(), (ulong)6_000_000 * 1_000_000_000_0));
			Block genesis = Block.BuildRandomPlus(addresses, numOutputs, genesisTXsPlus);

            Console.WriteLine(genesis.MarshalFull().Length);

			Console.WriteLine("Genesis block generated. ");

			for (int i = 0; i < numTestWallets; i++)
            {
				wallets[i].ToFile(Path.Combine(Visor.VisorConfig.GetDefault().WalletPath, wallets[i].Label + ".dis"));
				wallets[i].Decrypt("password123!");
			}

			myWallet.ToFile(Path.Combine(Visor.VisorConfig.GetDefault().WalletPath, myWallet.Label + ".dis"));
			myWallet.Decrypt("password123!");

			var err = genesis.Verify();

			if (err != null)
            {
				throw err;
            }

			Console.WriteLine("Genesis block verified. ");

			File.WriteAllText(Path.Combine(Visor.VisorConfig.GetDefault().VisorPath, "genesis_block.json"), Printable.Prettify(genesis.ReadableFull()));

			Console.WriteLine("Genesis block printed to file. ");


			Visor.Visor visor = new(myWallet);

			visor.ProcessBlock(genesis);

			myWallet.ToFile(Path.Combine(Visor.VisorConfig.GetDefault().WalletPath, myWallet.Label + ".dis"));
			//myWallet.Decrypt("password123!");

			for (int i = 0; i < numTestWallets; i++)
            {
				wallets[i].ProcessBlock(genesis);
				wallets[i].ToFile(Path.Combine(Visor.VisorConfig.GetDefault().WalletPath, wallets[i].Label + ".dis"));
                Console.WriteLine("Test wallet " + i + " has processed genesis block.");
			}

			//receiver.ProcessBlock(genesis);

			//myWallet.ProcessBlock(genesis);
			

			Console.WriteLine("All wallets processed genesis block. ");

			stopwatch.Stop();

            Console.WriteLine(stopwatch.ElapsedMilliseconds);
		}

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
				Console.WriteLine($"Address: {addr.ToString()}");
			}*/

			/*Key specvw = new Key(Coin.Printable.Byteify("0f3fe9c20b24a11bf4d6d1acd335c6a80543f1f0380590d7323caf1390c78e88"));
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
			Console.WriteLine($"Special Address: {specaddr.ToString()}");

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
			Console.WriteLine($"Special Address: {addr.ToString()}");*/

			/*

			for (int i = 0; i < 100; i++)
            {
				Console.WriteLine($"Addr: {GenerateBTCwallet().addr}");
            }*/

			//Console.WriteLine();

			//Console.WriteLine($"Special time: {BitConverter.ToString(Keccak.HashData(new byte[32]).Bytes).Replace("-", string.Empty).ToLower()}");

			/*for (int i = 0; i < 100; i++)
			{
				Mnemonic mnemonic = new Mnemonic(128, Mnemonic.Language.English);

				Console.WriteLine(mnemonic.GetMnemonic());

				Console.WriteLine(BitConverter.ToString(mnemonic.GetEntropy()).Replace("-", string.Empty).ToLower());
				Mnemonic check = new Mnemonic(mnemonic.GetMnemonic());
				Console.WriteLine(BitConverter.ToString(check.GetEntropy()).Replace("-", string.Empty).ToLower());
			}*/

			/*DB.DB db = new DB.DB("discreet/test");

			Transaction[] txs = new Transaction[1000];

			Stopwatch stopWatch = new Stopwatch();
			stopWatch.Start();

			for (int i = 0; i < 1000; i++)
			{
				txs[i] = Coin.Transaction.GenerateMock();
			}

			stopWatch.Stop();

            Console.WriteLine(stopWatch.ElapsedMilliseconds);

			stopWatch.Reset();

			stopWatch.Start();

			for (int i = 0; i < 1000; i++)
			{
				txs[i].Marshal();
			}

			stopWatch.Stop();

			Console.WriteLine(stopWatch.ElapsedMilliseconds);

			stopWatch.Reset();

			stopWatch.Start();

			for (int i = 0; i < 1000; i++)
            {
				db.AddTXToPool(txs[i]);
            }

			stopWatch.Stop();

			Console.WriteLine(stopWatch.ElapsedMilliseconds);

			string bytedata = "01 02 02 02 6A 8E E9 29 3F 1C 6F 09 69 38 21 DD 38 2F 2F 03 68 A9 6B EA 6D 55 F1 4E 03 FA 62 DA 15 D1 B2 1C 27 CD C9 F5 42 18 CD 3C 6B D3 23 D1 66 09 76 EB 1F 38 C2 89 16 2F A2 77 24 4E FB 0B 44 82 AD 86 59 19 63 C5 59 54 F1 E5 5A 71 F4 38 51 DC 08 18 3F 8A A8 DA 4C DE D7 F0 55 DB CE DB 3D 6D D3 D3 34 DE 63 F6 2E 92 89 41 45 73 37 6A 22 DA 78 2F 30 6A 82 D9 0D 37 10 7D 64 BD 40 E4 61 53 0A 09 02 7B 28 9C 5E EA 6F B8 19 4C ED 3F 59 44 08 95 1C 4B 89 78 71 03 6D 80 0E 2D 3A 54 41 60 66 78 6A 02 78 06 53 98 91 EF 63 67 B7 F2 61 38 50 21 19 CA 2F 7F 33 7C 13 31 66 5F 14 FB 70 FF A6 2F 1E E7 51 1F 09 2D 58 D8 24 0F 66 8F 2A 8E 7B AC 22 70 C8 F2 09 4A 20 ED 58 B1 AF 5E 1D B0 11 39 69 40 A0 2D 2B CA 4E 0A 03 50 CB 0D 3A 16 E2 A9 27 E2 B9 E4 61 1F EA AA 65 67 2F 42 1A 96 B9 78 81 0F 93 CF 85 16 34 2E 6B 71 51 F9 70 D1 FD 43 3B 89 75 88 49 E4 07 56 70 2A 8F B9 33 85 D8 8C 3D 36 D7 34 50 20 CC 81 62 B7 2D 16 33 9F 0F BE 67 88 E5 B7 06 4F 88 C3 26 9D 0E 68 56 EA 46 97 4B 64 FE 6A 79 31 8E 10 75 53 35 CD 45 9D 5E AB 3C EC 73 54 64 1A DD F6 4F EC 99 60 2E E5 9E 19 4F 14 77 41 6E C7 69 9B 0B 62 62 86 59 84 E5 31 7C B0 DC AF 66 BD 6F EA 08 C9 CA 41 5C C8 E0 28 0C 83 2B 32 79 F4 58 FB 5D 45 D2 FB 21 1D 5E 9C 01 B7 29 D3 61 EA CC B5 46 88 E6 7D 0F 41 01 08 5F 70 FA FA 19 C8 9E E7 03 07 C0 B9 3D 36 44 98 6C 35 75 19 64 0E 93 CA 3C FC 71 15 3A BF 67 F1 2D E0 0D 19 27 50 44 7F 72 46 6B 09 03 04 09 CC 0D 46 57 AE 58 0B 93 94 2D 89 5F E7 74 5C C8 D4 60 07 FC E8 42 39 08 23 5F 10 ED 58 49 23 31 88 05 0A 7F 3F 5A CA 40 41 0B DA DD 5B 56 79 67 49 47 57 02 40 05 EE 4C EE 27 1B E4 8C 6D 94 8C BB 29 09 B5 C7 05 7F AF CC 55 33 1C C4 69 7A 31 B4 EB 73 0D AC 25 90 B8 23 C9 A6 F6 B5 10 90 84 E7 B2 93 E5 BE 02 17 FA 91 7A 01 82 07 8B F6 B5 C5 B6 F3 2D 71 F3 E8 C6 CF 7E 44 C2 7E AB 6F 7A AE 2F D8 D1 10 34 7F 58 87 84 21 E2 9D 6D B8 AA BE E6 6E 65 FD 20 0F 74 8B 55 77 81 9B 62 F8 82 3E B0 10 61 5A D5 63 10 A8 78 99 C2 77 83 38 41 F0 24 62 83 21 5C 5E DA 44 0F 11 C0 CD A7 9D 32 FA 6B 84 65 AC 4C 0A B2 5F 68 72 A9 3B 99 35 30 BA 22 C5 D1 E9 C3 8E E4 71 35 04 65 15 45 B0 E0 D9 8F 64 80 7A 80 66 1C B6 D8 3F 2A 4F DF 42 E3 4F F6 D1 55 39 33 E1 13 0B 31 70 A1 2D 2E F3 BC ED 00 00 00 07 B6 C6 EB FA 0D BD F8 B1 3C A0 22 CC 5D 29 BF 91 AE F7 77 10 8B 3F 53 B7 B1 37 9E 67 31 8F 78 D1 CC C8 2D D2 C5 20 59 8F 4B 27 C2 FE CF A3 45 D9 FB D9 C5 AD 2A 3E 26 18 51 1F 03 FB 23 FD DB 98 1F B2 07 C7 97 9A 2E 42 03 A4 82 ED 20 09 41 29 59 BA 54 82 D3 6D 92 D1 B2 01 CA DA C7 62 EC 0D E9 14 BA 80 99 DC 9A A0 81 80 EC 5D 7C 29 62 9D 0E 0D E6 67 10 E0 DD E7 42 D7 33 45 A9 94 8F 8D F5 5D EC 86 AB 46 8C B0 AF C4 1C 98 EE 30 8E 63 2B 82 32 B9 75 92 E8 15 AE F4 1C DD 8C CC 87 0C D4 85 0E A4 5C 49 E5 00 08 E1 6C B6 6A 09 7A 73 01 BD F5 79 43 C0 80 EA 27 37 F0 C1 8A 66 B8 0C 48 6B A7 24 EF FA AA 4A 13 F7 01 9A 56 18 3B 2B 93 BA 2E F1 9B 36 33 BA 48 87 F7 39 67 EB 9E 6E 6A DD 1F 44 12 22 88 1A BB 48 EB 1D 15 D2 33 02 15 DE AC 93 23 19 A9 A5 0B B6 A6 7B 90 28 70 E0 D3 83 4F 67 DB E2 73 B3 28 F6 1E 4C 57 ED 0E AA B4 76 9E 9D 1C 3F 2B 1D 8C 22 90 5B 7F 40 C8 F6 32 42 BB 6D 97 B9 89 04 37 4F 12 17 C9 75 5A 2D B7 7C 01 EC 6D A5 BD 38 9F C6 C6 FE F4 DC 5D 22 43 9B 5E FB F9 1E B7 43 D0 23 3C 91 1F CC 69 D8 09 B0 94 E4 60 71 EF 91 81 D0 C9 EB 75 25 27 E5 41 1F 80 E3 A5 2C A1 18 87 5A 71 A9 35 D6 0C 92 93 2C A1 12 D0 01 87 41 F3 46 27 1C 5B D2 C0 4D B3 FA 2F 3B 67 98 28 9F CB BF 25 A9 9E C2 12 FA AD 41 13 6A 58 E7 48 B9 E2 14 D4 7E EA BC 17 10 F8 6C D4 34 CA C8 BE DD 9D E7 64 0D 6C CF E1 9A 41 21 1F 32 05 98 ED 76 4A 8F E0 B1 A4 DB F3 1B 43 28 9B 23 EE 95 48 99 F2 22 72 0D 69 10 5B D6 0C 13 3B 20 0E 82 2D 64 DA A1 11 56 81 E0 E9 03 C1 EE 0E 5E 49 A0 68 22 5C 1D 5A 36 3D 75 C0 11 47 06 19 D4 A5 0A 1A 88 DE C5 E4 9A 5D EF 47 7F 95 3A 6E C6 7B CA 31 1B E0 1B FE 2B 79 7C 14 FE 0B 5C 32 47 EA 71 35 50 A7 BF C8 01 46 D8 DA F9 B7 8D 29 33 BB 41 1D 37 27 83 03 F7 35 93 F5 FA A3 6B A7 A3 32 FE FB 99 7F E3 F7 3A 42 6F 2E A3 E7 AD 34 01 87 86 63 B3 77 8E F7 BB D3 C0 EE 93 EF F1 73 17 90 6D 9E F7 A8 C8 57 93 19 1D 2A 70 74 A1 35 DE 24 C1 92 81 DD E6 AF 54 3F 80 B7 C1 33 8C 23 2C DC 22 4A 14 33 47 C2 0F F5 49 8F 07 09 E5 ED 3D F0 60 D6 7A 8F E0 F5 45 8C 5A 85 D2 B7 11 08 9E 19 24 B7 40 F8 87 46 F7 94 8D 58 0E 14 47 86 23 C7 C9 99 F2 14 B8 58 D3 21 E1 8C F3 01 D0 E1 F9 88 74 AA 6D F0 8C B0 4B 78 20 26 09 5D BA AF 6A 48 5E 77 5A DC 80 96 D6 8B 07 68 4B B9 7F 36 02 A8 30 5C 7C 6A 9C 14 56 2E 0E 37 03 99 B6 E7 CB 51 52 AA 5A 84 F4 50 49 39 03 20 E3 E6 2E C9 BE 4B CF F7 D3 7D C9 29 74 17 AB AD 1C A2 DD 7C AF 98 B4 B6 CD E3 65 A9 78 6D 48 E1 3F A7 A0 14 BE 32 DE 8C 7C 88 3E C1 6C B5 AE A7 65 91 00 3A 4D E2 99 60 48 33 8F 8F 92 70 E2 3B EE DE 46 FA DE B0 AA A7 5D 92 9A 4B A3 E6 BD D2 AD A2 82 21 3A F3 93 8B A4 B4 32 34 4A 79 83 80 DE 71 F6 23 7D A3 A0 AC 21 52 3D 4E 45 BD F5 EC 21 3B 9D 98 FF E1 11 35 69 70 7E B8 2B E9 28 AE BF 2D 30 64 42 CA 56 EE 49 8D F0 A0 5C 23 EF A6 4A B9 77 86 76 BD 8C E8 55 75 35 66 F8 9A 51 D5 F9 00 D7 BF 38 6C 99 06 CF A7 85 59 AA 88 02 19 C8 3D 97 37 A6 69 97 77 B1 AA DB F5 A4 67 B9 A1 53 E3 8C 8E 22 FA A9 50 CD 1A 22 34 09 87 FB 05 62 66 E2 BF 30 2D 47 EB 70 E1 F1 2A 7A FC E8 A5 7D 43 F5 EA 8B 41 FD 55 91 FA BB 5B CF 3A CA 02 94 6C D8 63 18 BD AD 56 4A BE 4F EF C7 73 E8 97 87 73 06 B4 7B 9A FA C9 F1 47 47 07 50 E9 55 A4 ED 28 29 C9 B0 43 B3 F9 68 DF 1E 6B 84 82 BB 6A D1 75 FD 05 C2 E7 F4 4A AB E1 8B E7 CE D5 09 93 5D F4 CA F7 77 71 72 C4 74 DF 68 36 CF 53 6C 39 06 C7 61 05 9C C3 F5 08 A6 CD AA 72 92 38 1A 79 E8 10 77 06 8A D5 68 A3 DD 06 73 18 DD 9A 1A C1 FC BB 1C DB 28 C9 A6 26 85 FF D6 7B A8 AA 14 D8 98 65 CD 6B 64 6B 26 95 D3 4E B5 86 65 71 21 90 23 98 28 89 65 51 09 12 22 6C 65 91 45 A9 12 79 54 94 F6 DA 62 6F AE 99 FB 41 DA FC 89 12 7D CD C8 E0 CD 50 96 69 BD BD 24 65 70 E9 6D 46 6E F3 D8 F2 B9 02 B4 9E C0 77 23 07 1F 09 FD BF 15 3E FB 05 EA DF 1C 4E 01 BF 4D 83 12 24 38 A8 F2 70 E1 18 FA 8C 87 4D 65 5E 96 84 BD D7 01 60 DF 3F 7C 39 30 59 8E C3 A9 67 95 7B 77 11 D9 3A B8 54 56 A3 3D 7E C1 55 79 63 C2 E1 0A 72 C1 1E 32 43 D2 8F 9D AA 65 24 38 2C 65 47 E9 EC AF 54 B4 56 CD 2B 02 E5 30 B6 BA D0 B2 48 09 8B 08 28 F6 50 FE 81 1D 6B 72 95 EA C6 93 96 EB C6 9D 05 39 C4 36 06 74 CE 36 64 D1 7B CE 4E B5 86 65 71 21 90 23 98 28 89 65 51 09 12 22 6C 65 91 45 A9 12 79 54 94 F6 DA 62 6F AE 99 FB 41 DA FC 89 12 7D CD C8 E0 CD 50 96 69 BD BD 24 65 70 E9 6D 46 6E F3 D8 F2 B9 02 B4 9E C0 77 23 07 1F 09 FD BF 15 3E FB 05 EA DF 1C 4E 01 BF 4D 83 12 24 38 A8 F2 70 E1 18 FA 8C 87 4D 65 5E 96 84 BD D7 01 60 DF 3F 7C 39 30 59 8E C3 A9 67 95 7B 77 11 D9 3A B8 54 56 A3 3D 7E C1 55 79 63 C2 E1 0A 72 C1 1E 32 43 D2 8F 9D AA 65 24 38 2C 65 47 E9 EC AF 54 B4 56 CD 2B 02 E5 30 B6 BA D0 B2 48 09 8B 08 28 F6 50 FE 81 1D 6B 72 95 EA C6 93 96 EB C6 9D 05 39 C4 36 06 74 CE 36 64 D1 7B CE 35 64 0F 8D F0 CE 1A F2 95 AF 73 18 2E C4 BB AB B4 4F C4 19 3A BA A9 A9 46 B1 2B 00 14 5A 1F 0F 50 7C D4 78 88 68 DA 75 57 5E E5 3C 4B 7D 08 C1 70 AF 1D 49 CF B6 44 75 F6 68 74 5A E7 7C E9 04 89 A2 A3 F6 E8 66 AE 5E 0B 3E AB 60 08 A6 03 69 6D F2 93 F0 49 19 FF C1 83 60 86 A4 BB 4F C9 0D 4F FC 43 DB 94 1E 31 FB A1 F7 F2 88 03 79 2E A8 39 CD 9C E7 30 EB E0 C9 A4 1E DF B9 46 CA 73 C0 2E F2 F2 6B 45 B9 39 7A 74 89 4D 24 75 B2 B3 1F BE 4B D4 C4 75 F7 C5 F0 A9 54 1B 2E 13 A9 4F 97 50 43 F0 DF 46 F7 FA 13 7C CB C6 A8 9E 9F 78 02 D8 11 DB A5 1D 62 92 6D 12 A8 0D 01 91 F0 49 84 C1 FD CF 2D 9E 81 55 72 2A 48 63 4C D2 15 C8 09 BB 24 23 B6 12 9C B9 DD A6 84 1B BE 70 BF 4A DA 65 4E 80 DA 7A CB FC FE EE A8 5E CD 96 C8 78 32 F4 C9 0B 2E 91 05 E1 8E E0 04 41 CF D0 CB BA 00 77 FF 7E 4E 85 66 B0 05 D7 11 C5 E8 1E 81 17 37 A4 67 0A 48 7E 41 C9 06 0C DD 47 9B 91 24 AE B5 51 98 28 6C 76 06 29 29 73 59 F4 EF 64 AC C4 AA 8B 6A CB AB D7 AD DD 48 88 C4 EA 7E 4F 07 5E 55 D4 CC 90 16 0B 1E F1 CD 30 EB B8 B3 71 5C 14 13 73 27 70 A6 85 84 9E 92 4B 53 FE DF CD DB 77 91 D8 42 8E 26 C0 47 FC 47 56 5D BC 13 10 20 EC AA E5 89 AE 3C D6 6F 69 9C 9F 54 D9 05 73 80 9E A4 06 36 CF AB 38 05 1D 23 50 49 FA BD 4F 26 7F 6E 8C 58 DB 8D 99 1E 29 4A F1 99 D7 72 37 28 1E A6 05 35 FD 31 65 C9 98 57 E8 8E 7E 7F 74 F9 04 64 6B FD 66 E2 9C 48 90 CC B6 8D D6 D5 25 CD FC FA E9 DA 5A 6C 7F 04 40 38 FA 6C 30 3C 82 9F 13 58 13 BC D7 BC 87 FD 97 E5 F6 00 03 AB 8E B2 F2 9F 89 B4 C5 7F 57 BB 69 13 93 2F 48 19 B7 01 B5 AD 4D DC 85 7E 10 DC F9 C9 F7 B4 EB 77 E1 61 CD 88 88 2D 08 4D D0 BD A7 A1 CB 44 23 29 19 89 67 6B E3 A6 27 0A F5 45 04 FD 02 4A 33 B0 AD AF E4 6A 2C 00 D8 00 B1 73 48 7C 0F 23 D5 35 B6 D0 EA DE 5F E4 F2 56 8C B6 96 C1 67 C6 84 21 0C 17 FA 57 99 24 85 66 41 8D 40 6D F8 30 46 B1 51 4E 37 40 12 D2 96 9D 51 AA 59 28 E7 2D 5E 15 84 20 7A CF 2A 6A 78 58 D0 27 F3 AE 63 9D 3F 03 A3 D5 B4 D0 CC AC AB 75 49 E5 E6 D2 A7 D7 50 68 10 32 C6 9B 8F 67 25 A1 45 37 36 0B 76 68 59 55 7F 00 D0 EB 64 2C FB 03 3E 94 E7 2C A6 F6 21 1F CA A5 1D D7 89 B4 C5 7F 57 BB 69 13 93 2F 48 19 B7 01 B5 AD 4D DC 85 7E 10 DC F9 C9 F7 B4 EB 77 E1 61 CD 88 88 2D 08 4D D0 BD A7 A1 CB 44 23 29 19 89 67 6B E3 A6 27 0A F5 45 04 FD 02 4A 33 B0 AD AF E4 6A 2C 00 D8 00 B1 73 48 7C 0F 23 D5 35 B6 D0 EA DE 5F E4 F2 56 8C B6 96 C1 67 C6 84 21 0C 17 FA 57 99 24 85 66 41 8D 40 6D F8 30 46 B1 51 4E 37 40 12 D2 96 9D 51 AA 59 28 E7 2D 5E 15 84 20 7A CF 2A 6A 78 58 D0 27 F3 AE 63 9D 3F 03 A3 D5 B4 D0 CC AC AB 75 49 E5 E6 D2 A7 D7 50 68 10 32 C6 9B 8F 67 25 A1 45 37 36 0B 76 68 59 55 7F 00 D0 EB 64 2C FB 03 3E 94 E7 2C A6 F6 21 1F CA A5 1D D7 B0 52 F2 A6 B6 3F 53 84 C7 01 C8 97 92 72 CF BB 1B 74 F1 DD 47 27 82 6B 01 BE DF 95 19 D1 63 0D E2 FA 55 B1 C0 00 F8 BB 8B 7C E9 7C A1 09 2A D6 86 BC B1 57 99 70 9E 2D B5 98 56 99 5A FC 09 08 C6 C6 28 FC 2A CE 44 CE 2C 4C F6 7F 49 77 4E 61 B0 3B C0 2E 5D 8F 23 E8 1B 50 B7 BA 38 56 23 0C E2 BD DF 19 BE 46 55 8F 74 ED DF 30 9C 70 C0 F2 17 89 EF 60 60 8E 1C 08 14 90 FC E0 B8 51 0B 71 FD 2F A8 5A 36 E5 45 66 DC A5 B3 F8 F4 EF 38 3E AB C4 72 24 EB 4D BC 1F 4F 84 C4 DC 3A 9F D6 E8 00 00 00 22 01 22 75 50 ED 5E 87 BA 83 0D DB 71 BA 11 06 E7 1F AE E1 A3 F6 C7 28 FA AB E9 ED 02 C8 01 D4 0E 3A 7E".ToLower().Replace(" ", String.Empty);
			Coin.Transaction tx = new Coin.Transaction();
			tx.Unmarshal(Coin.Printable.Byteify(bytedata), 0);
            Console.WriteLine(Coin.Printable.Prettify(tx.Readable()));

			try
			{
				db.AddTXToPool(tx);
                Console.WriteLine("didn't fail :(");
			}
			catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }*/

			// PBKDF2 for "password123!" inputting any other 
			byte[] magic = new byte[32] { 0x17, 0x33, 0x50, 0x8c, 0xbe, 0x39, 0xf1, 0xe0, 0xac, 0x81, 0x84, 0xf4, 0x64, 0x18, 0x6f, 0x46, 0x61, 0x75, 0x1d, 0x94, 0x83, 0x64, 0xa6, 0x76, 0xc6, 0x69, 0xa7, 0x89, 0x77, 0x38, 0x47, 0x79 };


            // CipherObject initSettings = new CipherObject {  Key = magic, IV = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00  } };

            /*Wallet wallet = new Wallet("wrap", "password123!");
			Console.WriteLine("Encryption Key: " + Printable.Hexify(magic));

			Console.WriteLine("Entropy: " + Printable.Hexify(wallet.Entropy));

			Console.WriteLine(Printable.Prettify(wallet.ToJSON()));

			CipherObject entropyKeyObj = AESCBC.GenerateCipherObject("password123!");

			(CipherObject cipherObj, byte[] encryptedBytes) = CipherObject.GetFromPrependedArray(entropyKeyObj.Key, wallet.EncryptedEntropy);
			byte[] decrypted = AESCBC.Decrypt(encryptedBytes, cipherObj);
			Console.WriteLine("encrypted hex: " + Printable.Hexify(wallet.EncryptedEntropy));

			Console.WriteLine("decrypted: " + Printable.Hexify(decrypted));

			CipherObject encryptionParams = AESCBC.GenerateCipherObject("");


			Console.WriteLine("Encryption Key: " + BitConverter.ToString(magic));

            byte[] encrypted = AESCBC.Encrypt(Encoding.UTF8.GetBytes("This is a really long test string that will use multiple block ciphers for encryption to show that our AES encryption scheme supports ciphers of any lengths and can output digests of any length."), encryptionParams);
            Console.WriteLine("Encrypted bytes: " + BitConverter.ToString(encrypted));
            Console.WriteLine("Encrypted bytes length: " + encrypted.Length);


            byte[] decwypted = AESCBC.Decrypt(encrypted, encryptionParams);

            Console.WriteLine("Decrypted: " + Encoding.ASCII.GetString(decwypted));
			Console.WriteLine(Printable.Prettify(encryptionParams.ToString()));

			Wallet checkWalletJSON = Wallet.FromJSON(wallet.ToJSON());	

            Console.WriteLine(Printable.Prettify(checkWalletJSON.ToJSON()));

			wallet.ToFile("test.wlt");

			Wallet okAgain = Wallet.FromFile("test.wlt");

            Console.WriteLine(Printable.Prettify(okAgain.ToJSON()));

			Wallet testWallet = new Wallet("test", "test123!", 12, true);

            //Console.WriteLine(Printable.Prettify(testWallet.JSON()));

            Console.WriteLine(testWallet.GetMnemonic());

			string homePath = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX) ? Environment.GetEnvironmentVariable("HOME") : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
			Console.WriteLine(Path.Combine(homePath, ".discreet"));*/


            //RPCServer process = new RPCServer(8350);
            //await process.Start();
            //await Task.Delay(-1);


            //db.AddBlock(genesis);

            //Console.WriteLine(new Mnemonic(Randomness.Random(32)).GetMnemonic());

            //Block.BuildRandom()

            //GenerateGenesis();

            /*Stopwatch stopwatch = new Stopwatch();

			

			Wallet wallet = Wallet.FromFile(Path.Combine(Visor.VisorConfig.GetDefault().WalletPath, "myWallet.dis"));
			wallet.Decrypt("password123!");

			Wallet receiver = Wallet.FromFile(Path.Combine(Visor.VisorConfig.GetDefault().WalletPath, "test0.dis"));

			List<Transaction> txs = new List<Transaction>();

            Console.WriteLine("starting tx creation profile");

			stopwatch.Start();

			for (int i = 0; i < 100; i++)
            {
				Transaction testtx = wallet.CreateTransaction(0, new StealthAddress[] { receiver.Addresses[0].GetAddress() }, new ulong[] { 1_000_000_000_0 }, 2);
				txs.Add(testtx);

			}

			stopwatch.Stop();

            Console.WriteLine("100 transactions created in " + stopwatch.ElapsedMilliseconds + "ms");

			stopwatch.Reset();


			//Console.WriteLine(Printable.Prettify(testtx.Readable()));

			//Console.WriteLine(wallet.Addresses[0].Balance);
			//Console.WriteLine(wallet.Addresses[0].UTXOs.Count);

			stopwatch.Start();

			for (int i = 0; i < 100; i++)
            {
				var err = txs[i].Verify();

				if (err != null)
				{
					throw err;
				}
			}

			stopwatch.Stop();

            Console.WriteLine("100 transactions verified in " + stopwatch.ElapsedMilliseconds + "ms");*/

             Wallet wallet = Wallet.FromFile(Path.Combine(Visor.VisorConfig.GetDefault().WalletPath, "test0.dis"));
			wallet.Decrypt("password123!");

			
			Wallet receiver1 = Wallet.FromFile(Path.Combine(Visor.VisorConfig.GetDefault().WalletPath, "myWallet.dis"));
			receiver1.Decrypt("password123!");

			Console.WriteLine(receiver1.Addresses[0].Balance);


			Wallet receiver2 = Wallet.FromFile(Path.Combine(Visor.VisorConfig.GetDefault().WalletPath, "test1.dis"));

			Wallet receiver3 = Wallet.FromFile(Path.Combine(Visor.VisorConfig.GetDefault().WalletPath, "test2.dis"));

			Wallet receiver4 = Wallet.FromFile(Path.Combine(Visor.VisorConfig.GetDefault().WalletPath, "test3.dis"));

			//Transaction testtx = wallet.CreateTransaction(0, new StealthAddress[] { receiver1.Addresses[0].GetAddress(), receiver2.Addresses[0].GetAddress(), receiver3.Addresses[0].GetAddress(), receiver4.Addresses[0].GetAddress() }, new ulong[] { 5_000_000_000_0, 100_000_000_000_0, 250_000_000_000_0, 200_000_000_000_0 }, 2);
			

			Transaction testtx = wallet.CreateTransaction(0, new StealthAddress[] { receiver1.Addresses[0].GetAddress() }, new ulong[] { 69_000_000_000_0 }, 2);

			Console.WriteLine(Printable.Prettify(Readable.Transaction.ToReadable(testtx)));
			Console.WriteLine(testtx.Marshal().Length);

			var err = testtx.Verify();

			if (err != null)
			{
				throw err;
			}

			HashSet<SHA256> _in = new HashSet<SHA256>();

			_in.Add(new SHA256(new byte[32], false));

			for (int i = 0; i < 5; i++)
			{
				_in.Add(new SHA256(Randomness.Random(32), false));
			}

            if (_in.Add(new SHA256(new byte[32], false))) Console.WriteLine("SHA256 Equals not good");

			Key a = new Key();
			Key A = new Key();

			KeyOps.GenerateKeypair(ref a, ref A);

			Key m = new Key(new byte[32]);
			HashOps.HashToScalar(ref m, Encoding.ASCII.GetBytes("test"), 4);

			Signature s = new Signature(a, A, m);

            if (!s.Verify(m)) Console.WriteLine("signature not valid!");



			//Console.WriteLine(Printable.Prettify(File.ReadAllText(Path.Combine(Visor.VisorConfig.GetDefault().VisorPath, "mon.txt"))));


			/* testing DKSAP */

			/*Key r = new(new byte[32]);
			Key R = new(new byte[32]);

			KeyOps.GenerateKeypair(ref r, ref R);

			Key bv = new(new byte[32]);
			Key BV = new(new byte[32]);

			KeyOps.GenerateKeypair(ref bv, ref BV);

			Key bs  = new(new byte[32]);
			Key BS = new(new byte[32]);

			KeyOps.GenerateKeypair(ref bs, ref BS);

			StealthAddress Bob = new StealthAddress(BV, BS);

			Key T = KeyOps.DKSAP(ref r, Bob.view, Bob.spend, 1);

			Key t = KeyOps.DKSAPRecover(ref R, ref bv, ref bs, 1);

            Console.WriteLine(T.ToHex());
			Console.WriteLine(KeyOps.ScalarmultBase(ref t).ToHex());

            Console.WriteLine("\n\n\n\n\n\n\n");

			Transaction testTX = Transaction.GenerateRandomNoSpend(new StealthAddress(wallet.Addresses[0].PubViewKey, wallet.Addresses[0].PubSpendKey), 1);
			wallet.ProcessTransaction(testTX);*/


			/**
			 * test the json serializer
			 */
			//Console.WriteLine(Printable.Prettify(JsonSerializer.Serialize(Transaction.GenerateMock())));

			//Console.WriteLine(Printable.Prettify(JsonSerializer.Serialize(TXInput.GenerateMock())));

			//Console.WriteLine(Printable.Prettify(JsonSerializer.Serialize(TXOutput.GenerateMock())));

			//Console.WriteLine(Printable.Prettify(JsonSerializer.Serialize(new Key(new byte[32]))));

			//Key specvw = KeyOps.GenerateSeckey();

			//Coin.StealthAddress specaddr = new Coin.StealthAddress(KeyOps.GeneratePubkey(), KeyOps.GeneratePubkey());

			//Console.WriteLine(specaddr.ToString());

			//Console.WriteLine(Cipher.Base58.Encode(Randomness.Random(69)));

			/*Transaction tx1 = Transaction.GenerateMock();
			string tx1s = Printable.Hexify(tx1.Marshal());

			byte[] txb = tx1.Marshal();

			Transaction tx2 = new Transaction();
			tx2.Unmarshal(txb);
			string tx2s = Printable.Hexify(tx2.Marshal());

            Console.WriteLine(tx1s);
            Console.WriteLine("\n\n\n\n\n\n\n\n\n" + tx2s);
			if (tx1s != tx2s)
            {
                Console.WriteLine("uh oh spaghettio");
            }*/
		}

	}
}
