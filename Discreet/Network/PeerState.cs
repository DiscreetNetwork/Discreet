using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network
{
    public enum PeerState: uint
    {
        Normal = 0,     //peer is available for normal network activity
        Syncing = 1,    //peer is syncing their chain
        Startup = 2,    //peer is connecting to other peers and receiving versions
    }
}
