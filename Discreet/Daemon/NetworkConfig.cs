using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Daemon
{
    public class NetworkConfig
    {
        public int? MinDesiredConnections { get; set; }
        public int? MaxDesiredConnections { get; set; }

        public NetworkConfig()
        {
            MinDesiredConnections = 10;
            MaxDesiredConnections = MinDesiredConnections * 4;
        }
    }
}
