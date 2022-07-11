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
    }
}
