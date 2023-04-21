using Discreet.Common.Serialize;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public class SendMessagePacket: IPacketBody
    {
        public string Message { get; set; }

        public SendMessagePacket()
        {

        }
        
        public void Serialize(BEBinaryWriter writer)
        {
            writer.Write(Message);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Message = reader.ReadLengthPrefixedString();
        }
        
        public int Size => 4 + Message.Length;
    }
}
