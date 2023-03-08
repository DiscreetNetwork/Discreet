using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

namespace Discreet.Network.Core
{
    public interface IPacketBody
    {
        public void Deserialize(byte[] b, uint offset);
        public void Deserialize(Stream s);
        public uint Serialize(byte[] b, uint offset);
        public void Serialize(Stream s);

        public uint Checksum()
        {
            using MemoryStream _ms = new MemoryStream();
            Serialize(_ms);

            uint _checksum = Common.Serialization.GetUInt32(SHA256.HashData(SHA256.HashData(_ms.ToArray())), 0);

            return _checksum;
        }

        public int Size();
    }
}
