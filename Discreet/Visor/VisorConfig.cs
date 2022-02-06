using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Visor
{
    public class VisorConfig
    {
        public string VisorPath { get; private set; }
        public long DBSize { get; private set; }

        public string DBPath { get; private set; }
        public string LogPath { get; private set; }
        public string WalletPath { get; private set; }

        public string ConfigPath { get; private set; }

        public IPEndPoint Endpoint { get; private set; }
        public IPAddress BootstrapNode { get; private set; }

        public byte NetworkID { get; private set; }

        public VisorConfig()
        {
            VisorPath = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX) ? Environment.GetEnvironmentVariable("HOME") : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            VisorPath = Path.Combine(VisorPath, ".discreet");

            DBPath = Path.Combine(VisorPath, "data");
            LogPath = Path.Combine(VisorPath, "logs");
            WalletPath = Path.Combine(VisorPath, "wallets");

            ConfigPath = Path.Combine(VisorPath, "config.json");

            DBSize = 4294967296/4; // 1gb

            Endpoint = new IPEndPoint(IPAddress.Any, 6585);

            NetworkID = 1;
        }

        public static VisorConfig GetDefault()
        {
            return new VisorConfig();
        }
    }
}
