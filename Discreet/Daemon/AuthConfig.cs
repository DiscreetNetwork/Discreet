using Discreet.Cipher;
using Discreet.Daemon.BlockAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Daemon
{
    public class AuthConfig
    {
        public bool? Author { get; set; }
        public int? DataSourcePort { get; set; }
        public int? FinalizePort { get; set; }

        public int? Pid { get; set; }
        public int? NProc { get; set; }

        public List<string> SigningKeys { get; set; }
        public List<string> AuthorityKeys { get; set; }

        public AuthConfig()
        {
            Author = false;
            DataSourcePort = 8370;
            FinalizePort = 8371;
            Pid = 0;
            NProc = 1;
            SigningKeys = new List<string>();
            AuthorityKeys = new List<string>();
        }

        public void ConfigureDefaults()
        {
            Author ??= false;
            DataSourcePort ??= 8370;
            FinalizePort ??= 8371;

            Pid ??= 0;
            NProc ??= 1;

            if (SigningKeys == null || SigningKeys.Count == 0)
            {
                SigningKeys = new List<string>{ KeyOps.GenerateSeckey().ToHex() };
            }

            if (AuthorityKeys == null || AuthorityKeys.Count == 0)
            {
                AuthorityKeys = AuthKeys.Defaults.Select(x => x.ToHex()).ToList();
            }
        }
    }
}
