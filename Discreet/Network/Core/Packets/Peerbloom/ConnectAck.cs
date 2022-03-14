using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets.Peerbloom
{
    public class ConnectAck : IPacketBody
    {
        public bool IsPublic { get; set; }
        public bool Acknowledged { get; set; }
        public IPEndPoint ReflectedEndpoint { get; set; }

        public ConnectAck() { }

        public ConnectAck(Stream s)
        {
            Deserialize(s);
        }

        public ConnectAck(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            IsPublic = b[offset] != 0;
            Acknowledged = b[offset + 1] != 0;
            ReflectedEndpoint = Utils.DeserializeEndpoint(b, offset + 2);
        }

        public void Deserialize(Stream s)
        {
            IsPublic = s.ReadByte() != 0;
            Acknowledged = s.ReadByte() != 0;
            ReflectedEndpoint = Utils.DeserializeEndpoint(s);
        }

        public uint Serialize(byte[] b, uint offset)
        {
            b[offset] = (byte)(IsPublic ? 1 : 0);
            b[offset + 1] = (byte)(Acknowledged ? 1 : 0);
            Utils.SerializeEndpoint(ReflectedEndpoint, b, offset + 2);

            return offset + 20;
        }

        public void Serialize(Stream s)
        {
            s.WriteByte((byte)(IsPublic ? 1 : 0));
            s.WriteByte((byte)(Acknowledged ? 1 : 0));
            Utils.SerializeEndpoint(ReflectedEndpoint, s);
        }

        public int Size()
        {
            return 20;
        }
    }
}
