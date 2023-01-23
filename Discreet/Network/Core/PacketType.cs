using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core
{
    public enum PacketType: byte
    {
        NONE = 0,

        /* VERSION */
        VERSION = 1,
        VERACK = 2,

        /* USED FOR SYNCING AND INFORMATION */
        INVENTORY = 3,
        GETBLOCKS = 4,
        BLOCKS = 5,
        GETTXS = 6,
        TXS = 7,
        GETHEADERS = 9,
        HEADERS = 10,
        GETPOOL = 11,
        POOL = 12,

        /* currently unused for testnet */

        //GETTXSINBLOCK = 13, 

        NOTFOUND = 14,

        /* USED FOR GOSSIPING REJECTION */
        REJECT = 15,

        /* USED TO SEND NETWORK-WIDE MESSAGES */
        ALERT = 16,

        /* USED FOR SENDING/PROPAGATING TRANSACTIONS AND BLOCKS */
        SENDTX = 17,
        SENDBLOCK = 18,

        /* DEBUG MESSAGING */
        SENDMSG = 19,

        /* USED BY PEERBLOOM */
        REQUESTPEERS = 22,
        REQUESTPEERSRESP = 23,
        NETPING = 24,
        NETPONG = 25,
        OLDMESSAGE = 26,

        /* DISCONNECT */
        DISCONNECT = 27,
    }
}
