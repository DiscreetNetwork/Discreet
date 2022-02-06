using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets.Peerbloom
{
    public class ConnectAck : IPacketBody
    {
        public bool IsPublic { get; set; }
        public bool Acknowledged { get; set; }
        public Cipher.Key ID { get; set; }

        public ConnectAck() { }

        public ConnectAck(Stream s)
        {
            Deserialize(s);
        }

        public ConnectAck(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            IsPublic = b[offset] != 0;
            Acknowledged = b[offset + 1] != 0;

            ID = new Cipher.Key(new byte[32]);
            Array.Copy(b, offset + 2, ID.bytes, 0, 32);
        }

        public void Deserialize(Stream s)
        {
            IsPublic = s.ReadByte() != 0;
            Acknowledged = s.ReadByte() != 0;

            ID = new Cipher.Key(new byte[32]);
            s.Read(ID.bytes);
        }

        public uint Serialize(byte[] b, uint offset)
        {
            b[offset] = (byte)(IsPublic ? 1 : 0);
            b[offset + 1] = (byte)(Acknowledged ? 1 : 0);

            Array.Copy(ID.bytes, 0, b, offset + 2, 32);
            return offset + 34;
        }

        public void Serialize(Stream s)
        {
            s.WriteByte((byte)(IsPublic ? 1 : 0));
            s.WriteByte((byte)(Acknowledged ? 1 : 0));
            s.Write(ID.bytes);
        }

        public int Size()
        {
            return 34;
        }
    }
}
