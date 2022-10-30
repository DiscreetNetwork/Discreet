using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network
{
    public enum RequestCallbackContext: uint
    {
        SUCCESS = 0,
        NOTFOUND = 1,
        STALE = 2,
        INVALID = 3,
    }
}
