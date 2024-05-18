using Discreet.Common.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Coin.Models
{
    public class Datum : IHashable
    {
        public byte Version { get; set; }
        public byte[] Data { get; set; }

        public static readonly Func<Datum> Default = () => new Datum { Version = 0, Data = Array.Empty<byte>() };

        public void Serialize(BEBinaryWriter writer)
        {
            writer.Write(Version);
            writer.WriteByteArray(Data);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Version = reader.ReadUInt8();
            Data = reader.ReadByteArray();
        }

        public int Size => 5 + (Data?.Length ?? 0);
    }
}
