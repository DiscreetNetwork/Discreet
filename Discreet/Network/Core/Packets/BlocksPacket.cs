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
    public class BlocksPacket: IPacketBody
    {
        public Block[] Blocks { get; set; }

        public BlocksPacket()
        {

        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.WriteSerializableArray(Blocks);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Blocks = reader.ReadSerializableArray<Block>();
        }

        public int Size
        {
            get => 4 + Blocks?.Select(x => x.Size).Aggregate(0, (x, y) => x + y) ?? 0;
        }
    }
}
