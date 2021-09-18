using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Discreet.Network
{
    public class Peer
    {
        public IPAddress IP { get; private set; }
        public ushort Port { get; private set; }
        public byte Version { get; internal set; }


        public Peer(IPAddress address, ushort port, byte version)
        {
            this.IP = address;
            this.Port = port;
            this.Version = version;
        }

        internal IPEndPoint GossipEndpoint
        {
            get
            {
                return new IPEndPoint(IP, Port);
            }
        }


        public override string ToString()
        {
            return $"Endpoint: {GossipEndpoint.Address}:{GossipEndpoint.Port} Version: {Version}";
        }




    }
}
