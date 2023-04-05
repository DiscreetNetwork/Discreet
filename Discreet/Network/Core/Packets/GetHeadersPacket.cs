using Discreet.Common.Serialize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public class GetHeadersPacket : IPacketBody
    {
        public long StartingHeight { get; set; }
        public uint Count { get; set; }
        public Cipher.SHA256[] Headers { get; set; }

        public GetHeadersPacket()
        {

        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.Write(StartingHeight);
            writer.Write(Count);
            writer.WriteSHA256Array(Headers);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            StartingHeight = reader.ReadInt64();
            Count = reader.ReadUInt32();
            Headers = reader.ReadSHA256Array();
        }

        public int Size => 16 + ((Headers == null) ? 0 : 32 * Headers.Length);
    }
}
