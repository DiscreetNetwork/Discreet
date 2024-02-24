using Discreet.Common.Serialize;
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

        public void Serialize(BEBinaryWriter writer) { }
        
        public void Deserialize(ref MemoryReader reader) { }

        public int Size => 0;
    }
}
