using Discreet.Network.Peerbloom.Protocol.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom.Protocol.Tcp
{
    public class NetworkRequestPacket : ReadPacketBase
    {
        public string PacketType { get; set; }

        public NetworkRequestPacket(byte[] bytes) : base(bytes)
        {
            PacketType = base.ReadString();
        }

        public T ConvertTo<T>() where T : class
        {
            if(typeof(T) == typeof(PingRequestPacket))
            {
                return new PingRequestPacket(base.ReadInt()) as T;
            }

            if(typeof(T) == typeof(ReadPacketBase))
            {
                return this as T;
            }

            throw new NotSupportedException(typeof(T).Name);
        }
    }
}
