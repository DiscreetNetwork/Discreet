using Discreet.Network.Peerbloom.Extensions;
using Discreet.Network.Peerbloom.Protocol.Common;
using System;
using System.Collections.Generic;
using System.IO;
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
                //WritePacketBase requestPacket = new WritePacketBase();
                //requestPacket.WriteString("Connect");
                //requestPacket.WriteKey(localNode.Id.Value);
                //requestPacket.WriteInt(localNode.Endpoint.Port);

                Core.Packets.Peerbloom.Connect connectBody = new Core.Packets.Peerbloom.Connect { ID = localNode.Id.Value, Port = localNode.Endpoint.Port };
                Core.Packet connect = new Core.Packet(Core.PacketType.CONNECT, connectBody);

                await _tcpClient.GetStream().WriteAsync(connect.Serialize());

                //ReadPacketBase responsePacket = new ReadPacketBase(await _tcpClient.ReadBytesAsync());

                //bool isPublic = responsePacket.ReadBoolean();
                //ConnectionAcknowledged = responsePacket.ReadBoolean();
                //Id = new NodeId(responsePacket.ReadKey());

                Core.Packet connectAck = new Core.Packet(await _tcpClient.ReadBytesAsync());
                Core.Packets.Peerbloom.ConnectAck connectAckBody = (Core.Packets.Peerbloom.ConnectAck)connectAck.Body;

                bool isPublic = connectAckBody.IsPublic;
                ConnectionAcknowledged = connectAckBody.Acknowledged;
                Id = new NodeId(connectAckBody.ID);

                SetLastSeen();
                return (ConnectionAcknowledged, isPublic);
            }
            catch (SocketException e)
            {
                Visor.Logger.Log(e.Message);

                return (false, false);
            }
            catch (Exception ex)
            {
                Visor.Logger.Log(ex.Message);

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

            client.ReceiveTimeout = 15000;
            client.SendTimeout = 15000;

            try
            {
                await client.ConnectAsync(Endpoint.Address, Endpoint.Port);

                //WritePacketBase requestPacket = new WritePacketBase();
                //requestPacket.WriteString("Ping");
                //await client.GetStream().WriteAsync(requestPacket.ToNetworkByteArray());

                Core.Packet packet = new Core.Packet(Core.PacketType.NETPING, new Core.Packets.Peerbloom.NetPing { Data = new byte[0] });
                await client.GetStream().WriteAsync(packet.Serialize());

                //ReadPacketBase responsePacket = new ReadPacketBase(await client.ReadBytesAsync());
                Core.Packet resp = new Core.Packet(await client.ReadBytesAsync());

                client.Close();

                //string response = responsePacket.ReadString();
                if (resp.Header.Command == Core.PacketType.NETPONG)
                {
                    SetLastSeen();
                    Core.Packets.Peerbloom.NetPong respBody = (Core.Packets.Peerbloom.NetPong)resp.Body;
                    Visor.Logger.Log($"Pinged: {Endpoint.Address}:{Endpoint.Port} and got data \"{Common.Printable.Hexify(respBody.Data)}\"");
                    return true;
                }

                Visor.Logger.Log($"Could not ping: {Endpoint.Address}:{Endpoint.Port}");
                return false;
            }
            catch (SocketException e)
            {
                Visor.Logger.Log(e.Message);

                return false;
            }
            catch (IOException e)
            {
                Visor.Logger.Log($"RemoteNode.Ping: Client timed out at {Endpoint}");

                return false;
            }
            catch (Exception ex)
            {
                Visor.Logger.Log(ex.Message);

                return false;
            }
        }

        public async Task<List<RemoteNode>> FindNode(NodeId localNodeId, IPEndPoint localEndpoint, NodeId target)
        {
            TcpClient client = new TcpClient();

            try
            {
                await client.ConnectAsync(Endpoint.Address, Endpoint.Port);

                //WritePacketBase requestPacket = new WritePacketBase();
                //requestPacket.WriteString("FindNode");
                //requestPacket.WriteInt(localEndpoint.Port); // Also send the port we can be reached on
                //requestPacket.WriteKey(localNodeId.Value); // Also send our NodeId
                //requestPacket.WriteKey(target.Value);
                //await client.GetStream().WriteAsync(requestPacket.ToNetworkByteArray());

                Core.Packets.Peerbloom.FindNode findNodeBody = new Core.Packets.Peerbloom.FindNode { Port = localEndpoint.Port, ID = localNodeId.Value, Dest = target.Value };
                Core.Packet findNode = new Core.Packet(Core.PacketType.FINDNODE, findNodeBody);

                await client.GetStream().WriteAsync(findNode.Serialize());

                //ReadPacketBase responsePacket = new ReadPacketBase(await client.ReadBytesAsync());
                Core.Packet resp = new Core.Packet(await client.ReadBytesAsync());
                Core.Packets.Peerbloom.FindNodeResp respBody = (Core.Packets.Peerbloom.FindNodeResp)resp.Body;

                List<RemoteNode> remoteNodes = new List<RemoteNode>();
                foreach (var elem in respBody.Elems)
                {
                    NodeId id = new NodeId(elem.ID);
                    remoteNodes.Add(new RemoteNode(id, elem.Endpoint));
                }

                Visor.Logger.Log($"Sent `FindNode` to: {Endpoint.Address}:{Endpoint.Port}");
                return remoteNodes;
            }
            catch (SocketException e)
            {
                Visor.Logger.Log($"Failed to send `FindNode` to: {Endpoint.Address}:{Endpoint.Port} : {e.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Visor.Logger.Log(ex.Message);

                return null;
            }
        }

        public async Task Send(WritePacketBase messagePacket)
        {
            await _tcpClient.GetStream().WriteAsync(messagePacket.ToNetworkByteArray());
        }

        public async Task Send(Core.Packet packet)
        {
            await _tcpClient.GetStream().WriteAsync(packet.Serialize());
        }
    }
}
