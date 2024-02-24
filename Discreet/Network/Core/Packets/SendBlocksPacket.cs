using Discreet.Coin.Models;
using Discreet.Common.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public class SendBlocksPacket : IPacketBody
    {
        public Block[] Blocks { get; set; }

        /* not serialized with the packet body. used to pass error information onto the handler. */
        public string Error { get; set; }

        public SendBlocksPacket()
        {

        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.WriteSerializableArray(Blocks);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            try
            {
                Blocks = reader.ReadSerializableArray<Block>();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }

        public int Size
        {
            get => 4 + Blocks?.Select(x => x.Size)?.Aggregate(0, (x, y) => x + y) ?? 0;
        }
    }
}
