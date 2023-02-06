using Discreet.Wallets.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Wallets.Comparers
{
    public class UTXOEqualityComparer : IEqualityComparer<UTXO>
    {
        public bool Equals(UTXO? x, UTXO? y)
        {
            if (x == null && y == null) return true;
            if (x == y) return true;
            if (x == null || y == null) return false;

            if (x.Type != y.Type) return false;
            if (x.Type == 0)
            {
                if (x.LinkingTag == y.LinkingTag) return true;
            }
            else
            {
                if (x.TransactionSrc == y.TransactionSrc && x.Index == y.Index) return true;
            }

            return false;
        }

        public int GetHashCode([DisallowNull] UTXO obj)
        {
            if (obj.Type == 0)
            {
                return Discreet.Coin.Serialization.GetInt32(obj.LinkingTag.bytes, 0);
            }
            else
            {
                return Discreet.Coin.Serialization.GetInt32(obj.TransactionSrc.Bytes, 0);
            }
        }
    }
}
