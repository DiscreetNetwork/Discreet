using Discreet.Common.Serialize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets.Peerbloom
{
    public class Disconnect : IPacketBody
    {
        public DisconnectCode Code { get; set; }

        public Disconnect() { }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.Write((uint)Code);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Code = (DisconnectCode)reader.ReadUInt32();
        }

        public int Size => 4;
    }
}
