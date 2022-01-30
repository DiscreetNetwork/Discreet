using Discreet.Network.Peerbloom.Extensions;
using Discreet.Network.Peerbloom.Protocol.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom
{
    /// <summary>
    /// The root / entrypoint of the network, which hold references to all the required classes, 
    /// that makes the network operate
    /// </summary>
    public class Network
    {
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
            _localNode = new LocalNode(endpoint);
            _bucketManager = new BucketManager(_localNode);
            _messageStore = new MessageStore();
            _connectionPool = new ConnectionPool();
            _shutdownTokenSource = new CancellationTokenSource();
            _tcpReceiver = new TcpReceiver(_localNode, _connectionPool, _bucketManager, _shutdownTokenSource.Token);

            // Remove this when done testing
            //Program._messagePacketReceivedEvent += OnSendMessageReceived;
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
            RemoteNode bootstrapNode = new RemoteNode(new IPEndPoint(IPAddress.Parse("IP_HERE"), 5555));

            // For testing locally
            //RemoteNode bootstrapNode = new RemoteNode(new IPEndPoint(IPAddress.Loopback, 5555));

            // This check is in case THIS device, is the bootstrap node. In that case, it should not try to bootstrap itself, as it is the first node in the network
            // Eventually this check needs to be modified, when we include multiple bootstrap nodes
            // For now: make sure the int we check against, matches the port of the bootstrap node, in the line above
            if (_localNode.Endpoint.Port == 5555) return;

            Console.WriteLine("Bootstrapping the node\n");

            (bool acknowledged, bool isPublic) = await bootstrapNode.Connect(_localNode);

            if(!acknowledged)
            {
                Console.WriteLine($"Retrying bootstrap process in {Constants.BOOTSTRAP_RETRY_MILLISECONDS / 1000} seconds..");
                await Task.Delay(Constants.BOOTSTRAP_RETRY_MILLISECONDS);
                Console.Clear();
                await Bootstrap();
                return;
            }

            Console.WriteLine(isPublic ? "Continuing in public mode" : "Continuing in private mode");
            _localNode.SetNetworkMode(isPublic); // This determines how 'this' node relays messages


            _connectionPool.AddOutboundConnection(bootstrapNode);
            _ = _tcpReceiver.HandlePersistentConnection(bootstrapNode.GetSocket());

            _bucketManager.AddRemoteNode(bootstrapNode);


            var fetchedNodes = await bootstrapNode.FindNode(_localNode.Id, _localNode.Endpoint, _localNode.Id); // Perform a self-lookup, by calling FindNode with our own NodeId

            // We failed at establishing a connection to the bootstrap node
            if(fetchedNodes == null)
            {
                Console.WriteLine($"Failed to contact the bootstrap node with a `FindNode` command, retrying bootstrap process in {Constants.BOOTSTRAP_RETRY_MILLISECONDS / 1000} seconds..");
                await Task.Delay(Constants.BOOTSTRAP_RETRY_MILLISECONDS);
                Console.Clear();
                await Bootstrap();
                return;
            }

            // If we didnt get any peers, dont consider the bootstrap a success
            if(fetchedNodes.Count == 0)
            {
                Console.WriteLine($"Received no nodes, retrying bootsrap process in {Constants.BOOTSTRAP_RETRY_MILLISECONDS / 1000} seconds..");
                await Task.Delay(Constants.BOOTSTRAP_RETRY_MILLISECONDS);
                Console.Clear();
                await Bootstrap();
                return;
            }


            fetchedNodes.ForEach(n => _bucketManager.AddRemoteNode(n));
            Bucket bootstrapBucket = _bucketManager.GetBucket(bootstrapNode.Id);

            List<Bucket> otherBuckets = _bucketManager.GetBuckets().Where(x => x != bootstrapBucket).ToList();
            otherBuckets.ForEach(async x => await _bucketManager.RefreshBucket(x));

            Console.WriteLine("\n Connecting to nodes");
            foreach (var bucket in _bucketManager.GetBuckets())
            {
                foreach (var node in bucket.GetNodes())
                {
                    if (_connectionPool.GetOutboundConnections().Any(n => n.Id.Value == node.Id.Value)) continue;

                    (acknowledged, _) = await node.Connect(_localNode);
                    if (!acknowledged) continue;

                    _connectionPool.AddOutboundConnection(node);
                    _ = _tcpReceiver.HandlePersistentConnection(node.GetSocket());

                }
            }

            Console.WriteLine("\nBootstrap completed. Continuing in 2 seconds");
            await Task.Delay(2000);
            //Console.Clear();
        }

        // When sending the initial message
        public async Task SendMessage(string content)
        {
            string messageId = Guid.NewGuid().ToString();
            _messageStore.AddMessageIdentifier(messageId);

            WritePacketBase messagePacket = new WritePacketBase();
            messagePacket.WriteString("Message");                       // Command
            messagePacket.WriteString(messageId);                       // Message ID
            messagePacket.WriteString(content);                         // Message Content

            if(_localNode.IsPublic)
            {
                foreach (var inbound in _connectionPool.GetInboundConnections())
                {
                    await inbound.Send(messagePacket);
                }
            }
            
            foreach (var outbound in _connectionPool.GetOutboundConnections())
            {
                await outbound.Send(messagePacket);
            }
        }

        // When receiving the message
        private void OnSendMessageReceived(string messageId, string content)
        {
            if (_messageStore.Contains(messageId)) return;

            _messageStore.AddMessageIdentifier(messageId);

            Handler.GetHandler().Handle(content);

            WritePacketBase messagePacket = new WritePacketBase();
            messagePacket.WriteString("Message");                       // Command
            messagePacket.WriteString(messageId);                       // Message ID
            messagePacket.WriteString(content);                         // Message Content

            if (_localNode.IsPublic)
            {
                foreach (var inbound in _connectionPool.GetInboundConnections())
                {
                    _ = inbound.Send(messagePacket);
                }
            }

            foreach (var outbound in _connectionPool.GetOutboundConnections())
            {
                _ = outbound.Send(messagePacket);
            }
        }

        
    }
}
