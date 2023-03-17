using Discreet.RPC.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.RPC.Endpoints
{
    /// <summary>
    /// Contains status endpoints for the RPC API. Status endpoints are used to get information about the node in relation to the network.
    /// </summary>
    public static class StatusEndpoints
    {
        public class VersionRV
        {
            public string Build { get; set; }
            public int Version { get; set; }
        }

        [RPCEndpoint("get_version", APISet.STATUS)]
        public static object GetVersion()
        {
            try
            {
                return new VersionRV { Build = "testnet-win64", Version = 0 };
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetVersion failed: {ex.Message}", ex);

                return new RPCError($"Could not get version data");
            }
        }

        public class Blockchain
        {
            public Readable.BlockHeader Head { get; set; }
            public int TxPoolSize { get; set; }
            public TimeSpan TimeSinceLastBlock { get; set; }
        }

        public class GetHealthRV
        {
            public Blockchain Blockchain { get; set; }
            public Network.Core.Packets.Peerbloom.VersionPacket Version { get; set; }
            public TimeSpan Uptime { get; set; }
            public uint InboundConnections { get; set; }
            public uint OutboundConnections { get; set; }
            public uint Connecting { get; set; }
            public uint Wallets { get; set; }
            public Network.PeerState PeerState { get; set; }
        }

        [RPCEndpoint("get_health", APISet.STATUS)]
        public static object GetHealth()
        {
            try
            {
                var _daemon = Network.Handler.GetHandler().daemon;
                var _handler = Network.Handler.GetHandler();
                var _network = Network.Peerbloom.Network.GetNetwork();
                var dataView = DB.DataView.GetView();
                var _txpool = Daemon.TXPool.GetTXPool();

                var _up = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - _daemon.Uptime);
                var _in = _network.InboundConnectedPeers.Count;
                var _out = _network.OutboundConnectedPeers.Count;
                var _con = _network.ConnectingPeers.Count;
                var _wal = WalletsLegacy.WalletManager.Instance.Wallets.Count;
                var _state = _handler.State;
                var _ver = _handler.MakeVersionPacket();
                var _head = (Readable.BlockHeader)dataView.GetBlockHeader(dataView.GetChainHeight()).ToReadable();
                var _tsz = _txpool.GetTransactions().Count;
                var _blk = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - (long)_head.Timestamp);

                return new GetHealthRV
                {
                    Blockchain = new Blockchain
                    {
                        Head = _head,
                        TxPoolSize = _tsz,
                        TimeSinceLastBlock = _blk
                    },
                    Version = _ver,
                    Uptime = _up,
                    InboundConnections = (uint)_in,
                    OutboundConnections = (uint)_out,
                    Connecting = (uint)_con,
                    Wallets = (uint)_wal,
                    PeerState = _state
                };
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetHealth failed: {ex.Message}", ex);

                return new RPCError($"Could not get health data");
            }
        }

        public class Connection
        {
            public IPEndPoint Receiver { get; set; }
            public int Port { get; set; }
            public TimeSpan Uptime { get; set; }
            public DateTime LastValidSend { get; set; }
            public DateTime LastValidReceive { get; set; }
            public bool Connecting { get; set; }
            public bool IsOutbound { get; set; }
            public bool Acknowledged { get; set; }
        }

        public class ConnectionsRV
        {
            public int NumConnections { get; set; }
            public List<Connection> Connections { get; set; }
        }

        [RPCEndpoint("get_connections", APISet.STATUS)]
        public static object GetConnections()
        {
            try
            {
                var _network = Network.Peerbloom.Network.GetNetwork();

                List<Connection> connections = new List<Connection>();

                foreach (var conn in _network.OutboundConnectedPeers.Values)
                {
                    connections.Add(new Connection
                    {
                        Receiver = conn.Receiver,
                        Port = conn.Port,
                        Uptime = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - conn.TimeStarted),
                        LastValidSend = new DateTime(conn.LastValidSend).ToLocalTime(),
                        LastValidReceive = new DateTime(conn.LastValidReceive).ToLocalTime(),
                        Connecting = false,
                        IsOutbound = conn.IsOutbound,
                        Acknowledged = conn.ConnectionAcknowledged
                    });
                }

                foreach (var conn in _network.InboundConnectedPeers.Values)
                {
                    connections.Add(new Connection
                    {
                        Receiver = conn.Receiver,
                        Port = conn.Port,
                        Uptime = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - conn.TimeStarted),
                        LastValidSend = new DateTime(conn.LastValidSend).ToLocalTime(),
                        LastValidReceive = new DateTime(conn.LastValidReceive).ToLocalTime(),
                        Connecting = false,
                        IsOutbound = conn.IsOutbound,
                        Acknowledged = conn.ConnectionAcknowledged
                    });
                }

                foreach (var conn in _network.ConnectingPeers.Values)
                {
                    connections.Add(new Connection
                    {
                        Receiver = conn.Receiver,
                        Port = conn.Port,
                        Uptime = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - conn.TimeStarted),
                        LastValidSend = new DateTime(conn.LastValidSend).ToLocalTime(),
                        LastValidReceive = new DateTime(conn.LastValidReceive).ToLocalTime(),
                        Connecting = true,
                        IsOutbound = conn.IsOutbound,
                        Acknowledged = conn.ConnectionAcknowledged
                    });
                }

                return new ConnectionsRV
                {
                    NumConnections = connections.Count,
                    Connections = connections
                };
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetConnections failed: {ex.Message}", ex);

                return new RPCError($"Could not get connections");
            }
        }

        [RPCEndpoint("get_num_connections", APISet.STATUS)]
        public static object GetNumConnections()
        {
            try
            {
                var _network = Network.Peerbloom.Network.GetNetwork();

                return _network.InboundConnectedPeers.Count + _network.OutboundConnectedPeers.Count + _network.ConnectingPeers.Count;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetNumConnections failed: {ex.Message}", ex);

                return new RPCError($"Could not get num connections");
            }
        }

        [RPCEndpoint("get_outbound", APISet.STATUS)]
        public static object GetOutbound()
        {
            try
            {
                var _network = Network.Peerbloom.Network.GetNetwork();

                List<Connection> connections = new List<Connection>();

                foreach (var conn in _network.OutboundConnectedPeers.Values)
                {
                    connections.Add(new Connection
                    {
                        Receiver = conn.Receiver,
                        Port = conn.Port,
                        Uptime = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - conn.TimeStarted),
                        LastValidSend = new DateTime(conn.LastValidSend).ToLocalTime(),
                        LastValidReceive = new DateTime(conn.LastValidReceive).ToLocalTime(),
                        Connecting = false,
                        IsOutbound = conn.IsOutbound,
                        Acknowledged = conn.ConnectionAcknowledged
                    });
                }

                return new ConnectionsRV
                {
                    NumConnections = connections.Count,
                    Connections = connections
                };
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetOutbound failed: {ex.Message}", ex);

                return new RPCError($"Could not get outbound connections");
            }
        }

        [RPCEndpoint("get_inbound", APISet.STATUS)]
        public static object GetInbound()
        {
            try
            {
                var _network = Network.Peerbloom.Network.GetNetwork();

                List<Connection> connections = new List<Connection>();

                foreach (var conn in _network.InboundConnectedPeers.Values)
                {
                    connections.Add(new Connection
                    {
                        Receiver = conn.Receiver,
                        Port = conn.Port,
                        Uptime = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - conn.TimeStarted),
                        LastValidSend = new DateTime(conn.LastValidSend).ToLocalTime(),
                        LastValidReceive = new DateTime(conn.LastValidReceive).ToLocalTime(),
                        Connecting = false,
                        IsOutbound = conn.IsOutbound,
                        Acknowledged = conn.ConnectionAcknowledged
                    });
                }

                return new ConnectionsRV
                {
                    NumConnections = connections.Count,
                    Connections = connections
                };
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetInbound failed: {ex.Message}", ex);

                return new RPCError($"Could not get inbound connections");
            }
        }

        [RPCEndpoint("get_connecting", APISet.STATUS)]
        public static object GetConnecting()
        {
            try
            {
                var _network = Network.Peerbloom.Network.GetNetwork();

                List<Connection> connections = new List<Connection>();

                foreach (var conn in _network.ConnectingPeers.Values)
                {
                    connections.Add(new Connection
                    {
                        Receiver = conn.Receiver,
                        Port = conn.Port,
                        Uptime = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - conn.TimeStarted),
                        LastValidSend = new DateTime(conn.LastValidSend).ToLocalTime(),
                        LastValidReceive = new DateTime(conn.LastValidReceive).ToLocalTime(),
                        Connecting = true,
                        IsOutbound = conn.IsOutbound,
                        Acknowledged = conn.ConnectionAcknowledged
                    });
                }

                return new ConnectionsRV
                {
                    NumConnections = connections.Count,
                    Connections = connections
                };
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"RPC call to GetConnecting failed: {ex.Message}", ex);

                return new RPCError($"Could not get connecting connections");
            }
        }
    }
}
