using Discreet.Common.Serialize;
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

        public void Serialize(BEBinaryWriter writer)
        {
            writer.WriteByteArray(Data);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Data = reader.ReadByteArray();
        }

        public int Size => Data.Length + 4;
    }
}
