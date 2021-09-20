using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Discreet.Network.Core
{
    public class Peer
    {
        public PeerState State { get; private set; }
        public IPAddress IP { get; private set; }
        public byte Version { get; internal set; }
        public ushort GossipPort { get; private set; }
        public byte Generation { get; private set; }
        public byte Service { get; private set; }


        public Peer(IPAddress address, ushort port, byte version, PeerState state, ushort gossipPort)
        {
            IP = address;
            GossipPort = port;
            Version = version;

        }

        internal IPEndPoint GossipEndpoint
        {
            get
            {
                return new IPEndPoint(IP, GossipPort);
            }
        }


        public override string ToString()
        {
            return $"Endpoint: {GossipEndpoint.Address}:{GossipEndpoint.Port} Version: {Version}";
        }




    }
}
