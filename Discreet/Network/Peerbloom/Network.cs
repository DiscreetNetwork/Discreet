using Discreet.Network.Peerbloom.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace Discreet.Network.Peerbloom
{
    /// <summary>
    /// The root / entrypoint of the network, which hold references to all the required classes, 
    /// that makes the network operate
    /// </summary>
    public class Network
    {
        private static Network _network;

        private static object network_lock = new object();

        public static Network GetNetwork()
        {
            lock (network_lock)
            {
                if (_network == null) Instantiate();

                return _network;
            }
        }

        public static void Instantiate()
        {
            lock (network_lock)
            {
                Daemon.Logger.Debug($"Network.Instantiate: network is being instantiated");
                if (_network == null) _network = new Network(Daemon.DaemonConfig.GetDefault().Endpoint);
            }
        }

        public LocalNode LocalNode;

        public IPAddress ReflectedAddress;

        public void SetReflectedAddress(IPAddress addr) => ReflectedAddress = addr;

        /// <summary>
        /// A store to hold ids of received messages. 
        /// Used determine if we have already seen / received a specific message from the network
        /// </summary>
        private MessageStore _messageStore;

        /// <summary>
        /// Peers that initiated the connection with this node. Contains zero peers for private nodes.
        /// </summary>
        public ConcurrentDictionary<IPEndPoint, Connection> InboundConnectedPeers = new();

        /// <summary>
        /// Peers that this node initiated a connection with.
        /// </summary>
        public ConcurrentDictionary<IPEndPoint, Connection> OutboundConnectedPeers = new();

        /// <summary>
        /// Peers currently in the process of being connected.
        /// </summary>
        public ConcurrentDictionary<IPEndPoint, Connection> ConnectingPeers = new();

        /// <summary>
        /// Peers that are known by this node and aren't currently connected to or by this peer.
        /// </summary>
        public HashSet<IPEndPoint> UnconnectedPeers = new();

        public int MinDesiredConnections;
        public int MaxDesiredConnections;

        public void AddConnecting(Connection conn)
        {
            if (ConnectingPeers.Any(n => n.Key.Equals(conn.Receiver))) return;

            if (ConnectingPeers.Count == Constants.PEERBLOOM_MAX_CONNECTING)
            {
                Daemon.Logger.Warn($"Network.AddConnecting: Currently connecting to max pending peers; dropping new connection with peer {conn.Receiver}");
                /* dispose of connection */
                conn.Dispose();
                return;
            }

            if (!ConnectingPeers.TryAdd(conn.Receiver, conn))
            {
                Daemon.Logger.Error($"Network.AddInboundConnection failed for connection {conn.Receiver}");
            }
        }

        public void AddInboundConnection(Connection conn)
        {
            if (InboundConnectedPeers.Any(n => n.Key.Equals(conn.Receiver))) return;

            if (!ConnectingPeers.ContainsKey(conn.Receiver))
            {
                Daemon.Logger.Error($"Network.AddInboundConnection: currently not connecting with peer {conn.Receiver}!");
                return;
            }

            ConnectingPeers.Remove(conn.Receiver, out _);

            if (InboundConnectedPeers.Count == Constants.PEERBLOOM_MAX_INBOUND_CONNECTIONS)
            {
                Daemon.Logger.Warn($"Network.AddInboundConnection: Currently connected to max inbound peers; dropping new connection with peer {conn.Receiver}");

                /* we still want to add this peer to the list of known peers if it was properly authenticated */
                if (conn.ConnectionAcknowledged)
                {
                    UnconnectedPeers.Add(conn.Receiver);
                }

                /* dispose of connection */
                conn.Dispose();
                return;
            }

            if (!InboundConnectedPeers.TryAdd(conn.Receiver, conn))
            {
                Daemon.Logger.Error($"Network.AddInboundConnection failed for connection {conn.Receiver}");
            }
        }

        public void AddOutboundConnection(Connection conn)
        {
            if (OutboundConnectedPeers.Any(n => n.Key.Equals(conn.Receiver)))
            {
                Daemon.Logger.Warn($"Network.AddOutboundConnection: already connected to peer {conn.Receiver}, ignoring call");
                return;
            }

            if (!ConnectingPeers.ContainsKey(conn.Receiver))
            {
                Daemon.Logger.Error($"Network.AddOutboundConnection: currently not connecting with peer {conn.Receiver}!");
                return;
            }

            ConnectingPeers.Remove(conn.Receiver, out _);

            if (OutboundConnectedPeers.Count == Constants.PEERBLOOM_MAX_OUTBOUND_CONNECTIONS)
            {
                Daemon.Logger.Warn($"Network.AddOutboundConnection: Currently connected to max outbound peers; dropping new connection with peer {conn.Receiver}");
                

                /* we still want to add this peer to the list of known peers if it was properly authenticated */
                if (conn.ConnectionAcknowledged)
                {
                    UnconnectedPeers.Add(conn.Receiver);
                }

                /* dispose of connection */
                conn.Dispose();
                return;
            }

            if (!OutboundConnectedPeers.TryAdd(conn.Receiver, conn))
            {
                Daemon.Logger.Error($"Network.AddOutboundConnection failed for connection {conn.Receiver}");
            }
        }

        public List<IPEndPoint> GetPeers(int maxPeers)
        {
            List<IPEndPoint> remoteNodes = new List<IPEndPoint>(InboundConnectedPeers.Keys.Union(OutboundConnectedPeers.Keys));

            Random random = new Random();

            while (remoteNodes.Count > maxPeers)
            {
                remoteNodes.RemoveAt(random.Next(0, remoteNodes.Count));
            }

            return remoteNodes;
        }

        public Connection GetPeer(IPEndPoint endpoint)
        {
            foreach (var node in InboundConnectedPeers.Values)
            {
                if (node.Receiver.Equals(endpoint))
                {
                    return node;
                }
            }

            return null;
        }

        /// <summary>
        /// Removes and disposes of the specified connection from ConnectingPeers, InboundConnectedPeers, or OutboundConnectedPeers, if present.
        /// </summary>
        /// <param name="node"></param>
        public void RemoveNodeFromPool(Connection node)
        {
            if (node == null) return;

            bool res = false;

            foreach (var _node in ConnectingPeers.Values)
            {
                if (node == _node)
                {
                    res = ConnectingPeers.TryRemove(node.Receiver, out _) || res;
                }
            }

            foreach (var _node in InboundConnectedPeers.Values)
            {
                if (node == _node)
                {
                    res = InboundConnectedPeers.TryRemove(node.Receiver, out _) || res;
                }
            }

            foreach (var _node in OutboundConnectedPeers.Values)
            {
                if (node == _node)
                {
                    res = OutboundConnectedPeers.TryRemove(node.Receiver, out _) || res;
                }
            }

            if (res)
            {
                node.Dispose();
            }
        }

        public Handler handler { get; private set; }

        private Heartbeater heartbeater;
        private PeerExchanger peerExchanger;

        /// <summary>
        /// A common tokenSource to control our loops. 
        /// Can be used to gracefully shut down the application
        /// </summary>
        private CancellationTokenSource _shutdownTokenSource;

        public bool IsBootstrapping { get; private set; } = false;

        public bool IsBootstrap { get; private set; } = false;

        /// <summary>
        /// Default constructor for the network class
        /// </summary>
        public Network(IPEndPoint endpoint)
        {
            LocalNode = new LocalNode(endpoint);
            _messageStore = new MessageStore();
            _shutdownTokenSource = new CancellationTokenSource();
        }

        public void StartHeartbeater()
        {
            heartbeater = new Heartbeater(this);
            _ = Task.Run(() => heartbeater.Heartbeat(_shutdownTokenSource.Token)).ConfigureAwait(false);
        }

        public void StartPeerExchanger()
        {
            peerExchanger = new PeerExchanger(this);
            _ = Task.Run(() => peerExchanger.Exchange(_shutdownTokenSource.Token)).ConfigureAwait(false);
        }

        /// <summary>
        /// Starts the bucket refresh background service &
        /// starts the network receiver
        /// </summary>
        public async Task Start()
        {
            _ = Task.Run(() => StartListener(_shutdownTokenSource.Token));
        }

        public async Task StartListener(CancellationToken token)
        {
            TcpListener listener = new TcpListener(LocalNode.Endpoint);
            listener.Start();
            while (!token.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync();
                Daemon.Logger.Info($"TcpReceiver found a connection to client {client.Client.RemoteEndPoint}");
                if (!IsBootstrapping)
                {
                    var conn = new Connection(client, this, LocalNode, false);
                    _ = Task.Run(() => conn.Connect(true, _shutdownTokenSource.Token), token).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Cancels the tokenSource which should stop all running loops
        /// </summary>
        public void Shutdown()
        {
            _shutdownTokenSource.Cancel();
        }

        public async Task Bootstrap()
        {
            IsBootstrapping = true;
            Connection bootstrapNode = new Connection(new IPEndPoint(Daemon.DaemonConfig.GetDefault().BootstrapNode, 5555), this, LocalNode, true);

            // This check is in case THIS device, is the bootstrap node. In that case, it should not try to bootstrap itself, as it is the first node in the network
            // Eventually this check needs to be modified, when we include multiple bootstrap nodes
            // For now: make sure the int we check against, matches the port of the bootstrap node, in the line above
            if (LocalNode.Endpoint.Port == 5555)
            {
                Daemon.DaemonConfig.GetConfig().IsPublic = true;
                LocalNode.SetPublic();

                Daemon.Logger.Info($"Initiated bootstrap node on port {LocalNode.Endpoint.Port}");

                handler = Handler.Initialize(this);

                if (LocalNode.IsPublic)
                {
                    Handler.GetHandler().SetServices(Core.ServicesFlag.Public);
                }

                handler.Start(_shutdownTokenSource.Token);

                IsBootstrapping = false;

                IsBootstrap = true;

                return;
            }

            Daemon.Logger.Info("Bootstrapping the node...");

            /* this must be awaited to ensure our reflected Address is set. This is used to prevent self-connections. */
            await bootstrapNode.Connect();

            if(!bootstrapNode.ConnectionAcknowledged)
            {
                Daemon.Logger.Warn($"Retrying bootstrap process in {Constants.BOOTSTRAP_RETRY_MILLISECONDS / 1000} seconds..");
                await Task.Delay(Constants.BOOTSTRAP_RETRY_MILLISECONDS);
                //Console.Clear();
                await Bootstrap();
                return;
            }

            var fetchedNodes = await bootstrapNode.RequestPeers(LocalNode.Endpoint); // Perform a self-lookup

            // We failed at establishing a connection to the bootstrap node
            if(fetchedNodes == null)
            {
                Daemon.Logger.Warn($"Failed to contact the bootstrap node with a `RequestPeers` command, retrying bootstrap process in {Constants.BOOTSTRAP_RETRY_MILLISECONDS / 1000} seconds..");
                await Task.Delay(Constants.BOOTSTRAP_RETRY_MILLISECONDS);
                //Console.Clear();
                await Bootstrap();
                return;
            }

            // If we didnt get any peers, dont consider the bootstrap a success
            if(fetchedNodes.Count == 0)
            {
                if (!Daemon.Daemon.DebugMode)
                {
                    Daemon.Logger.Warn($"Received no nodes, retrying bootsrap process in {Constants.BOOTSTRAP_RETRY_MILLISECONDS / 1000} seconds..");
                    await Task.Delay(Constants.BOOTSTRAP_RETRY_MILLISECONDS);
                    //Console.Clear();
                    await Bootstrap();
                    return;
                }
            }

            Daemon.Logger.Info("Connecting to peers...");

            HashSet<IPEndPoint> endpoints = new HashSet<IPEndPoint>(fetchedNodes);

            if (Daemon.Daemon.DebugMode)
            {
                _ = Task.Run(() => bootstrapNode.Persistent(_shutdownTokenSource.Token)).ConfigureAwait(false);
            }

            foreach (var node in endpoints)
            {
                if (GetNode(node) != null) continue;

                Daemon.Logger.Debug($"Connecting to peer {node}");

                /* prevents self connections */
                if (node.Address.Equals(ReflectedAddress))
                {
                    Daemon.Logger.Debug($"Ignoring connection to peer {node}; this one is us");
                    continue;
                }

                _ = Task.Run(() => new Connection(node, this, LocalNode, true).Connect(true, _shutdownTokenSource.Token)).ConfigureAwait(false);
            }

            handler = Handler.Initialize(this);

            if (LocalNode.IsPublic)
            {
                Handler.GetHandler().SetServices(Core.ServicesFlag.Public);
            }

            handler.Start(_shutdownTokenSource.Token);

            IsBootstrapping = false;
            Daemon.Logger.Info("Bootstrap completed.");
        }

        public int Broadcast(Core.Packet packet)
        {
            if (packet.Header.Length + Constants.PEERBLOOM_PACKET_HEADER_SIZE > Constants.MAX_PEERBLOOM_PACKET_SIZE)
            {
                Daemon.Logger.Error($"Network.Broadcast: Broadcasted packet was larger than allowed {Constants.MAX_PEERBLOOM_PACKET_SIZE} bytes.");
                return 0;
            }

            Daemon.Logger.Info($"Discreet.Network.Peerbloom.Network.Broadcast: Broadcasting {packet.Header.Command}");

            int i = 0;

            if (LocalNode.IsPublic)
            {
                foreach (var conn in InboundConnectedPeers.Values)
                {
                    Daemon.Logger.Debug($"Network.Broadcast: broadcasting to peer {conn.Receiver}");
                    conn.Send(packet);
                    i++;
                }
            }

            foreach (var conn in OutboundConnectedPeers.Values)
            {
                Daemon.Logger.Debug($"Network.Broadcast: broadcasting to peer {conn.Receiver}");
                conn.Send(packet);
                i++;
            }

            Daemon.Logger.Debug($"Network.Broadcast: sent to {i} peers.");

            return i;
        }

        public bool Send(IPEndPoint endpoint, Core.Packet packet)
        {
            if (packet.Header.Length + Constants.PEERBLOOM_PACKET_HEADER_SIZE > Constants.MAX_PEERBLOOM_PACKET_SIZE)
            {
                Daemon.Logger.Error($"Network.Send: Sent packet was larger than allowed {Constants.MAX_PEERBLOOM_PACKET_SIZE} bytes.");
                return false;
            }

            Daemon.Logger.Info($"Network.Send: Sending {packet.Header.Command} to {endpoint}");

            bool success = InboundConnectedPeers.TryGetValue(endpoint, out var conn);

            if (!success || conn == null)
            {
                success = OutboundConnectedPeers.TryGetValue(endpoint, out conn);

                if (!success || conn == null)
                {
                    Daemon.Logger.Error($"Network.Send: failed to find a connected peer with endpoint {endpoint}");
                    return false;
                }
            }

            conn.Send(packet);

            return true;
        }

        public bool Send(Connection conn, Core.Packet packet)
        {
            if (packet.Header.Length + Constants.PEERBLOOM_PACKET_HEADER_SIZE > Constants.MAX_PEERBLOOM_PACKET_SIZE)
            {
                Daemon.Logger.Error($"Network.Send: Sent packet was larger than allowed {Constants.MAX_PEERBLOOM_PACKET_SIZE} bytes.");
                return false;
            }

            Daemon.Logger.Info($"Network.Send: Sending {packet.Header.Command} to {conn.Receiver}");

            conn.Send(packet);

            return true;
        }

        public async Task<bool> SendAsync(Connection conn, Core.Packet packet)
        {
            return await conn.SendAsync(packet);
        }

        public bool SendSync(IPEndPoint endpoint, Core.Packet packet)
        {
            return Send(endpoint, packet);
        }

        public Connection GetNode(IPEndPoint endpoint)
        {
            return GetPeer(endpoint);
        }

        public void AddPacketToQueue(Core.Packet p, Connection conn)
        {
            handler.AddPacketToQueue(p, conn);
        }
    }
}
