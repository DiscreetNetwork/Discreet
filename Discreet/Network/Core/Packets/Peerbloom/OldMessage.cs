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
        public uint MessageIDLen { get; set; }
        public string MessageID { get; set; }
        public uint MessageLen { get; set; }
        public string Message { get; set; }

        public OldMessage() { }

        public OldMessage(Stream s)
        {
            Deserialize(s);
        }

        public OldMessage(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            MessageIDLen = Common.Serialization.GetUInt32(b, offset);
            offset += 4;

            MessageID = Encoding.UTF8.GetString(b, (int)offset, (int)MessageIDLen);
            offset += MessageIDLen;

            MessageLen = Common.Serialization.GetUInt32(b, offset);
            offset += 4;

            Message = Encoding.UTF8.GetString(b, (int)offset, (int)MessageLen);
        }

        public void Deserialize(Stream s)
        {
            byte[] uintbuf = new byte[4];
            s.Read(uintbuf);
            MessageIDLen = Common.Serialization.GetUInt32(uintbuf, 0);

            byte[] _messageID = new byte[MessageIDLen];
            s.Read(_messageID);
            MessageID = Encoding.UTF8.GetString(_messageID);

            s.Read(uintbuf);
            MessageLen = Common.Serialization.GetUInt32(uintbuf, 0);

            byte[] _message = new byte[MessageLen];
            s.Read(_message);
            Message = Encoding.UTF8.GetString(_message);
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Common.Serialization.CopyData(b, offset, MessageIDLen);
            offset += 4;

            Array.Copy(Encoding.UTF8.GetBytes(MessageID), 0, b, offset, MessageIDLen);
            offset += MessageIDLen;

            Common.Serialization.CopyData(b, offset, MessageLen);
            offset += 4;

            Array.Copy(Encoding.UTF8.GetBytes(Message), 0, b, offset, MessageLen);
            offset += MessageLen;

            return offset;
        }

        public void Serialize(Stream s)
        {
            s.Write(Common.Serialization.UInt32(MessageIDLen));
            s.Write(Encoding.UTF8.GetBytes(MessageID));
            s.Write(Common.Serialization.UInt32(MessageLen));
            s.Write(Encoding.UTF8.GetBytes(Message));
        }

        public int Size()
        {
            return 8 + Message.Length + MessageID.Length;
        }
    }
}
