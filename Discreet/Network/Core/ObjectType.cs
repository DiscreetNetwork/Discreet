using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core
{
    public enum ObjectType: uint
    {
        None = 0,
        Transaction = 1,
        BlockHeader = 2,
        Block = 3,
    }
}
