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
    public class RequestPeers : IPacketBody
    {
        public IPEndPoint Endpoint;

        public int MaxPeers;

        public RequestPeers() { }

        public void Serialize(BEBinaryWriter writer)
        {
            Utils.SerializeEndpoint(Endpoint, writer);
            writer.Write(MaxPeers);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Endpoint = Utils.DeserializeEndpoint(ref reader);
            MaxPeers = reader.ReadInt32();
        }

        public int Size => 22;
    }
}
