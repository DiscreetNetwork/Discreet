﻿using System;
using Discreet.Cipher;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Discreet.Cipher.Mnemonics;
using Discreet.Coin;
using Discreet.WalletsLegacy;
using System.IO;
using Discreet.RPC;
using Discreet.Common;
using Discreet.RPC.Common;
using System.Net;
using System.Reflection;
using Discreet.DB;
using Discreet.Wallets.Utilities;
using Discreet.Wallets.Models;
using Discreet.Common.Serialize;
using Discreet.Network;
using Discreet.Network.Core.Packets.Peerbloom;
using Discreet.Coin.Models;
using System.Text.Json;
using Discreet.Coin.Converters;
using Discreet.Common.Converters;
using System.Text.Json.Serialization;
using Discreet.Wallets;
using System.Collections.Concurrent;
using Discreet.Coin.Script;
using Discreet.Scripting;
using Discreet.Sandbox;
using Discreet.Daemon.BlockAuth;

namespace Discreet
{
	class Entry
	{
		public static Daemon.Daemon daemon;

		public static async Task Main(string[] args)
        {
            //var cod = DVMAParserV1.ParseFile("C:\\\\users\\brand\\OneDrive\\Desktop\\wip_dapp.txt");
            //await Console.Out.WriteLineAsync(Printable.Hexify(cod));

            //return;

            Console.Title = $"Discreet Daemon (v{Assembly.GetExecutingAssembly().GetName().Version.ToString(3)})";
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
