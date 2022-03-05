using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.RPC.Common
{
    [Flags]
    public enum APISet: int
    {
        DEFAULT = 0,
        READ = 1 << 0,
        TXN = 1 << 1,
        WALLET = 1 << 2,
        STORAGE = 1 << 3,
        SEED_RECOVERY = 1 << 4,
    } 
}
