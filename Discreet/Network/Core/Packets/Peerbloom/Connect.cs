using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets.Peerbloom
{
    public class Connect : IPacketBody
    {
        public Cipher.Key ID { get; set; }
        public int Port { get; set; }

        public Connect() { }

        public Connect(Stream s)
        {
            Deserialize(s);
        }

        public Connect(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            ID = new Cipher.Key(new byte[32]);
            Array.Copy(b, offset, ID.bytes, 0, 32);
            Port = Coin.Serialization.GetInt32(b, offset + 32);
        }

        public void Deserialize(Stream s)
        {
            ID = new Cipher.Key(new byte[32]);
            s.Read(ID.bytes);

            byte[] uintbuf = new byte[4];
            s.Read(uintbuf);
            Port = Coin.Serialization.GetInt32(uintbuf, 0);
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Array.Copy(ID.bytes, 0, b, offset, 32);
            Coin.Serialization.CopyData(b, offset + 32, Port);
            return offset + 36;
        }

        public void Serialize(Stream s)
        {
            s.Write(ID.bytes);
            s.Write(Coin.Serialization.Int32(Port));
        }

        public int Size()
        {
            return 36;
        }
    }
}
