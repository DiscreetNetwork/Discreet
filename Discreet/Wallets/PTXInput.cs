using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Wallets
{
    public class PTXInput
    {
        public Coin.TXInput Input;
        public Cipher.Key[] M;
        public Cipher.Key[] P;
        public int l;
    }
}
