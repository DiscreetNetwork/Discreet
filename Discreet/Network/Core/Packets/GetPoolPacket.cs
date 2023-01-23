using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    /// <summary>
    /// Used to fetch transaction pool after startup.
    /// </summary>
    public class GetPoolPacket : IPacketBody
    {
        // empty packet

        public GetPoolPacket()
        {

        }

        public GetPoolPacket(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public GetPoolPacket(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            return;
        }

        public void Deserialize(Stream s)
        {
            return;
        }

        public uint Serialize(byte[] b, uint offset)
        {
            return offset;
        }

        public void Serialize(Stream s)
        {
            return;
        }

        public int Size()
        {
            return 0;
        }
    }
}
