using Discreet.Wallets.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Wallets.Comparers
{
    /// <summary>
    /// Sorts UTXO by their value, descending
    /// </summary>
    public class UTXOAmountComparer: IComparer<UTXO>
    {
        public int Compare(UTXO? a, UTXO? b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            var res = b.DecodedAmount.CompareTo(a.DecodedAmount);
            if (res == 0) res = a.TransactionSrc.CompareTo(b.TransactionSrc);
            if (res == 0) res = (a.DecodeIndex ?? 0) - (b.DecodeIndex ?? 0);
            if (res == 0) res = (int)a.Index - (int)b.Index;

            return res;
        }
    }
}
