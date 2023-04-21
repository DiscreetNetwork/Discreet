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
    public class Connect : IPacketBody
    {
        public IPEndPoint Endpoint { get; set; }

        public Connect() { }

        public Connect(MemoryStream s)
        {
            this.Deserialize(s);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Endpoint = Utils.DeserializeEndpoint(ref reader);
        }

        public void Serialize(BEBinaryWriter writer)
        {
            Utils.SerializeEndpoint(Endpoint, writer);
        }

        public int Size => 18;
    }
}
