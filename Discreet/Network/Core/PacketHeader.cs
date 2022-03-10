using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Discreet.Network.Core
{
    public class PacketHeader
    {
        public byte NetworkID { get; set; }
        public PacketType Command { get; set; }
        public uint Length { get; set; }
        public uint Checksum { get; set; }

        ///<summary>returns the size of the packet header, in bytes. </summary>
        public static uint Size()
        {
            return 10;
        }

        public PacketHeader() { }

        public PacketHeader(Stream s)
        {
            Decode(s);
        }

        public PacketHeader(byte[] b)
        {
            Decode(b, 0);
        }

        public PacketHeader(byte[] b, uint offset)
        {
            Decode(b, offset);
        }

        public PacketHeader(PacketType type, IPacketBody body)
        {
            NetworkID = Daemon.DaemonConfig.GetDefault().NetworkID;
            Command = type;
            Length = (uint)body.Size();
            Checksum = body.Checksum();
        }

        public void Encode(Stream s)
        {
            s.WriteByte(NetworkID);
            s.WriteByte((byte)Command);
            s.Write(Coin.Serialization.UInt32(Length));
            s.Write(Coin.Serialization.UInt32(Checksum));
        }

        public void Encode(byte[] buf, uint offset)
        {
            buf[offset] = NetworkID;
            buf[offset + 1] = (byte)Command;

            Coin.Serialization.CopyData(buf, offset + 2, Length);
            Coin.Serialization.CopyData(buf, offset + 6, Checksum);
        }

        public void Decode(Stream s)
        {
            NetworkID = (byte)s.ReadByte();
            Command = (PacketType)s.ReadByte();

            byte[] uintbuf = new byte[4];

            s.Read(uintbuf);
            Length = Coin.Serialization.GetUInt32(uintbuf, 0);

            s.Read(uintbuf);
            Checksum = Coin.Serialization.GetUInt32(uintbuf, 0);
        }

        public void Decode(byte[] buf, uint offset)
        {
            NetworkID = buf[offset];
            Command = (PacketType)buf[offset + 1];

            Length = Coin.Serialization.GetUInt32(buf, offset + 2);
            Checksum = Coin.Serialization.GetUInt32(buf, offset + 6);
        }
    }
}
