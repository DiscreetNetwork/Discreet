using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public class TransactionsPacket: IPacketBody
    {
        public uint TxsLen { get; set; }
        public Coin.FullTransaction[] Txs { get; set; }

        public TransactionsPacket()
        {

        }

        public TransactionsPacket(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public TransactionsPacket(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            TxsLen = Coin.Serialization.GetUInt32(b, offset);
            offset += 4;

            Txs = new Coin.FullTransaction[TxsLen];
            for (int i = 0; i < TxsLen; i++)
            {
                Txs[i] = new Coin.FullTransaction();
                offset += Txs[i].Deserialize(b, offset);
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

            /* currently fall back on other Deserialize until ICoin implements Marshal/Unmarshal with a stream parameter */
            Deserialize(_ms.ToArray(), 0);
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Coin.Serialization.CopyData(b, offset, TxsLen);
            offset += 4;

            for (int i = 0; i < TxsLen; i++)
            {
                Txs[i].Serialize(b, offset);
                offset += Txs[i].Size();
            }

            return offset;
        }

        public void Serialize(Stream s)
        {
            s.Write(Coin.Serialization.UInt32(TxsLen));

            foreach (Coin.FullTransaction tx in Txs)
            {
                s.Write(tx.Serialize());
            }
        }

        public int Size()
        {
            int rv = 4;

            foreach (Coin.FullTransaction tx in Txs)
            {
                rv += (int)tx.Size();
            }

            return rv;
        }
    }
}
