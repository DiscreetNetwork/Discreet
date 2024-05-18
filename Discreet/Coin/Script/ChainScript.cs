using Discreet.Common.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Coin.Script
{
    public class ChainScript : IHashable
    {
        public byte[] Code { get; set; }
        public byte[] Data { get; set; }

        public uint Version { get; set; }

        public int Size => 4 + (Version > 0 ? 8 + (Code.Length + Data.Length) : 0);

        public void Deserialize(ref MemoryReader reader)
        {
            Version = reader.ReadUInt32();

            if (Version > 0)
            {
                Code = reader.ReadByteArray();
                Data = reader.ReadByteArray();
            }
        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.Write(Version);
            
            if (Version > 0)
            {
                writer.WriteByteArray(Code);
                writer.WriteByteArray(Data);
            }
        }
    }
}
