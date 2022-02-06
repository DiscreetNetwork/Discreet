using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets.Peerbloom
{
    public class FindNode : IPacketBody
    {
        public int Port { get; set; }
        public Cipher.Key ID { get; set; }
        public Cipher.Key Dest { get; set; }

        public FindNode() { }

        public FindNode(Stream s)
        {
            Deserialize(s);
        }

        public FindNode(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            
            Port = Coin.Serialization.GetInt32(b, offset);
            ID = new Cipher.Key(new byte[32]);
            Dest = new Cipher.Key(new byte[32]);
            Array.Copy(b, offset + 4, ID.bytes, 0, 32);
            Array.Copy(b, offset + 36, Dest.bytes, 0, 32);
        }

        public void Deserialize(Stream s)
        {
            byte[] uintbuf = new byte[4];
            s.Read(uintbuf);
            Port = Coin.Serialization.GetInt32(uintbuf, 0);

            ID = new Cipher.Key(new byte[32]);
            s.Read(ID.bytes);
            Dest = new Cipher.Key(new byte[32]);
            s.Read(Dest.bytes);
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Coin.Serialization.CopyData(b, offset, Port);
            Array.Copy(ID.bytes, 0, b, offset + 4, 32);
            Array.Copy(Dest.bytes, 0, b, offset + 36, 32);
            
            return offset + 36 + 32;
        }

        public void Serialize(Stream s)
        {
            s.Write(Coin.Serialization.Int32(Port));
            s.Write(ID.bytes);
            s.Write(Dest.bytes);
        }

        public int Size()
        {
            return 36 + 32;
        }
    }
}
