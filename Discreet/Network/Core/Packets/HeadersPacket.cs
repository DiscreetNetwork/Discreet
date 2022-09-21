using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    internal class HeadersPacket: IPacketBody
    {
        public uint Count { get; set; }
        public Coin.BlockHeader[] Headers { get; set; }

        public HeadersPacket()
        {

        }

        public HeadersPacket(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public HeadersPacket(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            Count = Coin.Serialization.GetUInt32(b, offset);
            offset += 4;

            Headers = new Coin.BlockHeader[Count];
            for (int i = 0; i < Count; i++)
            {
                Headers[i] = new Coin.BlockHeader();
                offset = Headers[i].Deserialize(b, offset);
            }
        }

        public void Deserialize(Stream s)
        {
            using MemoryStream _ms = new MemoryStream();

            byte[] buf = new byte[4096];
            int bytesRead;

            while ((bytesRead = s.Read(buf, 0, buf.Length)) > 0)
            {
                _ms.Write(buf, 0, bytesRead);
            }

            Deserialize(_ms.ToArray(), 0);
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Coin.Serialization.CopyData(b, offset, Count);
            offset += 4;

            for (int i = 0; i < Count; i++)
            {
                Headers[i].Serialize(b, offset);
                offset += Headers[i].Size();
            }

            return offset;
        }

        public void Serialize(Stream s)
        {
            s.Write(Coin.Serialization.UInt32(Count));

            foreach (Coin.BlockHeader header in Headers)
            {
                s.Write(header.Serialize());
            }
        }

        public int Size()
        {
            int rv = 4;

            foreach (Coin.BlockHeader header in Headers)
            {
                rv += (int)header.Size();
            }

            return rv;
        }
    }
}
