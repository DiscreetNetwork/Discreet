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

        public RequestPeers(Stream s)
        {
            Deserialize(s);
        }

        public RequestPeers(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public void Deserialize(byte[] b, uint offset)
        {

            Endpoint = Utils.DeserializeEndpoint(b, offset);
            offset += 18;
            MaxPeers = Common.Serialization.GetInt32(b, offset);
        }

        public void Deserialize(Stream s)
        {
            Endpoint = Utils.DeserializeEndpoint(s);
            MaxPeers = Common.Serialization.GetInt32(s);
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Utils.SerializeEndpoint(Endpoint, b, offset);
            offset += 18;
            Common.Serialization.CopyData(b, offset, MaxPeers);
            return offset + 4;
        }

        public void Serialize(Stream s)
        {
            Utils.SerializeEndpoint(Endpoint, s);
            Common.Serialization.CopyData(s, MaxPeers);
        }

        public int Size()
        {
            return 22;
        }
    }
}
