using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin.Models;

namespace Discreet.WalletsLegacy
{
    public class PTXInput
    {
        public TXInput Input;
        public Cipher.Key[] M;
        public Cipher.Key[] P;
        public int l;
    }
}
