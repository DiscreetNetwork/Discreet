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
        private static VisorConfig _visor_config;

        private static object visor_config_lock = new object();

        public static VisorConfig GetConfig()
        {
            lock (visor_config_lock)
            {
                if (_visor_config == null) _visor_config = new VisorConfig();

                return _visor_config;
            }
        }

        public static void SetConfig(VisorConfig config)
        {
            lock (visor_config_lock)
            {
                _visor_config = config;
            }
        }

        public string VisorPath { get; set; }
        public long DBSize { get; set; }

        public string DBPath { get; set; }
        public string LogPath { get; set; }
        public string WalletPath { get; set; }

        public string ConfigPath { get; set; }


        public int Port { get; set; }
        public int RPCPort { get; set; }
        public IPEndPoint Endpoint { get; set; }
        public IPAddress BootstrapNode { get; set; }

        public byte NetworkID { get; set; }
        public uint NetworkVersion { get; set; }
        public Cipher.Key ID { get; set; }

        public VisorConfig()
        {
            VisorPath = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX) ? Environment.GetEnvironmentVariable("HOME") : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

            VisorPath = Path.Combine($"{VisorPath}\\", ".discreet");

            if (!Directory.Exists(VisorPath)) Directory.CreateDirectory(VisorPath);

            DBPath = Path.Combine(VisorPath, "data");
            LogPath = Path.Combine(VisorPath, "logs");
            WalletPath = Path.Combine(VisorPath, "wallets");

            if (!Directory.Exists(DBPath)) Directory.CreateDirectory(DBPath);
            if (!Directory.Exists(LogPath)) Directory.CreateDirectory(LogPath);
            if (!Directory.Exists(WalletPath)) Directory.CreateDirectory(WalletPath);

            ConfigPath = Path.Combine(VisorPath, "config.json");

            DBSize = 4294967296/4; // 1gb

            Port = 6585;

            Endpoint = new IPEndPoint(IPAddress.Any, Port);


            NetworkID = 1;
            NetworkVersion = 1;

            RPCPort = 8350;

            ID = new Network.Peerbloom.NodeId().Value;
        }

        public static VisorConfig GetDefault()
        {
            return GetConfig();
        }
    }
}
