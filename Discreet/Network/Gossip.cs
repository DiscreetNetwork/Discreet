using Discreet.Network.Core;
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

        public static Int64 MAX_MESSAGE_SIZE = 16000000; // 16 mb max packet size (test)

        // used for netseek for corrupted packets, also used to identify network
        public static byte SPECIAL_BYTE_TESTNET = 0x00;
        public static byte SPECIAL_BYTE_MAINNET = 0x01;

        private TcpClient _tcpClient;
        private readonly Dictionary<IPEndPoint, Peer> _peers = new Dictionary<IPEndPoint, Peer>();
        private readonly Peer _self;

        public bool debug = true;

        public Gossip(ushort port, ushort gossipPort)
        {
            _self = new Peer(IPAddress.Any, port, SPECIAL_BYTE_MAINNET, PeerState.Alive, gossipPort);
        }


        public async Task StartWhisper()
        {
            if (debug) { Console.WriteLine($"Starting Discreet Gossip Network in debug mode."); }
            InitializeTCPClient(_self.GossipEndpoint);
            ListenCallback();


        }

        private TcpClient InitializeTCPClient(EndPoint listenEndPoint)
        {
            var tcpClient = new TcpClient();
            try
            {
                tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                tcpClient.Client.Bind(listenEndPoint);
    
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return tcpClient;
        }

        private async void ListenCallback()
        {
            while (true)
            {
                try
                {
                    // Read TCP bytestream.
                }

                catch (SocketException socEx)
                {
                    Console.WriteLine(socEx.Message);
                    _tcpClient = InitializeTCPClient(_self.GossipEndpoint);
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private async void ReceiveCallback(IAsyncResult result)
        {
           // Handle callback
        }

    }
  
}