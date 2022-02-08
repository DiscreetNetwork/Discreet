using Discreet.Network.Peerbloom.Extensions;
using Discreet.Network.Peerbloom.Protocol.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

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
                if (_network == null) _network = new Network(Visor.VisorConfig.GetDefault().Endpoint);
            }
        }


        private LocalNode _localNode;

        /// <summary>
        /// The manager class used to perform operations on buckets
        /// </summary>
        private BucketManager _bucketManager;

        /// <summary>
        /// A store to hold ids of received messages. 
        /// Used determine if we have already seen / received a specific message from the network
        /// </summary>
        private MessageStore _messageStore;

        /// <summary>
        /// Holds references to all the 'RemoteNodes' that we have an open connection to
        /// </summary>
        private ConnectionPool _connectionPool;

        private TcpReceiver _tcpReceiver;

        /// <summary>
        /// A common tokenSource to control our loops. 
        /// Can be used to gracefully shut down the application
        /// </summary>
        private CancellationTokenSource _shutdownTokenSource;

        /// <summary>
        /// Default constructor for the network class
        /// </summary>
        public Network(IPEndPoint endpoint)
        {
            _localNode = new LocalNode(endpoint, new NodeId(Visor.VisorConfig.GetConfig().ID));
            _bucketManager = new BucketManager(_localNode);
            _messageStore = new MessageStore();
            _connectionPool = new ConnectionPool();
            _shutdownTokenSource = new CancellationTokenSource();
            _tcpReceiver = new TcpReceiver(_localNode, _connectionPool, _bucketManager, _shutdownTokenSource.Token);

            // Remove this when done testing
            //Program._messagePacketReceivedEvent += OnSendMessageReceived;
        }

        public Cipher.Key GetNodeID()
        {
            return _localNode.Id.Value;
        }

        /// <summary>
        /// Starts the bucket refresh background service &
        /// starts the network receiver
        /// </summary>
        public void Start()
        {
            //_ = BucketRefreshBackgroundService();
            _ = _tcpReceiver.Start();
        }


        /// <summary>
        /// Responsible for periodically running bucket refreshes, to make sure our buckets are up to date
        /// </summary>
        /// <returns></returns>
        public async Task BucketRefreshBackgroundService()
        {
            while(!_shutdownTokenSource.IsCancellationRequested)
            {
                foreach (var bucket in _bucketManager.GetBuckets())
                {
                    if (DateTime.UtcNow < bucket.LastUpdated.AddMilliseconds(Constants.BUCKET_REFRESH_TIME_UNTILL_UPDATE)) continue;

                    await _bucketManager.RefreshBucket(bucket);
                }

                await Task.Delay(Constants.BUCKET_REFRESH_LOOP_DELAY_TIME_MILLISECONDS);
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
            // For actual live chatting
            RemoteNode bootstrapNode = new RemoteNode(new IPEndPoint(Visor.VisorConfig.GetDefault().BootstrapNode, 5555));

            // For testing locally
            //RemoteNode bootstrapNode = new RemoteNode(new IPEndPoint(IPAddress.Loopback, 5555));

            // This check is in case THIS device, is the bootstrap node. In that case, it should not try to bootstrap itself, as it is the first node in the network
            // Eventually this check needs to be modified, when we include multiple bootstrap nodes
            // For now: make sure the int we check against, matches the port of the bootstrap node, in the line above
            if (_localNode.Endpoint.Port == 5555) return;

            Visor.Logger.Log("Bootstrapping the node\n");

            (bool acknowledged, bool isPublic) = await bootstrapNode.Connect(_localNode);

            if(!acknowledged)
            {
                Visor.Logger.Log($"Retrying bootstrap process in {Constants.BOOTSTRAP_RETRY_MILLISECONDS / 1000} seconds..");
                await Task.Delay(Constants.BOOTSTRAP_RETRY_MILLISECONDS);
                //Console.Clear();
                await Bootstrap();
                return;
            }

            Visor.Logger.Log(isPublic ? "Continuing in public mode" : "Continuing in private mode");
            _localNode.SetNetworkMode(isPublic); // This determines how 'this' node relays messages
           
            if (isPublic)
            {
                Handler.GetHandler().SetServices(Core.ServicesFlag.Public);
            }

            _connectionPool.AddOutboundConnection(bootstrapNode);
            _ = _tcpReceiver.HandlePersistentConnection(bootstrapNode.GetSocket());

            _bucketManager.AddRemoteNode(bootstrapNode);


            var fetchedNodes = await bootstrapNode.FindNode(_localNode.Id, _localNode.Endpoint, _localNode.Id); // Perform a self-lookup, by calling FindNode with our own NodeId

            // We failed at establishing a connection to the bootstrap node
            if(fetchedNodes == null)
            {
                Visor.Logger.Log($"Failed to contact the bootstrap node with a `FindNode` command, retrying bootstrap process in {Constants.BOOTSTRAP_RETRY_MILLISECONDS / 1000} seconds..");
                await Task.Delay(Constants.BOOTSTRAP_RETRY_MILLISECONDS);
                //Console.Clear();
                await Bootstrap();
                return;
            }

            // If we didnt get any peers, dont consider the bootstrap a success
            if(fetchedNodes.Count == 0)
            {
                Visor.Logger.Log($"Received no nodes, retrying bootsrap process in {Constants.BOOTSTRAP_RETRY_MILLISECONDS / 1000} seconds..");
                await Task.Delay(Constants.BOOTSTRAP_RETRY_MILLISECONDS);
                //Console.Clear();
                await Bootstrap();
                return;
            }


            fetchedNodes.ForEach(n => _bucketManager.AddRemoteNode(n));
            Bucket bootstrapBucket = _bucketManager.GetBucket(bootstrapNode.Id);

            List<Bucket> otherBuckets = _bucketManager.GetBuckets().Where(x => x != bootstrapBucket).ToList();
            otherBuckets.ForEach(async x => await _bucketManager.RefreshBucket(x));

            Visor.Logger.Log("\n Connecting to nodes");
            foreach (var bucket in _bucketManager.GetBuckets())
            {
                foreach (var node in bucket.GetNodes())
                {
                    if (_connectionPool.GetOutboundConnections().Any(n => n.Id.Value.Equals(node.Id.Value))) continue;

                    (acknowledged, _) = await node.Connect(_localNode);
                    if (!acknowledged) continue;

                    _connectionPool.AddOutboundConnection(node);
                    _ = _tcpReceiver.HandlePersistentConnection(node.GetSocket());

                }
            }

            Visor.Logger.Log("\nBootstrap completed. Continuing in 2 seconds");
            await Task.Delay(2000);
            //Console.Clear();
        }

        // When sending the initial message
        /* DEPRACATED */
        public async Task SendMessage(string content)
        {
            string messageId = Guid.NewGuid().ToString();
            _messageStore.AddMessageIdentifier(messageId);

            uint len = (uint)Encoding.UTF8.GetBytes(content).Length;

            Core.Packet packet = new Core.Packet(Core.PacketType.OLDMESSAGE, new Core.Packets.Peerbloom.OldMessage { MessageIDLen = (uint)messageId.Length, MessageID = messageId, MessageLen = len, Message = content });
            //WritePacketBase messagePacket = new WritePacketBase();
            //messagePacket.WriteString("Message");                       // Command
            //messagePacket.WriteString(messageId);                       // Message ID
            //messagePacket.WriteString(content);                         // Message Content

            if(_localNode.IsPublic)
            {
                foreach (var inbound in _connectionPool.GetInboundConnections())
                {
                    await inbound.Send(packet);
                }
            }
            
            foreach (var outbound in _connectionPool.GetOutboundConnections())
            {
                await outbound.Send(packet);
            }
        }

        public async Task<int> Broadcast(Core.Packet packet)
        {
            int i = 0;

            if (_localNode.IsPublic)
            {
                foreach(var inbound in _connectionPool.GetInboundConnections())
                {
                    await inbound.Send(packet);
                    i++;
                }
            }

            foreach (var outbound in _connectionPool.GetOutboundConnections())
            {
                await outbound.Send(packet);
                i++;
            }

            return i;
        }

        public async Task<bool> Send(IPEndPoint endpoint, Core.Packet packet)
        {
            var node = _connectionPool.FindNodeInPool(endpoint);

            if (node == null) return false;

            await node.Send(packet);

            return true;
        }

        public RemoteNode GetNode(IPEndPoint endpoint)
        {
            return _connectionPool.FindNodeInPool(endpoint);
        }

        // When receiving the message (used for tests; remember to replace the handler call with method specified in Program.cs)
        public void OnSendMessageReceived(string messageId, string content)
        {
            if (_messageStore.Contains(messageId)) return;

            _messageStore.AddMessageIdentifier(messageId);

            // replace with method from Program.cs!
            //Visor.Logger.Log($"Received message ({messageId}): {content}");
            //Program._messageReceivedEvent?.Invoke(content);

            uint len = (uint)Encoding.UTF8.GetBytes(content).Length;

            Core.Packet packet = new Core.Packet(Core.PacketType.OLDMESSAGE, new Core.Packets.Peerbloom.OldMessage { MessageIDLen = (uint)messageId.Length, MessageID = messageId, MessageLen = len, Message = content });

            //WritePacketBase messagePacket = new WritePacketBase();
            //messagePacket.WriteString("Message");                       // Command
            //messagePacket.WriteString(messageId);                       // Message ID
            //messagePacket.WriteString(content);                         // Message Content

            if (_localNode.IsPublic)
            {
                foreach (var inbound in _connectionPool.GetInboundConnections())
                {
                    _ = inbound.Send(packet);
                }
            }

            foreach (var outbound in _connectionPool.GetOutboundConnections())
            {
                _ = outbound.Send(packet);
            }
        }
    }
}
