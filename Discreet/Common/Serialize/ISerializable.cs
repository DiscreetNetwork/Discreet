using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Common.Serialize
{
    public interface ISerializable
    {
        public int Size { get; }
        public void Serialize(BinaryWriter writer);
        public void Deserialize(ref MemoryReader reader);
    }
}
