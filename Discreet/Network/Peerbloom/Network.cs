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
                if (_network == null) _network = new Network(Daemon.DaemonConfig.GetConfig().Endpoint);
            }
        }

        public LocalNode LocalNode;

        public IPAddress ReflectedAddress;

        public void SetReflectedAddress(IPAddress addr) => ReflectedAddress = addr;

        /// <summary>
        /// A store to hold ids of received messages. 
        /// Used determine if we have already seen / received a specific message from the network
        /// </summary>
        public MessageCache Cache;

        /// <summary>
        /// Peers that initiated the connection with this node. Contains zero peers for private nodes.
        /// </summary>
        public ConcurrentDictionary<IPEndPoint, Connection> InboundConnectedPeers { get; set; } = new();

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

        /// <summary>
        /// Used to test connections.
        /// </summary>
        public ConcurrentDictionary<IPEndPoint, Connection> Feelers = new();

        public int MinDesiredConnections;
        public int MaxDesiredConnections;

        public Connection GetPeerByAddress(IPAddress addr)
        {
            foreach (var p in InboundConnectedPeers)
            {
                if (p.Key.Address.Equals(addr)) return p.Value;
            }

            foreach (var p in OutboundConnectedPeers)
            {
                if (p.Key.Address.Equals(addr)) return p.Value;
            }

            foreach (var p in ConnectingPeers)
            {
                if (p.Key.Address.Equals(addr)) return p.Value;
            }

            return null;
        }

        public void AddConnecting(Connection conn)
        {
            if (ConnectingPeers.Any(n => n.Key.Equals(conn.Receiver))) return;

            if (ConnectingPeers.Count == Constants.PEERBLOOM_MAX_CONNECTING)
            {
                Daemon.Logger.Warn($"Network.AddConnecting: Currently connecting to max pending peers; dropping new connection with peer {conn.Receiver}");
                /* dispose of connection */
                Task.Run(() => conn.Disconnect(true, Core.Packets.Peerbloom.DisconnectCode.MAX_CONNECTING_PEERS));
                return;
            }

            if (!ConnectingPeers.TryAdd(conn.Receiver, conn))
            {
                Daemon.Logger.Error($"Network.AddConnecting: failed for connection {conn.Receiver}");
            }
        }

        public void AddFeeler(Connection conn)
        {
            if (Feelers.Any(n => n.Key.Equals(conn.Receiver))) return;

            if (Feelers.Count == Constants.PEERBLOOM_MAX_FEELERS)
            {
                Daemon.Logger.Warn($"Network.AddFeeler: Currently testing maximum number of feelers; dropping connection to feeler {conn.Receiver}");
                Task.Run(() => conn.Disconnect(true, Core.Packets.Peerbloom.DisconnectCode.MAX_FEELER_PEERS));
                return;
            }

            if (!Feelers.TryAdd(conn.Receiver, conn))
            {
                Daemon.Logger.Error($"Network.AddFeeler: failed for connection {conn.Receiver}");
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
                Task.Run(() => conn.Disconnect(true, Core.Packets.Peerbloom.DisconnectCode.MAX_INBOUND_PEERS));
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
                Task.Run(() => conn.Disconnect(true, Core.Packets.Peerbloom.DisconnectCode.MAX_OUTBOUND_PEERS));
                return;
            }

            if (!OutboundConnectedPeers.TryAdd(conn.Receiver, conn))
            {
                Daemon.Logger.Error($"Network.AddOutboundConnection failed for connection {conn.Receiver}");
            }
        }

        public List<IPEndPoint> GetPeers(int maxPeers)
        {
            return peerlist.GetAddr(maxPeers, 0);
        }

        public Connection GetPeer(IPEndPoint endpoint, bool excludeConnecting = false)
        {
            foreach (var node in InboundConnectedPeers.Values)
            {
                if (node.Receiver.Equals(endpoint))
                {
                    return node;
                }
            }

            foreach (var node in OutboundConnectedPeers.Values)
            {
                if (node.Receiver.Equals(endpoint))
                {
                    return node;
                }
            }

            if (!excludeConnecting)
            {
                foreach (var node in ConnectingPeers.Values)
                {
                    if (node.Receiver.Equals(endpoint))
                    {
                        return node;
                    }
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
        private Feeler feeler;
        public Peerlist peerlist;
        public IncomingTester IncomingTester;

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
            Cache = MessageCache.GetMessageCache();
            _shutdownTokenSource = new CancellationTokenSource();
            peerlist = new Peerlist();
            feeler = new Feeler(this, peerlist);
            IncomingTester = new IncomingTester(this, peerlist);
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
            peerlist.Start(feeler, _shutdownTokenSource.Token);
            _ = Task.Run(() => feeler.Start(_shutdownTokenSource.Token));
        }

        public async Task StartListener(CancellationToken token)
        {
            TcpListener listener = new TcpListener(LocalNode.Endpoint);
            listener.Start();
            _ = Task.Run(() => IncomingTester.Start(token)).ConfigureAwait(false);
            while (!token.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(token);
                Daemon.Logger.Info($"TcpReceiver found a connection to client {client.Client.RemoteEndPoint}");
                if (!IsBootstrapping)
                {
                    // check if we're already connected to a node at this endpoint
                    if (GetPeer((IPEndPoint)client.Client.RemoteEndPoint) != null)
                    {
                        Daemon.Logger.Warn($"Network.TcpListener: already connected to a peer at this endpoint ({client.Client.RemoteEndPoint}); dropping connection");
                        client.Dispose();
                    }
                    else
                    {
                        var conn = new Connection(client, this, LocalNode, false);
                        _ = Task.Run(() => conn.Connect(true, _shutdownTokenSource.Token), token).ConfigureAwait(false);
                    }
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
            Connection bootstrapNode = new Connection(new IPEndPoint(Daemon.DaemonConfig.GetConfig().BootstrapNode, 5555), this, LocalNode, true);

            handler = Handler.Initialize(this);

            if (LocalNode.IsPublic)
            {
                Handler.GetHandler().SetServices(Core.ServicesFlag.Public);
            }

            handler.Start(_shutdownTokenSource.Token);

            // This check is in case THIS device, is the bootstrap node. In that case, it should not try to bootstrap itself, as it is the first node in the network
            // Eventually this check needs to be modified, when we include multiple bootstrap nodes
            // For now: make sure the int we check against, matches the port of the bootstrap node, in the line above
            if (LocalNode.Endpoint.Port == 5555)
            {
                Daemon.DaemonConfig.GetConfig().IsPublic = true;
                LocalNode.SetPublic();

                Daemon.Logger.Info($"Initiated bootstrap node on port {LocalNode.Endpoint.Port}");

                IsBootstrapping = false;

                IsBootstrap = true;

                _ = Task.Run(() => RunNetwork()).ConfigureAwait(false);

                return;
            }

            int NumberConnections = (Daemon.Daemon.DebugMode ? 1 : 2);

            if (peerlist.NumNew + peerlist.NumTried > 0)
            {
                Daemon.Logger.Info("Attempting to connect to known peers...");

                List<Peer> checkedPeers = new List<Peer>();
                Peer peer;
                int timeoutLength = 5000;
                int numAttempts = 1;
                do
                {
                    (peer, _) = peerlist.Select(false, true);

                    if (checkedPeers.Contains(peer)) continue;

                    Connection conn = new Connection(peer.Endpoint, this, LocalNode, true);

                    bool success = await conn.Connect(true, _shutdownTokenSource.Token, false, timeoutLength, numAttempts);
                    peerlist.Attempt(peer.Endpoint, !success);

                    checkedPeers.Add(peer);
                    if (peerlist.NumTried == checkedPeers.Count && !success)
                    {
                        Daemon.Logger.Warn("Could not find any online/valid peers. Increasing timeout length and allowed attempts.");
                        timeoutLength += 5000;
                        numAttempts++;
                        checkedPeers.Clear();
                    }
                }
                while (peer != null && OutboundConnectedPeers.Count < NumberConnections && checkedPeers.Count < peerlist.NumTried + peerlist.NumNew);
            }

            if (OutboundConnectedPeers.Count > 0)
            {
                Daemon.Logger.Info($"Successfully connected to {OutboundConnectedPeers.Count} peers");
                _ = Task.Run(() => RunNetwork()).ConfigureAwait(false);
                IsBootstrapping = false;
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
                peerlist.Create(bootstrapNode.Receiver, bootstrapNode.Receiver, out _);
                peerlist.Good(bootstrapNode.Receiver, false);
                _ = Task.Run(() => bootstrapNode.Persistent(_shutdownTokenSource.Token)).ConfigureAwait(false);
            }

            int connectCount = 0;

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

                peerlist.AddNew(node, bootstrapNode.Receiver, 0);
            }

            // select at least two nodes to connect to
            if (!Daemon.Daemon.DebugMode)
            {
                List<IPEndPoint> checkedPeers = new List<IPEndPoint>();
                int numConnected = 0;

                foreach (var node in endpoints)
                {
                    if (numConnected >= 2)
                    {
                        break;
                    }

                    if (node.Address.Equals(ReflectedAddress))
                    {
                        Daemon.Logger.Debug($"Ignoring connection to peer {node}; this one is us");
                        continue;
                    }

                    if (checkedPeers.Contains(node)) continue;

                    Connection conn = new Connection(node, this, LocalNode, true);

                    bool success = await conn.Connect(true, _shutdownTokenSource.Token, false, 5000, 2);
                    peerlist.Attempt(node, !success);

                    if (success) numConnected++;

                    checkedPeers.Add(node);
                    if (numConnected == 0 && endpoints.Count == checkedPeers.Count && !success)
                    {
                        Daemon.Logger.Warn("Could not find any online/valid peers. Restarting bootstrap.");
                        await Bootstrap();
                        return;
                    }
                }
            }
            

            IsBootstrapping = false;
            Daemon.Logger.Info("Bootstrap completed.");
            _ = Task.Run(() => RunNetwork()).ConfigureAwait(false);

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

        public bool SendRequest(Connection conn, Core.Packet packet, long durationMilliseconds = 0, Action<IPEndPoint, Core.Packets.InventoryVector, bool> callback = null)
        {
            if (packet.Header.Length + Constants.PEERBLOOM_PACKET_HEADER_SIZE > Constants.MAX_PEERBLOOM_PACKET_SIZE)
            {
                Daemon.Logger.Error($"Network.SendRequest: Sent packet was larger than allowed {Constants.MAX_PEERBLOOM_PACKET_SIZE} bytes.");
                return false;
            }

            Daemon.Logger.Info($"Network.SendRequest: Sending request {packet.Header.Command} to {conn.Receiver}");

            // hacky; make specific functions for sending packets which call this instead (in the future)
            if (packet.Header.Command == Core.PacketType.GETBLOCKS)
            {
                handler.RegisterNeeded((Core.Packets.GetBlocksPacket)packet.Body, conn.Receiver, durationMilliseconds, callback);
            }
            else if (packet.Header.Command == Core.PacketType.GETTXS)
            {
                handler.RegisterNeeded((Core.Packets.GetTransactionsPacket)packet.Body, conn.Receiver, durationMilliseconds, callback);
            }
            else if (packet.Header.Command == Core.PacketType.GETHEADERS)
            {
                handler.RegisterNeeded((Core.Packets.GetHeadersPacket)packet.Body, conn.Receiver, durationMilliseconds, callback);
            }

            conn.Send(packet);

            return true;
        }

        public bool Send(IPEndPoint endpoint, Core.Packet packet)
        {
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

            return Send(conn, packet);
        }

        public bool Send(Connection conn, Core.Packet packet)
        {
            if (packet.Header.Length + Constants.PEERBLOOM_PACKET_HEADER_SIZE > Constants.MAX_PEERBLOOM_PACKET_SIZE)
            {
                Daemon.Logger.Error($"Network.Send: Sent packet was larger than allowed {Constants.MAX_PEERBLOOM_PACKET_SIZE} bytes.");
                return false;
            }

            Daemon.Logger.Info($"Network.Send: Sending {packet.Header.Command} to {conn.Receiver}");

            // hacky; make specific functions for sending packets which call this instead (in the future)
            if (packet.Header.Command == Core.PacketType.GETBLOCKS)
            {
                handler.RegisterNeeded((Core.Packets.GetBlocksPacket)packet.Body, conn.Receiver);
            }
            else if (packet.Header.Command == Core.PacketType.GETTXS)
            {
                handler.RegisterNeeded((Core.Packets.GetTransactionsPacket)packet.Body, conn.Receiver);
            }
            else if (packet.Header.Command == Core.PacketType.GETHEADERS)
            {
                handler.RegisterNeeded((Core.Packets.GetHeadersPacket)packet.Body, conn.Receiver);
            }

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
        
        public async Task RunNetwork()
        {
            while (!_shutdownTokenSource.IsCancellationRequested)
            {
                await Task.Delay(60 * 1000, _shutdownTokenSource.Token);

                if (_shutdownTokenSource.IsCancellationRequested) return;

                if (_network.OutboundConnectedPeers.Count + _network.ConnectingPeers.Count < Constants.PEERBLOOM_MAX_OUTBOUND_CONNECTIONS)
                {
                    (Peer p, _) = peerlist.Select(false, true);
                    
                    if (p != null && !_network.OutboundConnectedPeers.ContainsKey(p.Endpoint) && !_network.ConnectingPeers.ContainsKey(p.Endpoint) && !_network.Feelers.ContainsKey(p.Endpoint))
                    {
                        _ = Task.Run(async () =>
                        {
                            if (p.Endpoint.Address.Equals(_network.ReflectedAddress)) return;

                            Connection conn = new Connection(p.Endpoint, this, LocalNode, true, p);

                            bool success = await conn.Connect(true, _shutdownTokenSource.Token, false);

                            peerlist.Attempt(p.Endpoint, !success);
                        });
                    }
                }

                foreach (var conn in OutboundConnectedPeers.Values)
                {
                    var peer = peerlist.FindPeer(conn.Receiver, out _);

                    // can sometimes happen during client restarts
                    if (peer == null)
                    {
                        peerlist.AddNew(conn.Receiver, conn.Receiver, 0);
                        peer = peerlist.FindPeer(conn.Receiver, out _);
                    }

                    if (!peer.InTried)
                    {
                        peerlist.Good(conn.Receiver, false);
                    }
                    
                    //peer.LastSeen = DateTime.UtcNow.Ticks;
                }

                /*foreach (var conn in InboundConnectedPeers.Values)
                {
                    if (conn.ConnectionAcknowledged)
                    {
                        var peer = peerlist.FindPeer(conn.Receiver, out _);
                        if (peer != null) peer.LastSeen = DateTime.UtcNow.Ticks;
                    }
                }*/

                foreach (var conn in ConnectingPeers.Values)
                {
                    if (!conn.ConnectionAcknowledged && 
                        new DateTime(conn.LastValidReceive).AddSeconds(Constants.CONNECTING_FORCE_TIMEOUT).Ticks < DateTime.UtcNow.Ticks &&
                        new DateTime(conn.LastValidSend).AddSeconds(Constants.CONNECTING_FORCE_TIMEOUT).Ticks < DateTime.UtcNow.Ticks)
                    {
                        await conn.Disconnect(true, code: Core.Packets.Peerbloom.DisconnectCode.CONNECTING_TIMEOUT);
                    }
                }
            }
        }
    }
}
