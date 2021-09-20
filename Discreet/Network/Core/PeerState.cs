using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core
{
    [Flags]
    public enum PeerState : byte
    {
        None = 0,
        Compressed = 1 << 0,    // 1
        Encrypted = 1 << 1,     // 2
        Ping = 1 << 2,          // 4
        Ack = 1 << 3,           // 8
        Alive = 1 << 4,         // 16
        Suspect = 1 << 5,       // ...
        Dead = 1 << 6,
    }
}