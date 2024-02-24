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
    public class VerAck : IPacketBody
    {
        public IPEndPoint ReflectedEndpoint { get; set; }

        /* this field is set to >0 if reflected by peer accepting the connection. This is used to prevent network loops. */
        public int Counter { get; set; }

        public VerAck() { }

        public void Serialize(BEBinaryWriter writer)
        {
            Utils.SerializeEndpoint(ReflectedEndpoint, writer);
            writer.Write(Counter);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            ReflectedEndpoint = Utils.DeserializeEndpoint(ref reader);
            Counter = reader.ReadInt32();
        }

        public int Size => 22;
    }
}
