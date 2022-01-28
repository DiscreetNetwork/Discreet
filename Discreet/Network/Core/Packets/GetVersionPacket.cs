using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Discreet.Network.Core.Packets
{
    public class GetVersionPacket: IPacketBody
    {
        public GetVersionPacket() { }

        public GetVersionPacket(byte[] b, uint offset) { }

        public GetVersionPacket(Stream s) { }

        public void Deserialize(byte[] b, uint offset) { }

        public void Deserialize(Stream s) { }

        public uint Serialize(byte[] b, uint offset) { return offset; }

        public void Serialize(Stream s);
    }
}
