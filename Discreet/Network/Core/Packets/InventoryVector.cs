using Discreet.Common.Serialize;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Discreet.Cipher.Extensions.ByteArrayExtensions;

namespace Discreet.Network.Core.Packets
{
    public struct InventoryVector : ISerializable
    {
        public ObjectType Type;
        public Cipher.SHA256 Hash;

        public InventoryVector(ObjectType type, Cipher.SHA256 hash) { this.Type = type; this.Hash = hash; }

        public InventoryVector(ObjectType type, long hash) { this.Type = type; this.Hash = new Cipher.SHA256(hash); }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.Write((uint)Type);
            writer.WriteSHA256(Hash);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Type = (ObjectType)reader.ReadUInt32();
            Hash = reader.ReadSHA256();
        }

        public int Size => 36;

        public int Compare(InventoryVector y)
        {
            int tcmp = ((uint)Type).CompareTo((uint)y.Type);
            if (tcmp == 0)
            {
                return Hash.Bytes.Compare(y.Hash.Bytes);
            }

            return tcmp;
        }

        public override bool Equals([NotNullWhen(true)] object obj)
        {
            if (obj == null) return false;
            if (obj is InventoryVector b) return Type == b.Type && Hash.Bytes.BEquals(b.Hash.Bytes);
            return false;
        }

        public static bool operator ==(InventoryVector x, InventoryVector y) => x.Type == y.Type && x.Hash.Bytes.BEquals(y.Hash.Bytes);
        public static bool operator !=(InventoryVector x, InventoryVector y) => !(x == y);

        public override int GetHashCode()
        {
            return Common.Serialization.GetInt32(Hash.Bytes, 28);
        }
    }
}
