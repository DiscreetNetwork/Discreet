using Discreet.Common.Serialize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public class GetBlocksPacket: IPacketBody
    {
        public int Count { get => Blocks?.Length ?? 0; }
        public Cipher.SHA256[] Blocks { get; set; }

        public GetBlocksPacket()
        {

        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.WriteSHA256Array(Blocks);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Blocks = reader.ReadSHA256Array();
        }

        public int Size => 4 + 32 * Blocks.Length;
    }
}
