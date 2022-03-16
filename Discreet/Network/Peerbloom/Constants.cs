using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom
{
    public static class Constants
    {

        /// <summary>
        /// Determines how long we should wait, before retrying the bootstrap process, in case of a unsuccessful run.
        /// </summary>
        public const int BOOTSTRAP_RETRY_MILLISECONDS = 5000;

        /// <summary>
        /// The maximum size of any Peerbloom packet. Any packet above this size is discarded.
        /// </summary>
        public const int MAX_PEERBLOOM_PACKET_SIZE = 16 * 1024 * 1024;

        /// <summary>
        /// The size of the Peerbloom message header.
        /// </summary>
        public const int PEERBLOOM_PACKET_HEADER_SIZE = 10;

        /// <summary>
        /// The time in milliseconds to timeout a connect attempt.
        /// </summary>
        public const int CONNECTION_CONNECT_TIMEOUT = 10000;

        /// <summary>
        /// The maximum number of connection tries allowed.
        /// </summary>
        public const int CONNECTION_MAX_CONNECT_ATTEMPTS = 5;

        /// <summary>
        /// The time in milliseconds to timeout a single write operation.
        /// </summary>
        public const int CONNECTION_WRITE_TIMEOUT = 5000;

        /// <summary>
        /// The time in milliseconds to timeout a single read operation.
        /// </summary>
        public const int CONNECTION_READ_TIMEOUT = 10000;

        /// <summary>
        /// The maximum number of outbound connections allowed by the network.
        /// </summary>
        public const int PEERBLOOM_MAX_OUTBOUND_CONNECTIONS = 10;

        /// <summary>
        /// The maximum number of inbound connections allowed by the network.
        /// </summary>
        public const int PEERBLOOM_MAX_INBOUND_CONNECTIONS = 120;

        /// <summary>
        /// The maximum number of peers allowed by the network in the peerlist.
        /// </summary>
        public const int PEERBLOOM_MAX_PEERS = 1000;

        /// <summary>
        /// The maximum number of pending connections allowed by the network.
        /// </summary>
        public const int PEERBLOOM_MAX_CONNECTING = 50;

        /// <summary>
        /// The maximum number of peers to be received during a RequestPeers call.
        /// </summary>
        public const int PEERBLOOM_MAX_PEERS_PER_REQUESTPEERS = 10;

        /// <summary>
        /// How often to run the peer exchanger, in seconds.
        /// </summary>
        public const int PEER_EXCHANGER_TIMER = 120;


        /// <summary>
        /// How many outbound peers at a time to send a RequestPeers to.
        /// </summary>
        public const int PEER_EXCHANGER_OUTBOUND = 2;

        /// <summary>
        /// How many inbound peers at a time to send a RequestPeers to.
        /// </summary>
        public const int PEER_EXCHANGER_INBOUND = 2;

        /// <summary>
        /// How often to run the heartbeater, in seconds.
        /// </summary>
        public const int PEERBLOOM_HEARTBEATER = 300;
    }
}
