using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets.Peerbloom
{
    public enum DisconnectCode : uint
    {
        CLEAN = 0,
        FAULTY = 1,
        FATAL_ERROR = 2,
        CONNECTING_TIMEOUT = 3,
        MAX_CONNECTING_PEERS = 4,
        MAX_INBOUND_PEERS = 5,
        MAX_OUTBOUND_PEERS = 6,
        MAX_FEELER_PEERS = 7,
    }
}
