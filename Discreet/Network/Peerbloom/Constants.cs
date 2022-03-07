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
        /// Determines the max size of a nodes individual bucket
        /// A lower value initally creates faster stability in a new / small network, as bucket splits occur sooner
        /// A higher value will increase the time it takes for the network to stabilize, thus at the birth of the network, messages will have a higher chance of not reaching all nodes, because fewer bucket splits have occured
        /// </summary>
        public const int BUCKET_LENGTH = 20;

        /// <summary>
        /// Determines how aggressive a node is at splitting its buckets
        /// A lower value results in less overall bucket splits, which leads to less stability (less overlapping nodes) and might result in messages not reaching some nodes
        /// A higher value results in more overall bucket splits, which leads to more stability, and will produce more duplicate messages across the network / more noise, bandwith
        /// </summary>
        public const int ALPHA = 5;

        public const int ID_BIT_LENGTH = 256;

        /// <summary>
        /// Determines how long we should wait, before retrying the bootstrap process, in case of a unsuccessful run
        /// </summary>
        public const int BOOTSTRAP_RETRY_MILLISECONDS = 5000;


        /// <summary>
        /// Determines how long we should wait based on a buckets 'LastUpdated' property, untill we run another bucket refresh
        /// </summary>
        public const int BUCKET_REFRESH_TIME_UNTILL_UPDATE = 1000 * 10;

        /// <summary>
        /// Determines how long we should wait, before iterating all buckets again
        /// </summary>
        public const int BUCKET_REFRESH_LOOP_DELAY_TIME_MILLISECONDS = 5000;


        /// <summary>
        /// The maximum size of any Peerbloom packet. Any packet above this size is discarded.
        /// </summary>
        public const int MAX_PEERBLOOM_PACKET_SIZE = 16 * 1024 * 1024;
    }
}
