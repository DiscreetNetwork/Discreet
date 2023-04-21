using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets.Comparers
{
    public class AlertEqualityComparer : IEqualityComparer<AlertPacket>
    {
        bool IEqualityComparer<AlertPacket>.Equals(AlertPacket x, AlertPacket y)
        {
            return x.Checksum == y.Checksum;
        }

        int IEqualityComparer<AlertPacket>.GetHashCode(AlertPacket obj)
        {
            return obj.Checksum.GetHashCode();
        }
    }
}
