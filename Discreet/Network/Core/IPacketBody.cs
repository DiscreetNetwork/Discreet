using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using Discreet.Common.Serialize;

namespace Discreet.Network.Core
{
    public interface IPacketBody : ISerializable
    {
        public uint Checksum()
        {
            using MemoryStream _ms = new MemoryStream();
            this.Serialize(_ms);

            uint _checksum = Common.Serialization.GetUInt32(SHA256.HashData(SHA256.HashData(_ms.ToArray())), 0);

            return _checksum;
        }
    }
}
