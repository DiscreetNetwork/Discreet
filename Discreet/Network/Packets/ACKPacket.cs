using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Packets
{
    public class AckMessage : WireMessage

    {
        public AckMessage(int sequenceNumber)
        {
            SequenceNumber = sequenceNumber;
        }

        public int SequenceNumber { get; private set; }

        public override WireMessageType Type
        {
            get { return WireMessageType.Ack; }
        }

  
    }
}