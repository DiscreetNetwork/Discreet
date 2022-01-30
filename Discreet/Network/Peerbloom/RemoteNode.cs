using Discreet.Network.Peerbloom.Extensions;
using Discreet.Network.Peerbloom.Protocol.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom
{
    public class RemoteNode
    {
        /// <summary>
        /// The NodeId that belongs to the remote node (peer)
        /// </summary>
        public NodeId Id { get; private set; }

        /// <summary>
        /// The IP & Port that the node can be contacted with
        /// </summary>
        public IPEndPoint Endpoint { get; private set; }

        /// <summary>
        /// Last time we pinged and received a response from the node
        /// </summary>
        public DateTime LastSeen { get; private set; }

        /// <summary>
        /// Have we initialized a connection that has been ack'd on both ends?
        /// </summary>
        public bool ConnectionAcknowledged { get; private set; }

        private TcpClient _tcpClient = new TcpClient();
        public TcpClient GetSocket() => _tcpClient;

        public RemoteNode(NodeId nodeId, IPEndPoint ipEndpoint)
        {
            Id = nodeId;
            Endpoint = ipEndpoint;
            LastSeen = DateTime.UtcNow;
        }

        public RemoteNode(IPEndPoint ipEndpoint)
        {
            Endpoint = ipEndpoint;
            LastSeen = DateTime.UtcNow;
        }

        /// <summary>
        /// Used when constructing a RemoteNode from a 'Connect' Packet, with a open tcp client
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="endpoint"></param>
        /// <param name="client"></param>
        public RemoteNode(NodeId nodeId, IPEndPoint endpoint, TcpClient client)
        {
            Id = nodeId;
            Endpoint = endpoint;
            _tcpClient = client;
        }

        public void SetLastSeen() => LastSeen = DateTime.UtcNow;

        /// <summary>
        /// Try to open a connection to the node, and return whether the acknowledge were successfull
        /// </summary>
        /// <param name="localNode"></param>
        /// <returns></returns>
        public async Task<(bool, bool)> Connect(LocalNode localNode)
        {
            try
            {
                await _tcpClient.ConnectAsync(Endpoint.Address, Endpoint.Port);

                WritePacketBase requestPacket = new WritePacketBase();
                requestPacket.WriteString("Connect");
                requestPacket.WriteBigInteger(localNode.Id.Value);
                requestPacket.WriteInt(localNode.Endpoint.Port);
                await _tcpClient.GetStream().WriteAsync(requestPacket.ToNetworkByteArray());

                ReadPacketBase responsePacket = new ReadPacketBase(await _tcpClient.ReadBytesAsync());

                bool isPublic = responsePacket.ReadBoolean();
                ConnectionAcknowledged = responsePacket.ReadBoolean();
                Id = new NodeId(responsePacket.ReadBigInteger());

                SetLastSeen();
                return (ConnectionAcknowledged, isPublic);
            }
            catch (SocketException)
            {
                return (false, false);
            }
        }

        /// <summary>
        /// A single Request-Response packet, to determine if the RemoteNode can be contacted on its localEndpoint
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Ping()
        {
            TcpClient client = new TcpClient();

            try
            {
                await client.ConnectAsync(Endpoint.Address, Endpoint.Port);

                WritePacketBase requestPacket = new WritePacketBase();
                requestPacket.WriteString("Ping");
                await client.GetStream().WriteAsync(requestPacket.ToNetworkByteArray());

                ReadPacketBase responsePacket = new ReadPacketBase(await client.ReadBytesAsync());
                client.Close();

                string response = responsePacket.ReadString();
                if (response.Equals("Pong"))
                {
                    SetLastSeen();
                    Console.WriteLine($"Pinged: {Endpoint.Address}:{Endpoint.Port}");
                    return true;
                }

                Console.WriteLine($"Could not ping: {Endpoint.Address}:{Endpoint.Port}");
                return false;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        public async Task<List<RemoteNode>> FindNode(NodeId localNodeId, IPEndPoint localEndpoint, NodeId target)
        {
            TcpClient client = new TcpClient();

            try
            {
                await client.ConnectAsync(Endpoint.Address, Endpoint.Port);

                WritePacketBase requestPacket = new WritePacketBase();
                requestPacket.WriteString("FindNode");
                requestPacket.WriteInt(localEndpoint.Port); // Also send the port we can be reached on
                requestPacket.WriteBigInteger(localNodeId.Value); // Also send our NodeId
                requestPacket.WriteBigInteger(target.Value);
                await client.GetStream().WriteAsync(requestPacket.ToNetworkByteArray());

                ReadPacketBase responsePacket = new ReadPacketBase(await client.ReadBytesAsync());

                List<RemoteNode> remoteNodes = new List<RemoteNode>();
                int nodeCount = responsePacket.ReadInt();
                for (int i = 0; i < nodeCount; i++)
                {
                    NodeId id = new NodeId(responsePacket.ReadBigInteger());
                    IPAddress ip = IPAddress.Parse(responsePacket.ReadString());
                    int port = responsePacket.ReadInt();
                    remoteNodes.Add(new RemoteNode(id, new IPEndPoint(ip, port)));
                }

                Console.WriteLine($"Sent `FindNode` to: {Endpoint.Address}:{Endpoint.Port}");
                return remoteNodes;
            }
            catch (SocketException)
            {
                Console.WriteLine($"Failed to send `FindNode` to: {Endpoint.Address}:{Endpoint.Port}");
                return null;
            }
        }

        public async Task Send(WritePacketBase messagePacket)
        {
            await _tcpClient.GetStream().WriteAsync(messagePacket.ToNetworkByteArray());
        }
    }
}
