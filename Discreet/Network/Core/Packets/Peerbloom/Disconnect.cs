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

        public Disconnect(Stream s)
        {
            Deserialize(s);
        }

        public Disconnect(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            Code = (DisconnectCode)Common.Serialization.GetUInt32(b, offset);
        }

        public void Deserialize(Stream s)
        {
            Code = (DisconnectCode)Common.Serialization.GetUInt32(s);
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Common.Serialization.CopyData(b, offset, (uint)Code);
            return offset + 4;
        }

        public void Serialize(Stream s)
        {
            Common.Serialization.CopyData(s, (uint)Code);
        }

        public int Size()
        {
            return 4;
        }
    }
}
