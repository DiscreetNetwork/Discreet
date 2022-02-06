using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets.Peerbloom
{
    public class NetPing: IPacketBody
    {
        public byte[] Data { get; set; }

        public NetPing() { }

        public NetPing(Stream s)
        {
            Deserialize(s);
        }

        public NetPing(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            Data = new byte[b.Length - offset];
            Array.Copy(b, offset, Data, 0, Data.Length);
        }

        public void Deserialize(Stream s)
        {
            using MemoryStream _ms = new MemoryStream();

            byte[] buf = new byte[64];
            int bytesRead;

            while ((bytesRead = s.Read(buf, 0, buf.Length)) > 0)
            {
                _ms.Write(buf, 0, bytesRead);
            }

            Data = _ms.ToArray();
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Array.Copy(Data, 0, b, offset, Data.Length);
            return offset + (uint)Data.Length;
        }

        public void Serialize(Stream s)
        {
            s.Write(Data);
        }

        public int Size()
        {
            return Data.Length;
        }
    }
}
