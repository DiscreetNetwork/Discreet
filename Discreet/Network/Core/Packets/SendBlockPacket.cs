using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public class SendBlockPacket: IPacketBody
    {
        public Coin.Block Block { get; set; }

        /* not serialized with the packet body. used to pass error information onto the handler. */
        public string Error { get; set; }

        public SendBlockPacket()
        {

        }

        public SendBlockPacket(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public SendBlockPacket(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            Block = new Coin.Block();

            try
            {
                Block.Deserialize(b, offset);
            }
            catch (Exception e)
            {
                Error = e.Message;
            }
        }

        public void Deserialize(Stream s)
        {
            using MemoryStream _ms = new MemoryStream();

            byte[] buf = new byte[8192];
            int bytesRead;

            while ((bytesRead = s.Read(buf, 0, buf.Length)) > 0)
            {
                _ms.Write(buf, 0, bytesRead);
            }

            Block = new Coin.Block();

            try
            {
                Block.Deserialize(_ms.ToArray());
            }
            catch (Exception e)
            {
                Error = e.Message;
            }
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Block.Serialize(b, offset);

            return offset + Block.Size();
        }

        public void Serialize(Stream s)
        {
            s.Write(Block.Serialize());
        }

        public int Size()
        {
            return (int)Block.Size();
        }
    }
}
