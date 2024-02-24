using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin.Models;
using Discreet.Common.Serialize;

namespace Discreet.Network.Core.Packets
{
    public class TransactionsPacket: IPacketBody
    {
        public int TxsLen { get => Txs?.Length ?? 0; }
        public FullTransaction[] Txs { get; set; }

        public TransactionsPacket()
        {

        }

        public void Serialize(BEBinaryWriter writer)
        {
            writer.WriteSerializableArray(Txs);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Txs = reader.ReadSerializableArray<FullTransaction>();
        }

        public int Size
        {
            get => 4 + Txs?.Select(x => x.Size).Aggregate(0, (x, y) => x + y) ?? 0;
        }
    }
}
