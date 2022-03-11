using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using Discreet.Network.Peerbloom.Extensions;
using System.Threading;
using System.Security.Cryptography;

namespace Discreet.Network.Peerbloom
{
    public class Connection
    {
        public IPEndPoint Receiver { get; set; }
        public LocalNode Sender { get; set; }

        public byte[] WriteBuffer;
        public byte[] ReadBuffer;

        public int ReadOffset;
        public int WriteOffset;

        public long LastValidReceive;
        public long LastValidSend;

        private TcpClient _tcpClient;

        public TcpClient Client { get { return _tcpClient; } }

        public ConcurrentQueue<Core.Packet> WriteQueue;

        private Network _network;

        private const int connectTimeout = 10;

        /// <summary>
        /// Have we initialized a connection that has been ack'd on both ends?
        /// </summary>
        public bool ConnectionAcknowledged { get; private set; }

        public bool IsPersistent { get; private set; }

        public Connection(TcpClient tcpClient, Network network, LocalNode node)
        {
            LastValidReceive = DateTime.UtcNow.Ticks;
            LastValidSend = DateTime.UtcNow.Ticks;

            WriteBuffer = new byte[65536];
            ReadBuffer = new byte[65536];

            _tcpClient = tcpClient;

            _network = network;

            WriteQueue = new ConcurrentQueue<Core.Packet>();

            ConnectionAcknowledged = false;

            Sender = node;
            Receiver = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
        }

        public Connection(IPEndPoint receiver, Network network, LocalNode node)
        {
            _tcpClient = new TcpClient();
            LastValidReceive = DateTime.UtcNow.Ticks;
            LastValidSend = DateTime.UtcNow.Ticks;

            WriteBuffer = new byte[65536];
            ReadBuffer = new byte[65536];

            _network = network;

            WriteQueue = new ConcurrentQueue<Core.Packet>();

            ConnectionAcknowledged = false;

            Sender = node;
            Receiver = receiver;
        }

        /// <summary>
        /// Try to open a connection to the node, and return whether the acknowledge were successfull
        /// </summary>
        /// <param name="localNode"></param>
        /// <returns></returns>
        public async Task Connect(bool persist = false, CancellationToken token = default)
        {
            try
            {
                if (!_tcpClient.Connected)
                {
                    var result = _tcpClient.BeginConnect(Receiver.Address, Receiver.Port, null, null);

                    var _timeout = DateTime.UtcNow.AddSeconds(connectTimeout).Ticks;

                    while (!result.IsCompleted && _timeout > DateTime.UtcNow.Ticks)
                    {
                        await Task.Delay(500);
                    }

                    var success = result.IsCompleted;

                    if (!success)
                    {
                        Daemon.Logger.Warn($"Connection.Connect: Could not connect to peer {Receiver} due to timeout");

                        _tcpClient.Dispose();
                    }

                    Core.Packets.Peerbloom.Connect connectBody = new Core.Packets.Peerbloom.Connect { Endpoint = new IPEndPoint(Sender.Endpoint.Address, Sender.Endpoint.Port) };
                    Core.Packet connect = new Core.Packet(Core.PacketType.CONNECT, connectBody);

                    await _tcpClient.GetStream().WriteAsync(connect.Serialize());

                    Core.Packet connectAck = await _tcpClient.ReadPacketAsync();
                    Core.Packets.Peerbloom.ConnectAck connectAckBody = (Core.Packets.Peerbloom.ConnectAck)connectAck.Body;

                    IsPersistent = true;
                    ConnectionAcknowledged = connectAckBody.Acknowledged;

                    _network.AddConnection(this);

                    if (persist)
                    {
                        await Persistent(token);
                    }
                }
                else
                {
                    _network.AddConnection(this);
                    IsPersistent = true;
                    ConnectionAcknowledged = true;
                    await Persistent(token);
                }
            }
            catch (SocketException e)
            {
                Daemon.Logger.Error($"Connection.Connect: {e.Message}");
            }
            catch (Exception ex)
            {
                if (ex is InvalidOperationException) Daemon.Logger.Debug($"Connect.Connect: socket was unexpectedly closed.");
                Daemon.Logger.Error($"Connection.Connect: {ex.Message}");
            }
        }

        public async Task<List<IPEndPoint>> RequestPeers(IPEndPoint localEndpoint)
        {
            if (!ConnectionAcknowledged)
            {
                Daemon.Logger.Error($"RequestPeers: connection was never acknowledged with peer {Receiver}");

                return null;
            }

            if (!_tcpClient.Connected)
            {
                Daemon.Logger.Error($"RequestPeers: node is not currently connected with peer {Receiver}");

                return null;
            }

            try
            {
                Core.Packets.Peerbloom.RequestPeers findNodeBody = new Core.Packets.Peerbloom.RequestPeers { Endpoint = localEndpoint, MaxPeers = 10 };
                Core.Packet findNode = new Core.Packet(Core.PacketType.REQUESTPEERS, findNodeBody);

                await _tcpClient.GetStream().WriteAsync(findNode.Serialize());
                Daemon.Logger.Info($"Sent `RequestPeers` to: {Receiver.Address}:{Receiver.Port}");

                Core.Packet resp = await _tcpClient.ReadPacketAsync();
                Core.Packets.Peerbloom.RequestPeersResp respBody = (Core.Packets.Peerbloom.RequestPeersResp)resp.Body;

                if (respBody.Length > 10)
                {
                    throw new Exception($"RequestPeers: Received too many nodes (got {respBody.Length}; maximum is set to 10)");
                }

                List<IPEndPoint> remoteNodes = respBody.Elems.Select(x => x.Endpoint).ToList();

                return remoteNodes;
            }
            catch (SocketException e)
            {
                Daemon.Logger.Error($"Failed to send `RequestPeers` to: {Receiver.Address}:{Receiver.Port} : {e.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"Connection.RequestPeers: {ex.Message}");

                return null;
            }
        }

        public void Send(Core.Packet p)
        {
            WriteQueue.Enqueue(p);
        }

        public async Task SendAll()
        {
            while (!WriteQueue.IsEmpty)
            {
                Core.Packet p;
                bool success = WriteQueue.TryDequeue(out p);

                if (!success) return;

                Daemon.Logger.Debug($"Connection.SendAll: sending packet {p.Header.Command} to {Receiver}");

                try
                {
                    await _tcpClient.GetStream().WriteAsync(p.Serialize());

                }
                catch (Exception e)
                {
                    Daemon.Logger.Error($"Connection.SendAll: {e}");
                }
            }
        }

        public async Task Persistent(CancellationToken token)
        {
            var ns = _tcpClient.GetStream();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!WriteQueue.IsEmpty) await SendAll();

                    if (ns.DataAvailable)
                    {
                        byte[] _headerBytes = new byte[10];
                        var _numBytes = await ns.ReadAsync(_headerBytes, 0, 10);

                        if (_numBytes < 10)
                        {
                            Daemon.Logger.Error($"Connection.Persistent: received {_numBytes} during read (less than header length)");

                            continue;
                        }

                        Core.PacketHeader Header = new(_headerBytes);

                        if (Header.NetworkID != Daemon.DaemonConfig.GetConfig().NetworkID)
                        {
                            throw new Exception($"wrong network ID; expected {Daemon.DaemonConfig.GetConfig().NetworkID} but got {Header.NetworkID}");
                        }

                        if ((Header.Length + 10) > Constants.MAX_PEERBLOOM_PACKET_SIZE)
                        {
                            throw new Exception($"Received packet was larger than allowed {Constants.MAX_PEERBLOOM_PACKET_SIZE} bytes.");
                        }

                        byte[] _bytes = new byte[Header.Length];

                        var timeout = DateTime.Now.AddSeconds(5);
                        int _numRead;
                        int _offset = 0;

                        do
                        {
                            _numRead = await ns.ReadAsync(_bytes, _offset, _bytes.Length - _offset);
                            _offset += _numRead;
                        } while (_offset < Header.Length && DateTime.Now.Ticks < timeout.Ticks && _numRead > 0);

                        if (_offset < Header.Length)
                        {
                            throw new Exception($"ReadPacketAsync: expected {Header.Length} bytes in payload, but got {_numRead}");
                        }

                        uint _checksum = Coin.Serialization.GetUInt32(SHA256.HashData(SHA256.HashData(_bytes)), 0);
                        if (_checksum != Header.Checksum)
                        {
                            throw new Exception($"ReadPacketAsync: checksum mismatch; got {Header.Checksum}, but calculated {_checksum}");
                        }

                        Core.Packet p = new Core.Packet();

                        p.Header = Header;
                        p.Body = Core.Packet.DecodePacketBody(Header.Command, _bytes, 0);

                        _network.AddPacketToQueue(p, this);
                    }

                    await Task.Delay(100);
                    
                }
                catch (Exception ex)
                {
                    Daemon.Logger.Error($"Connection.Persistent: {ex.Message}");
                    break;
                }
            }
        }
    }
}
