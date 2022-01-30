using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom
{
    public class NodeId
    {
        public BigInteger Value { get; private set; }

        public NodeId()
        {
            Value = new BigInteger(Utility.RandomByteArray(Constants.ID_BIT_LENGTH / 8).Concat(new byte[] { 0 }).ToArray()); // We append a 0 byte, to ensure the BigInteger is a positive value
        }

        public NodeId(BigInteger value)
        {
            Value = value;
        }

        // Only take 'ID_BIT_LENGTH', so we skip the the appended 0, from when we generated the Id
        public byte[] GetBytes() => Value.ToByteArray().Take(Constants.ID_BIT_LENGTH / 8).ToArray();
    }
}
