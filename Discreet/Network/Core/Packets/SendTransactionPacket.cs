using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public class SendTransactionPacket: IPacketBody
    {
        public Coin.FullTransaction Tx { get; set; }

        public SendTransactionPacket()
        {

        }

        public SendTransactionPacket(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public SendTransactionPacket(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            Tx = new Coin.FullTransaction();

            Tx.Unmarshal(b, offset);
        }

        public void Deserialize(Stream s)
        {
            using MemoryStream _ms = new MemoryStream();

            byte[] buf = new byte[2048];
            int bytesRead;

            while ((bytesRead = s.Read(buf, 0, buf.Length)) > 0)
            {
                _ms.Write(buf, 0, bytesRead);
            }

            Tx = new Coin.FullTransaction();

            Tx.Unmarshal(_ms.ToArray());
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Tx.Marshal(b, offset);

            return offset + Tx.Size();
        }

        public void Serialize(Stream s)
        {
            s.Write(Tx.Marshal());
        }
    }
}
