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
        public uint MessageLen { get; set; }
        public string Message { get; set; }

        public SendMessagePacket()
        {

        }

        public SendMessagePacket(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public SendMessagePacket(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            MessageLen = Common.Serialization.GetUInt32(b, offset);
            offset += 4;

            Message = Encoding.UTF8.GetString(b, (int)offset, (int)MessageLen);
        }

        public void Deserialize(Stream s)
        {
            byte[] _messageLen = new byte[4];
            s.Read(_messageLen);
            MessageLen = Common.Serialization.GetUInt32(_messageLen, 0);

            byte[] _message = new byte[MessageLen];
            s.Read(_message);
            Message = Encoding.UTF8.GetString(_message);
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Common.Serialization.CopyData(b, offset, MessageLen);
            offset += 4;

            Array.Copy(Encoding.UTF8.GetBytes(Message), 0, b, offset, MessageLen);
            offset += MessageLen;

            return offset;
        }

        public void Serialize(Stream s)
        {
            s.Write(Common.Serialization.UInt32(MessageLen));
            s.Write(Encoding.UTF8.GetBytes(Message));
        }

        public int Size()
        {
            return 4 + Message.Length;
        }
    }
}
