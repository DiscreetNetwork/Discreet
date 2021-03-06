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

        public Connect(Stream s)
        {
            Deserialize(s);
        }

        public Connect(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            Endpoint = Utils.DeserializeEndpoint(b, offset);
        }

        public void Deserialize(Stream s)
        {
            Endpoint = Utils.DeserializeEndpoint(s);
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Utils.SerializeEndpoint(Endpoint, b, offset);
            return offset + 18;
        }

        public void Serialize(Stream s)
        {
            Utils.SerializeEndpoint(Endpoint, s);
        }

        public int Size()
        {
            return 18;
        }
    }
}
