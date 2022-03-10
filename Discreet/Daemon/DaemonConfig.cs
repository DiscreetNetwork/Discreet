using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Daemon
{
    public class DaemonConfig
    {
        private static DaemonConfig _daemon_config;

        private static object daemon_config_lock = new object();

        public static DaemonConfig GetConfig()
        {
            lock (daemon_config_lock)
            {
                if (_daemon_config == null) _daemon_config = new DaemonConfig();

                return _daemon_config;
            }
        }

        public static void SetConfig(DaemonConfig config)
        {
            lock (daemon_config_lock)
            {
                _daemon_config = config;
            }
        }

        public string DaemonPath { get; set; }
        public long DBSize { get; set; }

        public string DBPath { get; set; }
        public string LogPath { get; set; }
        public string WalletPath { get; set; }

        public string ConfigPath { get; set; }


        public int Port { get; set; }

        public int RPCPort { get; set; }
        public bool RPCIndented { get; set; }
        public int RPCIndentSize { get; set; }
        public bool RPCUseTabs { get; set; }

        public int HTTPPort { get; set; }
        public IPEndPoint Endpoint { get; set; }
        public IPAddress BootstrapNode { get; set; }

        public byte NetworkID { get; set; }
        public uint NetworkVersion { get; set; }
        public Cipher.Key ID { get; set; }

        public DaemonConfig()
        {
            DaemonPath = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX) ? Environment.GetEnvironmentVariable("HOME") : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

            DaemonPath = Path.Combine($"{DaemonPath}\\", ".discreet");

            if (!Directory.Exists(DaemonPath)) Directory.CreateDirectory(DaemonPath);

            DBPath = Path.Combine(DaemonPath, "data");
            LogPath = Path.Combine(DaemonPath, "logs");
            WalletPath = Path.Combine(DaemonPath, "wallets");

            if (!Directory.Exists(DBPath)) Directory.CreateDirectory(DBPath);
            if (!Directory.Exists(LogPath)) Directory.CreateDirectory(LogPath);
            if (!Directory.Exists(WalletPath)) Directory.CreateDirectory(WalletPath);

            ConfigPath = Path.Combine(DaemonPath, "config.json");

            DBSize = 4294967296/4; // 1gb

            Port = 6585;

            Endpoint = new IPEndPoint(IPAddress.Any, Port);


            NetworkID = 1;
            NetworkVersion = 1;

            RPCPort = 8350;
            RPCIndented = false;
            RPCIndentSize = 0;
            RPCUseTabs = false;

            HTTPPort = 8351;

            ID = new Network.Peerbloom.NodeId().Value;
        }

        public static DaemonConfig GetDefault()
        {
            return GetConfig();
        }
    }
}
