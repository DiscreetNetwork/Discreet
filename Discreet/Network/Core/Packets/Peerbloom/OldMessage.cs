using Discreet.Common.Serialize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets.Peerbloom
{
    public class OldMessage: IPacketBody
    {
        public string MessageID { get; set; }
        public string Message { get; set; }

        public OldMessage() { }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.Write(MessageID);
            writer.Write(Message);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            MessageID = reader.ReadLengthPrefixedString();
            Message = reader.ReadLengthPrefixedString();
        }

        public int Size => 8 + Encoding.UTF8.GetByteCount(MessageID) + Encoding.UTF8.GetByteCount(Message);
    }
}
