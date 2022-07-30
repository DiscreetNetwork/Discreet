using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.DB
{
    public class U64
    {
        public ulong Value;

        public U64(ulong value)
        {
            Value = value;
        }
    }

    public class L64
    {
        public long Value;

        public L64(long value)
        {
            Value = value;
        }
    }

    public class U32
    {
        public uint Value;

        public U32(uint value)
        {
            Value = value;
        }
    }
}