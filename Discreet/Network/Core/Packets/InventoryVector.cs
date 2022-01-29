using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public struct InventoryVector
    {
        public ObjectType Type;
        public Cipher.SHA256 Hash;
    }
}
