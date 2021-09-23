using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Discreet.Network.Core
{
   public class PeerOptions
    {
        public int MAX_MESSAGE_SIZE { get; set; } = 16000000; // 16 mb max packet size (test)
        private int _protocolPeriodMilliseconds = 500;
        private int _ackTimeoutMilliseconds = 250;
        private int _deadTimeoutMilliseconds = 5000;
        public int ProtocolPeriodMilliseconds
        {
            get
            {
                return _protocolPeriodMilliseconds;
            }
            set
            {
                _protocolPeriodMilliseconds = value;
                _ackTimeoutMilliseconds = value / 2;
                _deadTimeoutMilliseconds = value * 10;
            }
        }
        public int AckTimeoutMilliseconds { get { return _ackTimeoutMilliseconds; } }
        public int DeadTimeoutMilliseconds { get { return _deadTimeoutMilliseconds; } }
        public int DeadCoolOffMilliseconds { get; set; } = 300000;
        public int PruneTimeoutMilliseconds { get; set; } = 600000;

        // TODO[Frederik]: Adaptive fanouts: https://www.comp.nus.edu.sg/~ooiwt/papers/fanout-icdcs05-final.pdf
        public int FanoutFactor { get; set; } = 3; 
        public int NumberOfIndirectEndpoints { get; set; } = 3;
        public IPEndPoint[] SeedMembers { get; set; } = new IPEndPoint[0];
        public IEnumerable<IPeerListener> MemberListeners { get; set; } = Enumerable.Empty<IPeerListener>();


        public int CalculateFanout()
        {
            throw new NotImplementedException("Method not yet implemented.");
        }
    }
}
