using Discreet.Cipher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin.Models;

namespace Discreet.Wallets.Models
{
    public class PrivateTxInput
    {
        public TXInput Input;
        public Key[] M;
        public Key[] P;
        public int l;
    }
}
