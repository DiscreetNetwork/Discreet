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
        /// The time in seconds to force a timeout on an inbound connecting peer.
        /// </summary>
        public const int CONNECTING_FORCE_TIMEOUT = 30;

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
        public const int PEER_EXCHANGER_TIMER = 90;

        /// <summary>
        /// How many outbound peers at a time to send a RequestPeers to.
        /// </summary>
        public const int PEER_EXCHANGER_OUTBOUND = 1;

        /// <summary>
        /// How many inbound peers at a time to send a RequestPeers to.
        /// </summary>
        public const int PEER_EXCHANGER_INBOUND = 1;

        /// <summary>
        /// How often to run the heartbeater, in seconds.
        /// </summary>
        public const int PEERBLOOM_HEARTBEATER = 150;

        /// <summary>
        /// When to ping a peer by the heartbeater, in seconds.
        /// </summary>
        public const int PEERBLOOM_HEARTBEATER_TIMEOUT = 60 * 20;

        /// <summary>
        /// Maximum number of feeler connections to use at a time.
        /// </summary>
        public const int PEERBLOOM_MAX_FEELERS = 3;

        /// <summary>
        /// How often to run the Feeler.
        /// </summary>
        public const int PEERBLOOM_FEELER_INTERVAL = 60;

        /// <summary>
        /// Number of buckets to use for tried addresses.
        /// </summary>
        public const int PEERLIST_MAX_TRIED_BUCKETS = 64;

        /// <summary>
        /// Number of buckets to use for new addresses.
        /// </summary>
        public const int PEERLIST_MAX_NEW_BUCKETS = 256;

        /// <summary>
        /// How many items to be stored in a bucket.
        /// </summary>
        public const int PEERLIST_BUCKET_SIZE = 64;

        /// <summary>
        /// How many buckets per group for the tried table.
        /// </summary>
        public const int TRIED_BUCKETS_PER_GROUP = 4;

        /// <summary>
        /// How many buckets per source group for the new table.
        /// </summary>
        public const int NEW_BUCKETS_PER_SOURCE_GROUP = 32;

        /// <summary>
        /// How many days old a peer can maximally be.
        /// </summary>
        public const int PEERLIST_HORIZON_DAYS = 30;

        /// <summary>
        /// How many retries we allow a new peer to undergo until we give up.
        /// </summary>
        public const int PEERLIST_MAX_RETRIES = 3;

        /// <summary>
        /// How many successive failures we allow a peer to undergo in PEERLIST_MIN_FAIL_DAYS until it can be considered for eviction.
        /// </summary>
        public const int PEERLIST_MAX_FAILURES = 5;

        /// <summary>
        /// How many days to allow a peer to fail connection before considering eviction.
        /// </summary>
        public const int PEERLIST_MIN_FAIL_DAYS = 7;

        /// <summary>
        /// How many hours a successful connection should be to consider evicting another tried peer.
        /// </summary>
        public const int PEERLIST_REPLACEMENT_HOURS = 4;

        /// <summary>
        /// How many tried collisions to keep track of resolving at a time.
        /// </summary>
        public const int PEERLIST_MAX_TRIED_COLLISION_SIZE = 10;

        /// <summary>
        /// How many seconds to consider a connection attempt recent.
        /// </summary>
        public const int PEERLIST_RECENT_TRY = 600;

        /// <summary>
        /// How often, in seconds, to test a collision in tried.
        /// </summary>
        public const int PEERLIST_RESOLVE_COLLISION_INTERVAL = 300;

        /// <summary>
        /// How often, in seconds, to save the peerlist to disk.
        /// </summary>
        public const int PEERLIST_SAVE_INTERVAL = 600;
    }
}
