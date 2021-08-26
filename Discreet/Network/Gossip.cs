using System;

namespace Discreet.Network
{
    // Preliminary implementation of ESIN (encrypted, scalable, infection-like network)
    public class Gossip
    {

        /* gossip protocol implementation
         * Solve for: optimal peer list size
         * Solve for: optimal packet design
         * ZMQ implementation, out of scope.
         * https://www.cs.cornell.edu/courses/cs6410/2017fa/slides/20-p2p-gossip.pdf
         * Epidiemology based
         * Number of rounds til consistency: O(log n)
         */

        public static Int64 MAX_MESSAGE_SIZE = 4 * 10 ^ 6; // 4 mb max packet size (test)

        // used for netseek for corrupted packets, also used to identify network
        public static byte[] SPECIAL_BYTES_MAINNET = new byte[] {0x0B, 0x0E, 0x0E, 0x0F};






    }
  
}