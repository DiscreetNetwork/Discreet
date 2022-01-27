using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Discreet.Network.Common
{
    public class Packet
    {
        public PacketHeader Header { get; set; }

        public IPacketBody Body { get; set; }

        public PacketType Type()
        {
            return Header.Command;
        }

        public void Populate(Stream s)
        {
            Header = new PacketHeader(s);

            byte[] bodyData = new byte[Header.Length];
            int num = s.Read(bodyData);

            if (num < Header.Length)
            {
                throw new Exception($"Discreet.Network.Common.IPacket.Populate: expected {Header.Length} bytes in payload, but got {num}");
            }

            Body = DecodePacketBody(Header.Command, bodyData);
        }

        public static IPacketBody DecodePacketBody(PacketType t, byte[] bodyData)
        {
            switch (t)
            {
                default:
                    throw new NotImplementedException("Unimplemented");
            }
        }
    }
}
