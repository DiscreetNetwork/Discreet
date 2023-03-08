using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets.Peerbloom
{
    public class VersionPacket: IPacketBody
    {
        public uint Version { get; set; }
        public ServicesFlag Services { get; set; }
        public long Timestamp { get; set; }
        public long Height { get; set; }
        public int Port { get; set; } // the port of the peer. Can be different from the connected port.
        public bool Syncing { get; set; }

        public VersionPacket()
        {

        }

        public VersionPacket(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public VersionPacket(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            Version = Common.Serialization.GetUInt32(b, offset);
            offset += 4;

            Services = (ServicesFlag)Common.Serialization.GetUInt32(b, offset);
            offset += 4;

            Timestamp = Common.Serialization.GetInt64(b, offset);
            offset += 8;

            Height = Common.Serialization.GetInt64(b, offset);
            offset += 8;

            Port = Common.Serialization.GetInt32(b, offset);
            offset += 4;

            Syncing = b[offset] != 0;
        }

        public void Deserialize(Stream s)
        {
            byte[] uintbuf = new byte[4];
            byte[] longbuf = new byte[8];

            s.Read(uintbuf);
            Version = Common.Serialization.GetUInt32(uintbuf, 0);

            s.Read(uintbuf);
            Services = (ServicesFlag)Common.Serialization.GetUInt32(uintbuf, 0);

            s.Read(longbuf);
            Timestamp = Common.Serialization.GetInt64(longbuf, 0);

            s.Read(longbuf);
            Height = Common.Serialization.GetInt64(longbuf, 0);

            Port = Common.Serialization.GetInt32(s);

            Syncing = s.ReadByte() != 0;
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Common.Serialization.CopyData(b, offset, Version);
            offset += 4;

            Common.Serialization.CopyData(b, offset, (uint)Services);
            offset += 4;

            Common.Serialization.CopyData(b, offset, Timestamp);
            offset += 8;

            Common.Serialization.CopyData(b, offset, Height);
            offset += 8;

            Common.Serialization.CopyData(b, offset, Port);
            offset += 4;

            b[offset] = Syncing ? (byte)1 : (byte)0;
            return offset + 1;
        }

        public void Serialize(Stream s)
        {
            s.Write(Common.Serialization.UInt32(Version));
            s.Write(Common.Serialization.UInt32((uint)Services));
            s.Write(Common.Serialization.Int64(Timestamp));
            s.Write(Common.Serialization.Int64(Height));
            Common.Serialization.CopyData(s, Port);
            s.WriteByte(Syncing ? (byte)1 : (byte)0);
        }

        public int Size()
        {
            return 29;
        }
    }
}
