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
        public Cipher.Key Value { get; private set; }

        public NodeId()
        {
            Value = new Cipher.Key(Utility.RandomByteArray(Constants.ID_BIT_LENGTH / 8).ToArray());
        }

        public NodeId(Cipher.Key value)
        {
            Value = value;
        }

        // Only take 'ID_BIT_LENGTH', so we skip the the appended 0, from when we generated the Id
        public byte[] GetBytes() => Value.bytes;
    }
}
