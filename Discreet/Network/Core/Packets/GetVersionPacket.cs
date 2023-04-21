using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Discreet.Common.Serialize;

namespace Discreet.Network.Core.Packets
{
    public class GetVersionPacket: IPacketBody
    {
        public GetVersionPacket() { }

        public void Serialize(BEBinaryWriter writer) { }

        public void Deserialize(ref MemoryReader reader) { }

        public int Size => 0;
    }
}
