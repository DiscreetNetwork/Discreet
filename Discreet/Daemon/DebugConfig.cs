using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Daemon
{
    public class DebugConfig
    {
        public bool? DebugMode { get; set; }
        public bool? SkipSyncing {  get; set; }
        public bool? CheckBlockchain { get; set; }
        public bool? DebugPrints { get; set; }

        public DebugConfig()
        {
            DebugMode = false;
            SkipSyncing = false;
            CheckBlockchain = false;
            DebugPrints = false;
        }

        public void ConfigureDefaults()
        {
            if (DebugMode == null) DebugMode = false;
            if (SkipSyncing == null) SkipSyncing = false;
            if (CheckBlockchain == null) CheckBlockchain = false;
            if (DebugPrints == null) DebugPrints = false;
        }
    }
}
