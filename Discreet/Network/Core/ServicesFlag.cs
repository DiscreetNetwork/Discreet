using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core
{
    [Flags]
    public enum ServicesFlag: uint
    {
        None = 0,
        Public = 1 << 0,    //if set, node has set static address; if not, is behind NAT
        Full = 1 << 1,      //if node stores entire block history
        API = 1 << 2,       //if node enables API interactions
    }
}
