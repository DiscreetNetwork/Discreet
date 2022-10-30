using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Discreet.Cipher.Extensions.ByteArrayExtensions;

namespace Discreet.Network.Core.Packets
{
    public struct InventoryVector
    {
        public ObjectType Type;
        public Cipher.SHA256 Hash;

        public InventoryVector(ObjectType type, Cipher.SHA256 hash) { this.Type = type; this.Hash = hash; }

        public InventoryVector(ObjectType type, long hash) { this.Type = type; this.Hash = new Cipher.SHA256(hash); }

        public byte[] Serialize()
        {
            byte[] rv = new byte[36];
            Coin.Serialization.CopyData(rv, 0, (uint)Type);
            Array.Copy(Hash.Bytes, 0, rv, 4, 32);

            return rv;
        }

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
            return Coin.Serialization.GetInt32(Hash.Bytes, 28);
        }
    }
}
