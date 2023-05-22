using Discreet.Common.Serialize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public class GetUnitsPacket : IPacketBody
    {
        public int Count { get => Transactions?.Length ?? 0; }
        public Cipher.SHA256[] Transactions { get; set; }

        public GetUnitsPacket()
        {

        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.WriteSHA256Array(Transactions);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Transactions = reader.ReadSHA256Array();
        }

        public int Size => 4 + 32 * Count;
    }
}
