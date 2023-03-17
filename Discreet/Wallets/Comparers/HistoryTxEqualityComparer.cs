using Discreet.Wallets.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Wallets.Comparers
{
    public class HistoryTxEqualityComparer : IEqualityComparer<HistoryTx>
    {
        public bool Equals(HistoryTx? x, HistoryTx? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.TxID == y.TxID;
        }

        public int GetHashCode([DisallowNull] HistoryTx obj)
        {
            return obj.TxID.GetHashCode();
        }
    }
}
