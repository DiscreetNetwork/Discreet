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
    public class SendPreblockPacket : IPacketBody
    {
        public Block Block { get; set; }

        /* not serialized with the packet body. used to pass error information onto the handler. */
        public string Error { get; set; }

        public SendPreblockPacket()
        {

        }

        public void Serialize(BEBinaryWriter writer)
        {
            Block.Serialize(writer);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            try
            {
                Block = reader.ReadSerializable<Block>();
            }
            catch (Exception e)
            {
                Error = e.Message;
            }
        }

        public int Size => (int)Block.GetSize();
    }
}
