using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discreet.Common.Serialize;

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

        public void Serialize(BEBinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write((uint)Services);
            writer.Write(Timestamp);
            writer.Write(Height);
            writer.Write(Port);
            writer.Write(Syncing);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Version = reader.ReadUInt32();
            Services = (ServicesFlag)reader.ReadUInt32();
            Timestamp = reader.ReadInt64();
            Height = reader.ReadInt64();
            Port = reader.ReadInt32();
            Syncing = reader.ReadBool();
        }

        public int Size => 29;
    }
}
