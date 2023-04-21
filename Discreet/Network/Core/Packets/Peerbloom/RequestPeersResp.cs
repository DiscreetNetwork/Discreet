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
    public class RequestPeersResp: IPacketBody
    {
        public IPEndPoint[] Peers { get; set; }

        public RequestPeersResp() { }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.WriteArray(Peers, (x, writer) => Utils.SerializeEndpoint(x, writer));
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Peers = reader.ReadArray((ref MemoryReader reader) => Utils.DeserializeEndpoint(ref reader));
        }
        
        public int Size => 4 + Peers.Length * 18;
    }
}
