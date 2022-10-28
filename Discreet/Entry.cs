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
using Discreet.RPC.Common;
using System.Net;

namespace Discreet
{
	class Entry
	{
		public static Daemon.Daemon daemon;

		public static async Task Main(string[] args)
        {
            // daemon initialization and start
            daemon = Daemon.Daemon.Init();
            bool success = await daemon.Start();

            while (!success)
            {
                success = await daemon.Restart();
            }

            await daemon.MainLoop();
        }
	}
}
