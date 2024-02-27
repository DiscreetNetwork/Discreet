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

namespace Discreet
{
	class Entry
	{
		public static Daemon.Daemon daemon;

		public static async Task Main(string[] args)
        {
            Console.Title = $"Discreet Daemon (v{Assembly.GetExecutingAssembly().GetName().Version.ToString(3)})";
            // daemon initialization and start
            daemon = Daemon.Daemon.Init();
            bool success = await daemon.Start();

            while (!success)
            {
                success = await daemon.Restart();
            }

            await daemon.MainLoop();

            // generate new keys
            //var keys = Enumerable.Range(0, 16).Select(x => KeyOps.GenerateKeypair()).ToList();
            //var authKeys = keys.Select(x => x.Pk).ToList();
            //var signKeys = keys.Select(x => x.Sk).ToList();
            //var tab = new string(' ', 12);

            //await Console.Out.WriteLineAsync("Auth Keys:");
            //for (int i = 0; i < authKeys.Count; i++)
            //{
            //    await Console.Out.WriteLineAsync($"{tab}\"{authKeys[i].ToHex()}\"{(i < 15 ? "," : "")}");
            //}

            //await Console.Out.WriteLineAsync();
            //await Console.Out.WriteLineAsync("Signing Keys:");
            //for (int i = 0; i < signKeys.Count; i++)
            //{
            //    await Console.Out.WriteLineAsync($"{tab}\"{signKeys[i].ToHex()}\"{(i < 15 ? "," : "")}");
            //}

            //var signingKeys = new string[] {"ac028a08436618299ce2809971e23df6c98bb9ed4d9fc9b92e49e4aae621db06",
            //"daa0ce04a68a61ff2468e81e638fb46be8e16a86cafbc255dc490e34f10b1406",
            //"64a43d9d8edb3b8ac690fee8503ec652285172ff5db1e369913ce94a9922b603",
            //"22ca5bbfd54a718271d180346b5bff302cf76bdbc3c67466b04ad26e3957d20f",
            //"74bc73849b0a9b30d2276ee72f8c065e547f099497c8d921d738f35f97c0b40a",
            //"f42f4daa5e71859c8f816dc75705d693dd86cdd45e1527521041b2dd49fe8206",
            //"8aea8413e63899153d55181fd8dd314be3b848c93ccc4853e9e1c2fab64e1504",
            //"fcdc64408c53d1e62af5951499140a15b508b8cc9b7deca40ff3e74ab885a400",
            //"d86f1dc209c57cf24cdc33eb8397260fc602045c47a9d66ff104bccb8dee970c",
            //"e80bb672b01c720c552c1f7ba14073126518809c43425bdae04d1e1c5dcbc80b",
            //"b8e7771ef03127bc4a75841962ee80cfc1b56731c51e34279a1db9925d5c5302",
            //"6152f53c88c76e6c83e9e2c2d2be3f75c6cdb7f3a460aff7698e7eb2f3239d0f",
            //"1c5dadfac0a61451273991c9cb401ddb8b0bc752d223f3c74c31269c4c136f06",
            //"e9880d3953c205933fffd41b92e682ac927b4e02e7fdd2b42464b0d7dd6e6507",
            //"e1cdd99ed19b9e48e9f2e48116d9a80a0d80672ccae62dfdb26f02f9c82c960d",
            //"ac4cbccb8189d9765639a072e5e5b6a0306bac03a3ba28764c8a439f37deb907"};
            //var authorKeys = new string[] {"478c9e6acd2550d9ddb5dc724adebba2e813529c09befa6b4305ce38079f129a",
            //"14eaff489480db0153a828d51c4e92b0455db3d7852f23b4df242f846b773596",
            //"d5511e4365d1f82cceed2c824dac98984dbd6cb1d39dce9f013db7cad35608fa",
            //"7d402da6bc78912aacd311c16cb842f80d52f071d10c07291eaeddb9c60a53b7",
            //"3a338b5ad2dd4eed00775ebafd9e595e161f1cdc199c92a905b7ca724599a4b9",
            //"e52b19aac5ecc4b415004f737daaad66db2ff6b95dc6b61836b44dfa85dc1784",
            //"054174ecb73c31d94a55bcb590f373ca957773e984d804fc08a647e788c5cc75",
            //"76aa7818631f083c1dbf0de55ca93bad0901cc273b9528edd17374973042669c",
            //"d76ddff86c8d9419c12c115b4c9dbad4947dc51c069fd52436c507a98dc2fb4a",
            //"8f39f5756b58d6fd6f8a8a8f8317fe304012c16cc99a171ff4dfa65e00b85eb2",
            //"743d533ad3982520f18ab9eca75e859443f7b5e42ec8d95e4a615982830294c9",
            //"068bbe03cc0e618a0fbc866f89d50414b36de496f4439f61abae9e35528ed9c7",
            //"da5c58806a747e281325964b8c639fb1010193ac3a3bac90b8182244d3e375a4",
            //"f80cc33c39afff256986f28fe1bc151bc9ac03e6ad2866547cc641051e45c191",
            //"a08e5ca857c4f7fb2f0240415f87a3c7e6160380481ece4c730e02768be4dafe",
            //"556d49846cd3cbfd1f1a854a80514bc5e029d613146e3de329af994da6e4adb1"};

            //var parsedAuths = authorKeys.Select(x => Key.FromHex(x)).ToArray();
            //var parsedSigns = signingKeys.Select(x => Key.FromHex(x)).ToArray();

            //var fuckedDb = new DB.ChainDB("C:\\\\users\\brand\\discreet\\fucked_data\\chain");
            //var myDb = new DB.ChainDB("C:\\\\users\\brand\\discreet\\data\\chain");

            //var nout = myDb.GetOutputIndex();

            //for (uint i = 1; i < nout; i++)
            //{
            //    if (!myDb.GetOutput(i).Equals(fuckedDb.GetOutput(i)))
            //    {

            //    }
            //}

            //await Console.Out.WriteLineAsync("All good");
        }
    }
}
