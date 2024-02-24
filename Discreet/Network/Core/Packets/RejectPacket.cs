using Discreet.Common.Serialize;
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
        public string Reason { get; set; }              //usually, stores the exception message or error message
        public byte[] Data { get; set; }                //usually used to store the ID or hash of the block or transaction being rejected

        public RejectPacket()
        {

        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.Write((byte)RejectedType);
            writer.Write((byte)Code);
            writer.Write(Reason);
            writer.WriteByteArray(Data);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            RejectedType = (PacketType)reader.ReadUInt8();
            Code = (RejectionCode)reader.ReadUInt8();
            Reason = reader.ReadLengthPrefixedString();
            Data = reader.ReadByteArray();
        }

        public int Size => 10 + Encoding.UTF8.GetByteCount(Reason) + Data?.Length ?? 0;
    }
}
