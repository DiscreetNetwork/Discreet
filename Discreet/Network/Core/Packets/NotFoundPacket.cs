using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public class NotFoundPacket: IPacketBody
    {
        public uint Count { get; set; }
        public InventoryVector[] Inventory;

        public NotFoundPacket()
        {

        }

        public NotFoundPacket(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public NotFoundPacket(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            Count = Common.Serialization.GetUInt32(b, offset);
            offset += 4;

            Inventory = new InventoryVector[Count];

            for (int i = 0; i < Count; i++)
            {
                Inventory[i] = new InventoryVector
                {
                    Type = (ObjectType)Common.Serialization.GetUInt32(b, offset),
                    Hash = new Cipher.SHA256(b, offset + 4)
                };
                offset += 36;
            }
        }

        public void Deserialize(Stream s)
        {
            byte[] uintbuf = new byte[4];

            s.Read(uintbuf);
            Count = Common.Serialization.GetUInt32(uintbuf, 0);

            Inventory = new InventoryVector[Count];

            for (int i = 0; i < Count; i++)
            {
                byte[] hashbuf = new byte[32];

                s.Read(uintbuf);
                s.Read(hashbuf);
                Inventory[i] = new InventoryVector
                {
                    Type = (ObjectType)Common.Serialization.GetUInt32(uintbuf, 0),
                    Hash = new Cipher.SHA256(hashbuf, false),
                };
            }
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Common.Serialization.CopyData(b, offset, Count);
            offset += 4;

            foreach (InventoryVector v in Inventory)
            {
                Common.Serialization.CopyData(b, offset, (uint)v.Type);
                Array.Copy(v.Hash.Bytes, 0, b, offset + 4, 32);
                offset += 36;
            }

            return offset;
        }

        public void Serialize(Stream s)
        {
            s.Write(Common.Serialization.UInt32(Count));

            foreach (InventoryVector v in Inventory)
            {
                s.Write(Common.Serialization.UInt32((uint)v.Type));
                s.Write(v.Hash.Bytes);
            }
        }

        public int Size()
        {
            return 4 + 36 * Inventory.Length;
        }
    }
}
