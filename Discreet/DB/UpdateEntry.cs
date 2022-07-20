using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.DB
{
    public enum UpdateRule: int
    {
        NULL = 0,
        ADD = 1,
        DEL = 2,
        UNDEF = 3,
    }

    public enum UpdateType: int
    {
        NULL = 0,
        SPENTKEY = 1,
        OUTPUT = 2,
        PUBOUTPUT = 3,
        OUTPUTINDICES = 4,
        TX = 5,
        TXINDEX = 6,
        BLOCKHEADER = 7,
        BLOCKHEIGHT = 8,
        BLOCK = 9,
    }

    public class UpdateEntry
    {
        public UpdateRule rule { get; private set; }
        public UpdateType type { get; private set; }
        public byte[] key;
        public byte[] value;

        public UpdateEntry(byte[] k, byte[] v, UpdateRule r, UpdateType t)
        {
            rule = r;
            key = k;
            value = v;
            type = t;
        }

        public UpdateEntry(Coin.Transparent.TXInput input)
        {
            rule = UpdateRule.DEL;
            key = input.Serialize();
            value = Array.Empty<byte>();
            type = UpdateType.PUBOUTPUT;
        }
    }
}
