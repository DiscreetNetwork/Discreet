using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.DB
{
    public class OrphanBlockException : Exception
    {
        public long ChainHeight;
        public long BlockHeight;
        public Coin.Block Block;

        public OrphanBlockException(string msg) : base(msg) { }

        public OrphanBlockException(string msg, long h, long b, Coin.Block bl) : base(msg)
        {
            ChainHeight = h; BlockHeight = b; Block = bl;
        }
    }
}
