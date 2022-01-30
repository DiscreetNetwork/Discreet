using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom
{
    /// <summary>
    /// Responsible for keeping track of received messages by storing their ID
    /// </summary>
    public class MessageStore
    {
        List<string> _messageIdentifiers = new List<string>();

        public void AddMessageIdentifier(string messageIdentifier)
        {
            lock (this)
            {
                if (Contains(messageIdentifier)) return;
                _messageIdentifiers.Add(messageIdentifier);
            }
        }

        public bool Contains(string messageIdentifier)
        {
            return _messageIdentifiers.Contains(messageIdentifier);
        }
    }
}
