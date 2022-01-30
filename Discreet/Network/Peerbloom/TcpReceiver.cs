using Discreet.Network.Peerbloom.Extensions;
using Discreet.Network.Peerbloom.Protocol.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom
{
    public class TcpReceiver
    {
        private readonly LocalNode _localNode;
        private readonly ConnectionPool _connectionPool;
        private readonly CancellationToken _cancellationToken;
        private readonly BucketManager _bucketManager;

        public TcpReceiver(LocalNode localNode, ConnectionPool connectionPool, BucketManager bucketManager, CancellationToken cancellationToken)
        {
            _localNode = localNode;
            _connectionPool = connectionPool;
            _bucketManager = bucketManager;
            _cancellationToken = cancellationToken;
        }

        public async Task Start()
        {
            TcpListener listener = new TcpListener(_localNode.Endpoint);
            listener.Start();
            while (!_cancellationToken.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                _ = HandleConnection(client);
            }
        }

        private async Task HandleConnection(TcpClient client)
        {
            IPEndPoint senderEndpoint = (IPEndPoint)client.Client.RemoteEndPoint;
            ReadPacketBase requestPacket = new ReadPacketBase(await client.ReadBytesAsync());
            WritePacketBase responsePacket = new WritePacketBase();
            switch (requestPacket.ReadString())
            {
                case "Connect":
                    Console.WriteLine($"Received `Connect` from: {senderEndpoint.Address}");

                    NodeId remoteNodeId = new NodeId(requestPacket.ReadBigInteger());
                    int remoteNodePort = requestPacket.ReadInt();
                    IPEndPoint remoteListenerEndpoint = new IPEndPoint(senderEndpoint.Address, remoteNodePort);
                    RemoteNode remoteNode = new RemoteNode(remoteNodeId, remoteListenerEndpoint, client);

                    bool isPublic = await remoteNode.Ping();

                    _connectionPool.AddInboundConnection(remoteNode);
                    bool acknowledged = true;

                    responsePacket.WriteBoolean(isPublic);
                    responsePacket.WriteBoolean(acknowledged);
                    responsePacket.WriteBigInteger(_localNode.Id.Value);
                    await client.GetStream().WriteAsync(responsePacket.ToNetworkByteArray());

                    _ = HandlePersistentConnection(client);
                    break;

                case "Ping":

                    responsePacket.WriteString("Pong");
                    await client.GetStream().WriteAsync(responsePacket.ToNetworkByteArray());

                    client.Dispose(); // Dispose this client, as its just a request response packet to determine if a connection can be made to this node
                    break;


                case "FindNode":
                    Console.WriteLine($"Received `FindNode` from: {senderEndpoint.Address}");
                    senderEndpoint.Port = requestPacket.ReadInt();
                    NodeId senderNodeId = new NodeId(requestPacket.ReadBigInteger());
                    RemoteNode senderNode = new RemoteNode(senderNodeId, senderEndpoint);
                    _bucketManager.AddRemoteNode(senderNode);

                    NodeId target = new NodeId(requestPacket.ReadBigInteger());

                    var nodes = _bucketManager.GetClosestNodes(target, senderNode.Id);
                    WritePacketBase findNodeResponse = new WritePacketBase();
                    findNodeResponse.WriteInt(nodes.Count);
                    foreach (var node in nodes)
                    {
                        findNodeResponse.WriteBigInteger(node.Id.Value);
                        findNodeResponse.WriteString(node.Endpoint.Address.ToString());
                        findNodeResponse.WriteInt(node.Endpoint.Port);
                    }
                    await client.GetStream().WriteAsync(findNodeResponse.ToNetworkByteArray());

                    client.Dispose(); // Dispose this client, as its just a request response packet
                    break;

                
            }


            /// TODO:
            ///  - Look into `TIME_WAIT` in a TCP Server/client model
            ///  - The server shouldnt close / dispose the connection, because that cant hinder the performance due to `TIME_WAIT`
            ///  - It should be the client that initiated the connection, that should also close it to avoid this issue
        }

        public async Task HandlePersistentConnection(TcpClient client)
        {
            IPEndPoint senderEndpoint = (IPEndPoint)client.Client.RemoteEndPoint;

            while (true)
            {
                ReadPacketBase requestPacket = new ReadPacketBase(await client.ReadBytesAsync());
                WritePacketBase responsePacket = new WritePacketBase();
                switch (requestPacket.ReadString())
                {
                    case "Message":
                        Console.WriteLine($"Received `Message` from: {senderEndpoint.Address}");

                        string messageId = requestPacket.ReadString();
                        string content = requestPacket.ReadString();
                        Handler.GetHandler().Handle(messageId + ": " + content);
                        break;
                }
            }
        }
    }
}
