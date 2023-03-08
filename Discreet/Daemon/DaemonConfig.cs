using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
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
                if (_daemon_config == null)
                {
                    var daemonPath = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX) ? Environment.GetEnvironmentVariable("HOME") : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

                    daemonPath = Path.Combine($"{daemonPath}", "discreet");

                    var configPath = Path.Combine(daemonPath, "config.json");

                    var _options = new JsonSerializerOptions();

                    _options.Converters.Add(new Common.Converters.IPAddressConverter());
                    _options.Converters.Add(new Common.Converters.IPEndPointConverter());

                    if (File.Exists(configPath))
                    {
                        try
                        {
                            string _configData = File.ReadAllText(configPath);

                            

                            DaemonConfig _config = JsonSerializer.Deserialize<DaemonConfig>(_configData, _options);

                            if (_config != null)
                            {
                                _daemon_config = _config;
                                _daemon_config.ConfigPath = configPath;
                                _daemon_config.ConfigureDefaults();
                            }
                        }
                        catch
                        {
                            _daemon_config = new DaemonConfig();

                            _daemon_config.Save();
                        }
                    }
                    else
                    {
                        _daemon_config = new DaemonConfig();

                        _daemon_config.Save();
                    }

                }

                return _daemon_config;
            }
        }

        public void Save()
        {
            var _options = new JsonSerializerOptions();

            _options.Converters.Add(new Common.Converters.IPAddressConverter());
            _options.Converters.Add(new Common.Converters.IPEndPointConverter());

            if (!Directory.Exists(DaemonPath)) Directory.CreateDirectory(DaemonPath);

            // sanity check
            if (!Directory.Exists(DBPath)) Directory.CreateDirectory(DBPath);
            if (!Directory.Exists(LogPath)) Directory.CreateDirectory(LogPath);
            if (!Directory.Exists(WalletPath)) Directory.CreateDirectory(WalletPath);

            File.WriteAllText(ConfigPath, Common.Printable.Prettify(JsonSerializer.Serialize<DaemonConfig>(this, _options)));
        }

        public void ConfigureDefaults()
        {
            var daemonPath = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX) ? Environment.GetEnvironmentVariable("HOME") : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

            daemonPath = Path.Combine($"{daemonPath}", "discreet");

            if (DaemonPath == null)
            {
                DaemonPath = daemonPath;
            }

            if (!Directory.Exists(DaemonPath)) Directory.CreateDirectory(DaemonPath);

            if (DBSize == null)
            {
                DBSize = 4294967296 / 4;
            }

            if (DBPath == null)
            {
                DBPath = Path.Combine(DaemonPath, "data");
            }

            if (LogPath == null)
            {
                LogPath = Path.Combine(DaemonPath, "logs");
            }

            if (WalletPath == null)
            {
                WalletPath = Path.Combine(DaemonPath, "wallets");
            }

            if (!Directory.Exists(DBPath)) Directory.CreateDirectory(DBPath);
            if (!Directory.Exists(LogPath)) Directory.CreateDirectory(LogPath);
            if (!Directory.Exists(WalletPath)) Directory.CreateDirectory(WalletPath);

            if (Port == null)
            {
                Port = 9875;
            }

            if (Endpoint == null)
            {
                Endpoint = new IPEndPoint(IPAddress.Any, Port.Value);
            }

            if (RPCPort == null)
            {
                RPCPort = 8350;
            }

            if (RPCIndented == null)
            {
                RPCIndented = false;
            }

            if (RPCIndentSize == null)
            {
                RPCIndentSize = 0;
            }

            if (RPCUseTabs == null)
            {
                RPCUseTabs = false;
            }

            if (PrintStackTraces == null)
            {
                PrintStackTraces = false;
            }

            if (VerboseLevel == null)
            {
                VerboseLevel = 0;
            }

            if(ZMQPort == null)
            {
                ZMQPort = 26833;
            }

            if (HTTPPort == null)
            {
                HTTPPort = 8351;
            }

            if (NetworkID == null)
            {
                NetworkID = 1;
            }

            if (NetworkVersion == null)
            {
                NetworkVersion = 1;
            }

            if (IsPublic == null)
            {
                IsPublic = false;
            }

            if (SigningKey == null || SigningKey.Length != 64)
            {
                SigningKey = Cipher.KeyOps.GenerateSeckey().ToHex();
            }

            if (MintGenesis == null)
            {
                MintGenesis = false;
            }

            if (MaxLogfileSize == null)
            {
                MaxLogfileSize = 10 * 1024 * 1024;
            }

            if (MaxNumLogfiles == null)
            {
                MaxNumLogfiles = 0;
            }

            if (NetConfig == null)
            {
                NetConfig = new NetworkConfig();
            }

            if (DbgConfig == null)
            {
                DbgConfig = new DebugConfig();
            }
            else
            {
                DbgConfig.ConfigureDefaults();
            }

            if (APISets == null)
            {
                APISets = new List<string>(new string[]
                {
                    "read",
                    "txn",
                    "wallet",
                    "storage",
                    "seed_recovery",
                    "status"
                });
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
        public long? DBSize { get; set; }

        public string DBPath { get; set; }
        public string LogPath { get; set; }
        public string WalletPath { get; set; }

        public string ConfigPath { get; set; }


        public int? Port { get; set; }

        public int? RPCPort { get; set; }
        public bool? RPCIndented { get; set; }
        public int? RPCIndentSize { get; set; }
        public bool? RPCUseTabs { get; set; }

        public bool? PrintStackTraces { get; set; }
        public int? VerboseLevel { get; set; }

        public int? ZMQPort { get; set; }

        public int? HTTPPort { get; set; }
        public IPEndPoint Endpoint { get; set; }
        public string BootstrapNode { get; set; }

        public byte? NetworkID { get; set; }
        public uint? NetworkVersion { get; set; }

        public bool? IsPublic { get; set; }

        public string SigningKey { get; set; }

        public bool? MintGenesis { get; set; }

        public long? MaxLogfileSize { get; set; }
        public int? MaxNumLogfiles { get; set; }

        public NetworkConfig NetConfig { get; set; }

        public DebugConfig DbgConfig { get; set; }
        public List<string> APISets { get; set; }

        public DaemonConfig()
        {
            DaemonPath = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX) ? Environment.GetEnvironmentVariable("HOME") : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

            DaemonPath = Path.Combine($"{DaemonPath}", "discreet");

            if (!Directory.Exists(DaemonPath)) Directory.CreateDirectory(DaemonPath);

            ConfigPath = Path.Combine(DaemonPath, "config.json");

            
            DBPath = Path.Combine(DaemonPath, "data");
            LogPath = Path.Combine(DaemonPath, "logs");
            WalletPath = Path.Combine(DaemonPath, "wallets");

            if (!Directory.Exists(DBPath)) Directory.CreateDirectory(DBPath);
            if (!Directory.Exists(LogPath)) Directory.CreateDirectory(LogPath);
            if (!Directory.Exists(WalletPath)) Directory.CreateDirectory(WalletPath);



            DBSize = 4294967296 / 4; // 1gb

            Port = 9875;

            Endpoint = new IPEndPoint(IPAddress.Any, Port.Value);


            NetworkID = 1;
            NetworkVersion = 1;

            IsPublic = false;

            RPCPort = 8350;
            RPCIndented = false;
            RPCIndentSize = 0;
            RPCUseTabs = false;

            PrintStackTraces = false;
            VerboseLevel = 0;

            ZMQPort = 26833;

            HTTPPort = 8351;

            SigningKey = Cipher.KeyOps.GenerateSeckey().ToHex();

            MintGenesis = false;

            MaxLogfileSize = 10 * 1024 * 1024;
            MaxNumLogfiles = 0;

            NetConfig = new NetworkConfig();
            DbgConfig = new DebugConfig();

            APISets = new List<string>(new string[]
            {
                "read",
                "txn",
                "wallet",
                "storage",
                "seed_recovery",
                "status"
            });
        }

        public static DaemonConfig GetDefault()
        {
            return GetConfig();
        }
    }
}
