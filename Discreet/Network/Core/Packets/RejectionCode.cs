using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public enum RejectionCode : byte
    {
        MALFORMED = 1,          //used if decoding the data returns an exception
        INVALID = 2,            //used if the data verification returns an exception
        OBSOLETE = 3,           //used for obsolete data formats (transaction type 1)
        DUPLICATE = 4,          //not currently used.
        NONSTANDARD = 5,        //not currently used.
        INSUFFICIENT_FEE = 6,   //used for transactions which provide insufficient fee (unused for testnet).
    }
}
