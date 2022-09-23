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

        public static bool operator ==(InventoryVector x, InventoryVector y) => x.Type == y.Type && x.Hash.Bytes.BEquals(y.Hash.Bytes);
        public static bool operator !=(InventoryVector x, InventoryVector y) => !(x == y);
    }

    public class InventoryVectorComparer : IComparer<InventoryVector>
    {
        public int Compare(InventoryVector x, InventoryVector y)
        {
            return x.Compare(y);
        }

        public bool Equals(InventoryVector x, InventoryVector y)
        {
            return x == y;
        }

        public int GetHashCode([DisallowNull] InventoryVector obj)
        {
            return Coin.Serialization.GetInt32(Cipher.SHA256.HashData(obj.Serialize()).Bytes, 0);
        }
    }
}
