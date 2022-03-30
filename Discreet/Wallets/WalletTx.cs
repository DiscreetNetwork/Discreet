using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin;

namespace Discreet.Wallets
{
    public class WalletTx
    {
        public FullTransaction Tx { get; set; }
        public long Timestamp { get; set; }

        /* what can be extracted will be extracted. otherwise, null. */
        public IAddress[] Receivers { get; set; }
        public IAddress[] Senders { get; set; }

        // WIP
    }
}
