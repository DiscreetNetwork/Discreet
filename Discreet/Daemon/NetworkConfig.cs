using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Daemon
{
    public class NetworkConfig
    {
        public int? MaxInboundConnections { get; set; }
        public int? MaxOutboundConnections { get; set; }
        public int? ConnectionReadTimeout { get; set; }
        public int? ConnectionWriteTimeout { get; set; }

        public List<IPEndPoint> Peers { get; set; }

        // DEPRECATED. Present only for backwards-compatibility.
        public int? MinDesiredConnections { get; set; }
        public int? MaxDesiredConnections { get; set; }

        public NetworkConfig()
        {
            MaxInboundConnections = Network.Peerbloom.Constants.PEERBLOOM_MAX_INBOUND_CONNECTIONS;
            MaxOutboundConnections = Network.Peerbloom.Constants.PEERBLOOM_MAX_OUTBOUND_CONNECTIONS;
            ConnectionReadTimeout = Network.Peerbloom.Constants.CONNECTION_READ_TIMEOUT;
            ConnectionWriteTimeout = Network.Peerbloom.Constants.CONNECTION_WRITE_TIMEOUT;
            Peers = new();
        }

        public void ConfigureDefaults()
        {
            MaxInboundConnections ??= Network.Peerbloom.Constants.PEERBLOOM_MAX_INBOUND_CONNECTIONS;
            MaxOutboundConnections ??= Network.Peerbloom.Constants.PEERBLOOM_MAX_OUTBOUND_CONNECTIONS;
            ConnectionReadTimeout ??= Network.Peerbloom.Constants.CONNECTION_READ_TIMEOUT;
            ConnectionWriteTimeout ??= Network.Peerbloom.Constants.CONNECTION_WRITE_TIMEOUT;
            Peers = new();
        }
    }
}
