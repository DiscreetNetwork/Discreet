using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Core.Packets
{
    public class GetTransactionsPacket : IPacketBody
    {
        public uint Count { get; set; }
        public Cipher.SHA256[] Transactions { get; set; }

        public GetTransactionsPacket()
        {

        }

        public GetTransactionsPacket(byte[] b, uint offset)
        {
            Deserialize(b, offset);
        }

        public GetTransactionsPacket(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(byte[] b, uint offset)
        {
            Count = Common.Serialization.GetUInt32(b, offset);
            offset += 4;

            Transactions = new Cipher.SHA256[Count];

            for (int i = 0; i < Count; i++)
            {
                Transactions[i] = new Cipher.SHA256(b, offset);
                offset += 32;
            }
        }

        public void Deserialize(Stream s)
        {
            byte[] uintbuf = new byte[4];

            s.Read(uintbuf);
            Count = Common.Serialization.GetUInt32(uintbuf, 0);

            Transactions = new Cipher.SHA256[Count];

            for (int i = 0; i < Count; i++)
            {
                byte[] hashbuf = new byte[32];

                s.Read(hashbuf);
                Transactions[i] = new Cipher.SHA256(hashbuf, false);
            }
        }

        public uint Serialize(byte[] b, uint offset)
        {
            Common.Serialization.CopyData(b, offset, Count);
            offset += 4;

            foreach (Cipher.SHA256 h in Transactions)
            {
                Array.Copy(h.Bytes, 0, b, offset, 32);
                offset += 32;
            }

            return offset;
        }

        public void Serialize(Stream s)
        {
            s.Write(Common.Serialization.UInt32(Count));

            foreach (Cipher.SHA256 h in Transactions)
            {
                s.Write(h.Bytes);
            }
        }

        public int Size()
        {
            return 4 + 32 * Transactions.Length;
        }
    }
}
