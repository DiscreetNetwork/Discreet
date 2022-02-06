using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public class RejectPacket: IPacketBody
    {
        public PacketType RejectedType { get; set; }    //type of packet resulting in error
        public RejectionCode Code { get; set; }         //code corresponding to the type of error
        public uint ReasonLen { get; set; }
        public string Reason { get; set; }              //usually, stores the exception message or error message
        public uint DataLen { get; set; }
        public byte[] Data { get; set; }                //usually used to store the ID or hash of the block or transaction being rejected

        public RejectPacket()
        {

        }

        public RejectPacket(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public RejectPacket(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            RejectedType = (PacketType)b[offset];
            Code = (RejectionCode)b[offset + 1];
            offset += 2;

            ReasonLen = Coin.Serialization.GetUInt32(b, offset);
            offset += 4;

            Reason = Encoding.UTF8.GetString(b, (int)offset, (int)ReasonLen);
            offset += ReasonLen;

            DataLen = Coin.Serialization.GetUInt32(b, offset);
            offset += 4;

            Data = new byte[DataLen];
            Array.Copy(b, offset, Data, 0, DataLen);
        }

        public void Deserialize(Stream s)
        {
            byte[] uintbuf = new byte[4];

            RejectedType = (PacketType)s.ReadByte();
            Code = (RejectionCode)s.ReadByte();

            s.Read(uintbuf);
            ReasonLen = Coin.Serialization.GetUInt32(uintbuf, 0);

            byte[] _reason = new byte[ReasonLen];
            s.Read(_reason);
            Reason = Encoding.UTF8.GetString(_reason);

            s.Read(uintbuf);
            DataLen = Coin.Serialization.GetUInt32(uintbuf, 0);

            Data = new byte[DataLen];
            s.Read(Data);
        }

        public uint Serialize(byte[] b, uint offset)
        {
            b[offset] = (byte)RejectedType;
            b[offset + 1] = (byte)Code;
            offset += 2;

            Coin.Serialization.CopyData(b, offset, ReasonLen);
            offset += 4;

            Array.Copy(Encoding.UTF8.GetBytes(Reason), 0, b, offset, ReasonLen);
            offset += ReasonLen;

            Coin.Serialization.CopyData(b, offset, DataLen);
            offset += 4;

            Array.Copy(Data, 0, b, offset, DataLen);
            offset += DataLen;

            return offset;
        }

        public void Serialize(Stream s)
        {
            s.WriteByte((byte)RejectedType);
            s.WriteByte((byte)Code);
            s.Write(Coin.Serialization.UInt32(ReasonLen));
            s.Write(Encoding.UTF8.GetBytes(Reason));
            s.Write(Coin.Serialization.UInt32(DataLen));
            s.Write(Data);
        }

        public int Size()
        {
            return 10 + Reason.Length + Data.Length;
        }
    }
}
