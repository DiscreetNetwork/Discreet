using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets.Peerbloom
{
    public class NetPong: IPacketBody
    {
        public byte[] Data { get; set; }

        public NetPong() { }

        public NetPong(Stream s)
        {
            Deserialize(s);
        }

        public NetPong(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            (_, Data) = Coin.Serialization.GetBytes(b, offset);
        }

        public void Deserialize(Stream s)
        {
            Data = Coin.Serialization.GetBytes(s);
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Coin.Serialization.CopyData(b, offset, Data);
            return offset + (uint)Size();
        }

        public void Serialize(Stream s)
        {
            Coin.Serialization.CopyData(s, Data);
        }

        public int Size()
        {
            return Data.Length + 4;
        }
    }
}
