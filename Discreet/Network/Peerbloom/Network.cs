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
                if (_network == null) _network = new Network(Daemon.DaemonConfig.GetDefault().Endpoint);
            }
        }

        public LocalNode LocalNode;

        /// <summary>
        /// A store to hold ids of received messages. 
        /// Used determine if we have already seen / received a specific message from the network
        /// </summary>
        private MessageStore _messageStore;

        public ConcurrentDictionary<IPEndPoint, Connection> ConnectedPeers = new();

        public HashSet<IPEndPoint> ConnectingPeers = new();

        public HashSet<IPEndPoint> UnconnectedPeers = new();

        public int MinDesiredConnections;
        public int MaxDesiredConnections;

        public void AddConnection(Connection conn)
        {
            if (ConnectedPeers.Any(n => n.Key.Address.Equals(conn.Receiver))) return;

            if (!ConnectedPeers.TryAdd(conn.Receiver, conn))
            {
                Daemon.Logger.Error($"Network.AddConnection failed for connection {conn.Receiver}");
            }
        }

        public List<IPEndPoint> GetPeers(int maxPeers)
        {
            List<IPEndPoint> remoteNodes = new List<IPEndPoint>(ConnectedPeers.Keys);

            Random random = new Random();

            while (remoteNodes.Count > maxPeers)
            {
                remoteNodes.RemoveAt(random.Next(0, remoteNodes.Count));
            }

            return remoteNodes;
        }

        public Connection GetPeer(IPEndPoint endpoint)
        {
            foreach (var node in ConnectedPeers.Values)
            {
                if (node.Receiver.Address.MapToIPv4().Equals(endpoint.Address.MapToIPv4()))
                {
                    return node;
                }
            }

            return null;
        }

        public void RemoveNodeFromPool(Connection node)
        {
            if (node == null) return;

            foreach (var _node in ConnectedPeers.Values)
            {
                if (node == _node)
                {
                    ConnectedPeers.TryRemove(node.Receiver, out _);
                }
            }
        }

        public void RemoveNodeFromPool(IPEndPoint node)
        {
            if (node == null) return;

            foreach (var _node in ConnectedPeers.Keys)
            {
                if (_node.Equals(node))
                {
                    ConnectedPeers.TryRemove(node, out _);
                }
            }
        }

        public Handler handler { get; private set; }

        /// <summary>
        /// A common tokenSource to control our loops. 
        /// Can be used to gracefully shut down the application
        /// </summary>
        private CancellationTokenSource _shutdownTokenSource;

        public bool IsBootstrapping { get; private set; } = false;

        /// <summary>
        /// Default constructor for the network class
        /// </summary>
        public Network(IPEndPoint endpoint)
        {
            LocalNode = new LocalNode(endpoint);
            _messageStore = new MessageStore();
            _shutdownTokenSource = new CancellationTokenSource();
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
                    var conn = new Connection(client, this, LocalNode);
                    _ = Task.Run(() => conn.Connect(true, _shutdownTokenSource.Token), token);
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
            Connection bootstrapNode = new Connection(new IPEndPoint(Daemon.DaemonConfig.GetDefault().BootstrapNode, 5555), this, LocalNode);

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

                return;
            }

            Daemon.Logger.Info("Bootstrapping the node...");

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

            Daemon.Logger.Info("Connecting to nodes...");

            HashSet<IPEndPoint> endpoints = new HashSet<IPEndPoint>(fetchedNodes);

            if (Daemon.Daemon.DebugMode)
            {
                _ = Task.Run(() => bootstrapNode.Persistent(_shutdownTokenSource.Token));
            }

            foreach (var node in endpoints)
            {
                if (GetNode(node) != null) continue;

                _ = Task.Run(() => new Connection(node, this, LocalNode).Connect(true, _shutdownTokenSource.Token)).ConfigureAwait(false);
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
            try
            {
                if (packet.Header.Length + 10 > Constants.MAX_PEERBLOOM_PACKET_SIZE) throw new Exception($"Sent packet was larger than allowed {Constants.MAX_PEERBLOOM_PACKET_SIZE} bytes.");
            } catch (Exception ex)
            {
                Daemon.Logger.Error(ex.Message);
                return -1;
            }

            Daemon.Logger.Info($"Discreet.Network.Peerbloom.Network.Send: Broadcasting {packet.Header.Command}");

            int i = 0;

            foreach (var conn in ConnectedPeers.Values)
            {
                conn.Send(packet);
                i++;
            }

            return i;
        }

        public bool Send(IPEndPoint endpoint, Core.Packet packet)
        {
            try
            {
                if (packet.Header.Length + 10 > Constants.MAX_PEERBLOOM_PACKET_SIZE) throw new Exception($"Sent packet was larger than allowed {Constants.MAX_PEERBLOOM_PACKET_SIZE} bytes.");
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error(ex.Message);
                return false;
            }

            Daemon.Logger.Info($"Discreet.Network.Peerbloom.Network.Send: Sending {packet.Header.Command} to {endpoint}");

            bool success = ConnectedPeers.TryGetValue(endpoint, out var conn);

            if (!success || conn == null) return false;

            conn.Send(packet);

            return true;
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
