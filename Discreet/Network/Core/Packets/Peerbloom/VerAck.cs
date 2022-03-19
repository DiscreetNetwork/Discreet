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

        public VerAck(Stream s)
        {
            Deserialize(s);
        }

        public VerAck(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            ReflectedEndpoint = Utils.DeserializeEndpoint(b, offset);
            Counter = Coin.Serialization.GetInt32(b, offset + 18);
        }

        public void Deserialize(Stream s)
        {
            ReflectedEndpoint = Utils.DeserializeEndpoint(s);
            Counter = Coin.Serialization.GetInt32(s);
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Utils.SerializeEndpoint(ReflectedEndpoint, b, offset);
            Coin.Serialization.CopyData(b, offset + 18, Counter);

            return offset + 22;
        }

        public void Serialize(Stream s)
        {
            Utils.SerializeEndpoint(ReflectedEndpoint, s);
            Coin.Serialization.CopyData(s, Counter);
        }

        public int Size()
        {
            return 22;
        }
    }
}
