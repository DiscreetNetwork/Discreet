using Discreet.Common.Serialize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public class InventoryPacket: IPacketBody
    {
        public int Count { get => Inventory?.Length ?? 0; }
        public InventoryVector[] Inventory { get; set; }

        public InventoryPacket()
        {

        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.WriteSerializableArray(Inventory);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Inventory = reader.ReadSerializableArray<InventoryVector>();
        }

        public int Size => 4 + 36 * Inventory.Length;
    }
}
