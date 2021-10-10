using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Visor
{
    public class VisorConfig
    {
        public string VisorPath;
        public long DBSize;

        public string DBPath;
        public string LogPath;
        public string WalletPath;

        public string ConfigPath;

        public VisorConfig()
        {
            VisorPath = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX) ? Environment.GetEnvironmentVariable("HOME") : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            VisorPath = Path.Combine(VisorPath, ".discreet");

            DBPath = Path.Combine(VisorPath, "data");
            LogPath = Path.Combine(VisorPath, "logs");
            WalletPath = Path.Combine(VisorPath, "wallets");

            ConfigPath = Path.Combine(VisorPath, "config.json");

            DBSize = 4294967296/4; // 1gb
        }

        public static VisorConfig GetDefault()
        {
            return new VisorConfig();
        }
    }
}
