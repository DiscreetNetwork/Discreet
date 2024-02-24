using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Discreet.Common.Serialize;

namespace Discreet.Network.Core
{
    public class PacketHeader : ISerializable
    {
        public byte NetworkID { get; set; }
        public PacketType Command { get; set; }
        public uint Length { get; set; }
        public uint Checksum { get; set; }

        ///<summary>returns the size of the packet header, in bytes. </summary>
        public int Size => 10;

        public PacketHeader() { }

        public PacketHeader(byte[] b)
        {
            this.Deserialize(b);
        }

        public PacketHeader(byte[] b, int offset)
        {
            this.Deserialize(b, offset);
        }

        public PacketHeader(PacketType type, IPacketBody body)
        {
            NetworkID = Daemon.DaemonConfig.GetConfig().NetworkID.Value;
            Command = type;
            Length = (uint)body.Size;
            Checksum = body.Checksum();
        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.Write(NetworkID);
            writer.Write((byte)Command);
            writer.Write(Length);
            writer.Write(Checksum);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            NetworkID = reader.ReadUInt8();
            Command = (PacketType)reader.ReadUInt8();
            Length = reader.ReadUInt32();
            Checksum = reader.ReadUInt32();
        }
    }
}
