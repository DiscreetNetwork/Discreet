using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Packets
{
    public abstract class WireMessage
    {
        public abstract WireMessageType Type { get; }
    }
}