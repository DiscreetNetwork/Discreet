using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin.Models;
using Discreet.Common.Serialize;
using Aurem.Units;

namespace Discreet.Network.Core.Packets
{
    public class SendUnitPacket: IPacketBody
    {
        // public FullTransaction Tx { get; set; }
        public Aurem.Units.Unit U { get; set; }

        /* not serialized with the packet body. used to pass error information onto the handler. */
        public string Error { get; set; }

        public SendUnitPacket()
        {

        }

        public void Serialize(BEBinaryWriter writer)
        {
            U.Serialize((Discreet.Common.Serialize.BEBinaryWriter) writer);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            try
            {
                U = reader.ReadSerializable<Aurem.Units.Unit>();
            }
            catch (Exception e)
            {
                Error = e.Message;
            }
        }

        public int Size => (int)U.GetSize();
    }
}
