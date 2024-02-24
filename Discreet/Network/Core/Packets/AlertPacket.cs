using Discreet.Common.Serialize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public class AlertPacket: IPacketBody
    {
        public Cipher.SHA256 Checksum { get; set; }
        public Cipher.Signature Sig { get; set; }
        public string Message { get; set; }

        public AlertPacket()
        {

        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.WriteSHA256(Checksum);
            Sig.Serialize(writer);
            writer.Write(Message);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Checksum = reader.ReadSHA256();
            Sig = reader.ReadSerializable<Cipher.Signature>();
            Message = reader.ReadLengthPrefixedString();
        }

        public int Size => 32 + 96 + 4 + Message.Length;
    }
}
