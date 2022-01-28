using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Discreet.Network.Core
{
    public interface IPacketBody
    {
        public void Deserialize(byte[] b, uint offset);
        public void Deserialize(Stream s);
        public uint Serialize(byte[] b, uint offset);
        public void Serialize(Stream s);
    }
}
