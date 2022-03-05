using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public class BlocksPacket: IPacketBody
    {
        public uint BlocksLen { get; set; }
        public Coin.Block[] Blocks { get; set; }

        public BlocksPacket()
        {

        }

        public BlocksPacket(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public BlocksPacket(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            BlocksLen = Coin.Serialization.GetUInt32(b, offset);
            offset += 4;

            Blocks = new Coin.Block[BlocksLen];
            for (int i = 0; i < BlocksLen; i++)
            {
                Blocks[i] = new Coin.Block();
                offset += Blocks[i].Deserialize(b, offset);
            }
        }

        public void Deserialize(Stream s)
        {
            using MemoryStream _ms = new MemoryStream();

            byte[] buf = new byte[4096];
            int bytesRead;

            while ((bytesRead = s.Read(buf, 0, buf.Length)) > 0)
            {
                _ms.Write(buf, 0, bytesRead);
            }

            Deserialize(_ms.ToArray(), 0);
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Coin.Serialization.CopyData(b, offset, BlocksLen);
            offset += 4;

            for (int i = 0; i < BlocksLen; i++)
            {
                Blocks[i].Serialize(b, offset);
                offset += Blocks[i].Size();
            }

            return offset;
        }

        public void Serialize(Stream s)
        {
            s.Write(Coin.Serialization.UInt32(BlocksLen));

            foreach (Coin.Block block in Blocks)
            {
                s.Write(block.Serialize());
            }
        }

        public int Size()
        {
            int rv = 4;

            foreach(Coin.Block block in Blocks)
            {
                rv += (int)block.Size();
            }

            return rv;
        }
    }
}
