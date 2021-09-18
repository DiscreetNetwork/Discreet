using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

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

        public static Int64 MAX_MESSAGE_SIZE = 4000000; // 4 mb max packet size (test)

        // used for netseek for corrupted packets, also used to identify network
        public static byte SPECIAL_BYTE_TESTNET = 0x00;
        public static byte SPECIAL_BYTE_MAINNET = 0x01;

        private UdpClient _udpClient;
        private readonly Dictionary<IPEndPoint, Peer> _peers = new Dictionary<IPEndPoint, Peer>();
        private readonly Peer _self;

        public bool debug = true;

        public Gossip(ushort port)
        {
            _self = new Peer(IPAddress.Any, port, SPECIAL_BYTE_MAINNET);
        }


        public async Task StartWhisper()
        {
            if (debug) { Console.WriteLine($"Starting Discreet Gossip Network in debug mode."); }
            InitializeUDPClient(_self.GossipEndpoint);



        }

        private UdpClient InitializeUDPClient(EndPoint listenEndPoint)
        {
            var udpClient = new UdpClient();
            try
            {
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(listenEndPoint);
                udpClient.DontFragment = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return udpClient;
        }

        private async void Listener()
        {
            while (true)
            {
                try
                {
                    var request = await _udpClient.ReceiveAsync().ConfigureAwait(false);
                    var receivedDateTime = DateTime.UtcNow;

                    using (var stream = new MemoryStream(request.Buffer, false))
                    {
                        // Handle incoming packets...
       
                    }
                }

                catch (SocketException socEx)
                {
                    Console.WriteLine(socEx.Message);
                    _udpClient = InitializeUDPClient(_self.GossipEndpoint);
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

    }
  
}