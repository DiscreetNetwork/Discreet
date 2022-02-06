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
        public uint MessageLen { get; set; }
        public string Message { get; set; }

        public AlertPacket()
        {

        }

        public AlertPacket(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public AlertPacket(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            Checksum = new Cipher.SHA256(b, offset);
            offset += 32;

            Sig = new Cipher.Signature(b, offset);
            offset += 96;

            MessageLen = Coin.Serialization.GetUInt32(b, offset);
            offset += 4;

            Message = Encoding.UTF8.GetString(b, (int)offset, (int)MessageLen);
        }

        public void Deserialize(Stream s)
        {
            byte[] _checksum = new byte[32];
            s.Read(_checksum);
            Checksum = new Cipher.SHA256(_checksum, false);

            byte[] _sig = new byte[96];
            s.Read(_sig);
            Sig = new Cipher.Signature(_sig);

            byte[] _messageLen = new byte[4];
            s.Read(_messageLen);
            MessageLen = Coin.Serialization.GetUInt32(_messageLen, 0);

            byte[] _message = new byte[MessageLen];
            s.Read(_message);
            Message = Encoding.UTF8.GetString(_message);
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Array.Copy(Checksum.Bytes, 0, b, offset, 32);
            offset += 32;

            Array.Copy(Sig.ToBytes(), 0, b, offset, 96);
            offset += 96;

            Coin.Serialization.CopyData(b, offset, MessageLen);
            offset += 4;

            Array.Copy(Encoding.UTF8.GetBytes(Message), 0, b, offset, MessageLen);
            offset += MessageLen;

            return offset;
        }

        public void Serialize(Stream s)
        {
            s.Write(Checksum.Bytes);
            s.Write(Sig.ToBytes());
            s.Write(Coin.Serialization.UInt32(MessageLen));
            s.Write(Encoding.UTF8.GetBytes(Message));
        }

        public int Size()
        {
            return 32 + 96 + 4 + Message.Length;
        }
    }
}
