using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public class GetBlocksPacket: IPacketBody
    {
        public uint Count { get; set; }
        public Cipher.SHA256[] Blocks { get; set; }

        public void Deserialize(byte[] b, uint offset)
        {
            Count = Coin.Serialization.GetUInt32(b, offset);
            offset += 4;

            Blocks = new Cipher.SHA256[Count];

            for (int i = 0; i < Count; i++)
            {
                Blocks[i] = new Cipher.SHA256(b, offset);
                offset += 32;
            }
        }

        public void Deserialize(Stream s)
        {
            byte[] uintbuf = new byte[4];

            s.Read(uintbuf);
            Count = Coin.Serialization.GetUInt32(uintbuf, 0);

            Blocks = new Cipher.SHA256[Count];

            for (int i = 0; i < Count; i++)
            {
                byte[] hashbuf = new byte[32];

                s.Read(hashbuf);
                Blocks[i] = new Cipher.SHA256(hashbuf, false);
            }
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Coin.Serialization.CopyData(b, offset, Count);
            offset += 4;

            foreach (Cipher.SHA256 h in Blocks)
            {
                Array.Copy(h.Bytes, 0, b, offset, 32);
                offset += 32;
            }

            return offset;
        }

        public void Serialize(Stream s)
        {
            s.Write(Coin.Serialization.UInt32(Count));

            foreach (Cipher.SHA256 h in Blocks)
            {
                s.Write(h.Bytes);
            }
        }
    }
}
