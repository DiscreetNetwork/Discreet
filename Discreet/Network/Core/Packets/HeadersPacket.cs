using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin.Models;
using Discreet.Common.Serialize;

namespace Discreet.Network.Core.Packets
{
    public class HeadersPacket: IPacketBody
    {
        public int Count { get => Headers?.Length ?? 0; }
        public BlockHeader[] Headers { get; set; }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.WriteSerializableArray(Headers);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Headers = reader.ReadSerializableArray<BlockHeader>();
        }

        public int Size
        {
            get => 4 + Headers?.Select(x => x.Size).Aggregate(0, (x , y) => x + y) ?? 0;
        }
    }
}
