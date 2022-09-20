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
using System.IO;

namespace Discreet.Network.Peerbloom
{
    public class Connection: IDisposable
    {
        public IPEndPoint Receiver { get; private set; }
        public LocalNode Sender { get; private set; }

        public long TimeStarted { get; private set; }

        public long LastValidReceive = 0;
        public long LastValidSend = 0;

        public long PingLatency = 0;
        public long PingStart = 0;
        public bool WasPinged = false;

        private TcpClient _tcpClient;

        private ConcurrentQueue<Core.Packet> WriteQueue;

        private Network _network;

        /// <summary>
        /// Have we initialized a connection that has been ack'd?
        /// </summary>
        public bool ConnectionAcknowledged { get; private set; }

        /// <summary>
        /// Used to acknowledge incoming connections.
        /// </summary>
        public void SetConnectionAcknowledged() => ConnectionAcknowledged = true;

        public bool IsPersistent { get; private set; }

        public bool IsOutbound { get; private set; }

        private bool disposed = false;

        private SemaphoreSlim _readMutex = new SemaphoreSlim(1, 1);
        private SemaphoreSlim _sendMutex = new SemaphoreSlim(1, 1);

        // the reported port the receiving peer exposes, if public
        private int _port = -1;

        public int Port 
        { 
            get 
            { 
                return _port;
            }

            set { _port = value; }
        }

        public Connection(TcpClient tcpClient, Network network, LocalNode node, bool isOutbound = false)
        {
            LastValidReceive = DateTime.UtcNow.Ticks;
            LastValidSend = DateTime.UtcNow.Ticks;

            _tcpClient = tcpClient;

            _network = network;

            WriteQueue = new ConcurrentQueue<Core.Packet>();

            ConnectionAcknowledged = false;

            Sender = node;
            Receiver = (IPEndPoint)tcpClient.Client.RemoteEndPoint;

            TimeStarted = DateTime.UtcNow.Ticks;

            IsOutbound = isOutbound;
        }

        public Connection(IPEndPoint receiver, Network network, LocalNode node, bool isOutbound = true)
        {
            _tcpClient = new TcpClient();
            LastValidReceive = DateTime.UtcNow.Ticks;
            LastValidSend = DateTime.UtcNow.Ticks;

            _network = network;

            WriteQueue = new ConcurrentQueue<Core.Packet>();

            ConnectionAcknowledged = false;

            Sender = node;
            Receiver = receiver;

            TimeStarted = DateTime.UtcNow.Ticks;

            IsOutbound = isOutbound;
        }

        public async Task<bool> ConnectTest(CancellationToken token = default, int timeout = 5000)
        {
            try
            {
                var _timeout = DateTime.UtcNow.AddMilliseconds(timeout).Ticks;

                var result = _tcpClient.BeginConnect(Receiver.Address, Receiver.Port, null, null);

                while (!result.IsCompleted && _timeout > DateTime.UtcNow.Ticks && !token.IsCancellationRequested)
                {
                    await Task.Delay(50, token);
                }

                if (!result.IsCompleted)
                {
                    Daemon.Logger.Warn($"Connection.ConnectTest: Could not connect to peer {Receiver} due to timeout");
                    _tcpClient.Dispose();
                    return false;
                }

                _tcpClient.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"Connection.ConnectTest: Could not connect to peer {Receiver}: {ex.Message}", ex);
                _tcpClient.Dispose();
                return false;
            }
        }

        /// <summary>
        /// Try to open a connection to the node, and return whether the acknowledge were successful
        /// </summary>
        /// <param name="localNode"></param>
        /// <returns></returns>
        public async Task<bool> Connect(bool persist = false, CancellationToken token = default, bool feeler = false, int customTimeout = -1, int customTries = -1)
        {
            int ConnectTimeout = (customTimeout > 0) ? customTimeout : Constants.CONNECTION_CONNECT_TIMEOUT;
            int NumTries = (customTries > 0) ? customTries : Constants.CONNECTION_MAX_CONNECT_ATTEMPTS;

            if (feeler)
            {
                _network.AddFeeler(this);
            }
            else
            {
                _network.AddConnecting(this);
            }

            /* there is a chance AddConnecting disposes this connection if maximum pending connections is reached. */
            if (disposed)
            {
                _network.RemoveNodeFromPool(this);
                if (feeler)
                {
                    _network.Feelers.TryRemove(Receiver, out _);
                }

                return false;
            }
            
            try
            {
                if (IsOutbound)
                {
                    int numConnectionAttempts = 0;

                    while (numConnectionAttempts < NumTries && !ConnectionAcknowledged && !token.IsCancellationRequested)
                    {
                        if (numConnectionAttempts > 0)
                        {
                            Daemon.Logger.Info($"Connection.Connect: retrying connection attempt and authentication for peer {Receiver}");
                        }

                        /* set timeout for this connect attempt */
                        var _timeout = DateTime.UtcNow.AddMilliseconds(ConnectTimeout).Ticks;

                        /* begin connection */
                        if (!_tcpClient.Connected)
                        {
                            var result = _tcpClient.BeginConnect(Receiver.Address, Receiver.Port, null, null);

                            while (!result.IsCompleted && _timeout > DateTime.UtcNow.Ticks && !token.IsCancellationRequested)
                            {
                                await Task.Delay(500, token);
                            }

                            if (!result.IsCompleted)
                            {
                                Daemon.Logger.Warn($"Connection.Connect: Could not connect to peer {Receiver} due to timeout");
                                numConnectionAttempts++;
                                continue;
                            }
                        }

                        if (!ConnectionAcknowledged)
                        {
                            /* perform connect send */
                            Core.Packets.Peerbloom.VersionPacket verBody = new Core.Packets.Peerbloom.VersionPacket
                            {
                                Version = Daemon.DaemonConfig.GetConfig().NetworkVersion.Value,
                                Services = _network.handler.Services,
                                Timestamp = DateTime.UtcNow.Ticks,
                                Height = DB.DataView.GetView().GetChainHeight(),
                                Port = Daemon.DaemonConfig.GetConfig().Port.Value,
                                Syncing = _network.handler.State == PeerState.Syncing
                            };

                            Core.Packet ver = new Core.Packet(Core.PacketType.VERSION, verBody);

                            byte[] _verData = ver.Serialize();

                            var verHandle = _tcpClient.GetStream().BeginWrite(_verData, 0, _verData.Length, null, null);

                            while (!verHandle.IsCompleted && _timeout > DateTime.UtcNow.Ticks && !token.IsCancellationRequested)
                            {
                                await Task.Delay(50, token);
                            }

                            if (!verHandle.IsCompleted)
                            {
                                break;
                            }

                            _tcpClient.GetStream().EndWrite(verHandle);

                            if (_timeout <= DateTime.UtcNow.Ticks)
                            {
                                Daemon.Logger.Warn($"Connection.Connect: failed to send Version packet to {Receiver} due to timeout");
                                numConnectionAttempts++;
                                continue;
                            }

                            Daemon.Logger.Debug($"Connection.Connect: sent version to {Receiver}");

                            /* receive remote version */
                            byte[] _remoteVersion = new byte[29 + 10];
                            int numReadBytes = 0;

                            do
                            {
                                var remoteVersionHandle = _tcpClient.GetStream().BeginRead(_remoteVersion, 0, _remoteVersion.Length, null, null);

                                while (!remoteVersionHandle.IsCompleted && _timeout > DateTime.UtcNow.Ticks && !token.IsCancellationRequested)
                                {
                                    await Task.Delay(50, token);
                                }

                                if (!remoteVersionHandle.IsCompleted)
                                {
                                    break;
                                }

                                numReadBytes += _tcpClient.GetStream().EndRead(remoteVersionHandle);
                            }
                            while (numReadBytes < _remoteVersion.Length && _timeout > DateTime.UtcNow.Ticks && !token.IsCancellationRequested);

                            if (_timeout <= DateTime.UtcNow.Ticks)
                            {
                                Daemon.Logger.Warn($"Connection.Connect: failed to read version packet from {Receiver} due to timeout");
                                numConnectionAttempts++;
                                continue;
                            }

                            if (numReadBytes < _remoteVersion.Length)
                            {
                                Daemon.Logger.Warn($"Connection.Connect: failed to fully receive version packet from {Receiver}");
                                numConnectionAttempts++;
                                continue;
                            }

                            Core.Packet remoteVersion = new Core.Packet(_remoteVersion);

                            if (remoteVersion.Header.Command != Core.PacketType.VERSION)
                            {
                                Daemon.Logger.Error($"Connection.Connect: packet type mismatch from {Receiver}: Expected VERSION; got {remoteVersion.Header.Command}");
                                numConnectionAttempts++;
                                continue;
                            }

                            Core.Packets.Peerbloom.VersionPacket remoteVersionBody = (Core.Packets.Peerbloom.VersionPacket)remoteVersion.Body;

                            if (remoteVersionBody.Version != Daemon.DaemonConfig.GetConfig().NetworkVersion)
                            {
                                Daemon.Logger.Warn($"Connection.Connect: Bad network version for peer {Receiver}; expected {Daemon.DaemonConfig.GetConfig().NetworkVersion}, but got {remoteVersionBody.Version}");
                                numConnectionAttempts++;
                                continue;
                            }

                            if (remoteVersionBody.Timestamp > DateTime.UtcNow.Add(TimeSpan.FromHours(2)).Ticks || remoteVersionBody.Timestamp < DateTime.UtcNow.Subtract(TimeSpan.FromHours(2)).Ticks)
                            {
                                Daemon.Logger.Warn($"Connection.Connect: version packet timestamp for peer {Receiver} is either too old or too far in the future!");
                                numConnectionAttempts++;
                                continue;
                            }

                            if (remoteVersionBody.Port < 0 || remoteVersionBody.Port > 65535)
                            {
                                Daemon.Logger.Warn($"Connection.Connect: version packet for peer {Receiver} specified an invalid port ({remoteVersionBody.Port})");
                                numConnectionAttempts++;
                                continue;
                            }

                            _port = remoteVersionBody.Port; // set port

                            if (!feeler)
                            {
                                if (_network.Cache.BadVersions.ContainsKey(Receiver))
                                {
                                    _network.Cache.BadVersions.Remove(Receiver, out _);
                                }

                                if (!_network.Cache.Versions.TryAdd(Receiver, remoteVersionBody))
                                {
                                    Daemon.Logger.Error($"Connection.Connect: failed to add version for peer {Receiver}");
                                    Dispose();

                                    return false;
                                }
                            }

                            Daemon.Logger.Debug($"Connection.Connect: received version from {Receiver}");
                            Connection _dupe = _network.GetPeer(new IPEndPoint(Receiver.Address, Port), true);

                            if (!feeler && _dupe != null)
                            {
                                // duplicate connection; end
                                Daemon.Logger.Warn($"Connection.Connect: duplicate peer connection found for {Receiver} (currently at {_dupe.Receiver}); ending connection");
                                await Disconnect(true, Core.Packets.Peerbloom.DisconnectCode.CLEAN);
                                return false;
                            }


                            /* time to send VerAck. At this point we can trust the connection is reliable. */
                            await SendAsync(new Core.Packet(Core.PacketType.VERACK, new Core.Packets.Peerbloom.VerAck { Counter = feeler ? -1 : 0, ReflectedEndpoint = Receiver }), token);

                            Daemon.Logger.Debug($"Connection.Connect: sent verack to {Receiver}");

                            var verAck = await ReadAsync(token);

                            if (verAck.Header.Command != Core.PacketType.VERACK)
                            {
                                Daemon.Logger.Error($"Connection.Connect: packet type mismatch from {Receiver}: Expected VERACK; got {verAck.Header.Command}");
                                numConnectionAttempts++;
                                continue;
                            }

                            Daemon.Logger.Debug($"Connection.Connect: received verack from {Receiver}");

                            var verAckBody = (Core.Packets.Peerbloom.VerAck)verAck.Body;

                            if (verAckBody.Counter <= 0)
                            {
                                /* was not acknowledged, end this connection */
                                Daemon.Logger.Error($"Connection.Connect: could not complete connection, as this node version is out of date or invalid, and was not ACK'd. ending connection");
                                _network.RemoveNodeFromPool(this);
                                Dispose();
                                return false;
                            }

                            IsPersistent = !feeler;
                            ConnectionAcknowledged = true;
                            LastValidReceive = DateTime.UtcNow.Ticks;
                            LastValidSend = DateTime.UtcNow.Ticks;

                            if (_network.OutboundConnectedPeers.Count == 0 || _network.ReflectedAddress == null)
                            {
                                _network.SetReflectedAddress(verAckBody.ReflectedEndpoint.Address);
                            }
                        }
                    }

                    if (numConnectionAttempts >= NumTries)
                    {
                        Daemon.Logger.Error($"Connection.Connect: failed to complete connection with peer {Receiver}, exceeded maximum connection attempts");
                        Dispose();
                        if (feeler)
                        {
                            _network.Feelers.TryRemove(Receiver, out _);
                        }
                        return false;
                    }

                    if (!ConnectionAcknowledged)
                    {
                        Daemon.Logger.Error($"Connection.Connect: failed to authenticate connection with peer {Receiver}");
                        Dispose();
                        if (feeler)
                        {
                            _network.Feelers.TryRemove(Receiver, out _);
                        }
                        return false;
                    }

                    if (!feeler)
                    {
                        _network.AddOutboundConnection(this);
                    }
                }
                else
                {
                    IsPersistent = true;
                    ConnectionAcknowledged = false;
                }
            }
            catch (SocketException e)
            {
                Daemon.Logger.Error($"Connection.Connect: a socket exception was encountered with peer {Receiver}: {e.Message}", e);
                Dispose();
                if (feeler)
                {
                    _network.Feelers.TryRemove(Receiver, out _);
                }

                return false;
            }
            catch (InvalidOperationException)
            {
                Daemon.Logger.Error($"Connection.Connect: socket with peer {Receiver} was unexpectedly closed.");
                Dispose();
                if (feeler)
                {
                    _network.Feelers.TryRemove(Receiver, out _);
                }

                return false;
            }
            catch (IOException e)
            {
                Daemon.Logger.Error($"Connection.Connect: an IO exception was encountered with peer {Receiver}: {e.Message}", e);
                Dispose();
                if (feeler)
                {
                    _network.Feelers.TryRemove(Receiver, out _);
                }

                return false;
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"Connection.Connect: an exception was encountered with peer {Receiver}: {ex.Message}", ex);
                Dispose();
                if (feeler)
                {
                    _network.Feelers.TryRemove(Receiver, out _);
                }

                return false;
            }

            if (_tcpClient != null)
            {
                if (!_tcpClient.Connected && !ConnectionAcknowledged)
                {
                    Dispose();
                    if (feeler)
                    {
                        _network.Feelers.TryRemove(Receiver, out _);
                    }
                    return false;
                }
                else if (!disposed)
                {
                    if (feeler)
                    {
                        _network.Feelers.TryRemove(Receiver, out _);
                        return true;
                    }

                    if (persist) await Persistent(token);

                    return true;
                }
            }
            else
            {
                Dispose();
                if (feeler)
                {
                    _network.Feelers.TryRemove(Receiver, out _);
                }
                return false;
            }

            // fallthrough
            if (feeler)
            {
                Dispose();
                _network.Feelers.TryRemove(Receiver, out _);
            }

            return true;
        }

        public async Task<List<IPEndPoint>> RequestPeers(IPEndPoint localEndpoint, CancellationToken token = default)
        {
            if (!ConnectionAcknowledged)
            {
                Daemon.Logger.Error($"Connection.RequestPeers: connection was never acknowledged with peer {Receiver}");

                return null;
            }

            if (!_tcpClient.Connected)
            {
                Daemon.Logger.Error($"Connection.RequestPeers: node is not currently connected with peer {Receiver}");

                return null;
            }

            try
            {
                /* create RequestPeers */
                Core.Packets.Peerbloom.RequestPeers requestPeersBody = new Core.Packets.Peerbloom.RequestPeers { Endpoint = localEndpoint, MaxPeers = Constants.PEERBLOOM_MAX_PEERS_PER_REQUESTPEERS };
                Core.Packet requestPeers = new Core.Packet(Core.PacketType.REQUESTPEERS, requestPeersBody);

                /* send RequestPeers */
                await SendAsync(requestPeers, token);

                /* receive RequestPeersResp */
                Core.Packet resp = await ReadAsync(token);

                if (resp == null)
                {
                    throw new Exception("response was null");
                }

                Core.Packets.Peerbloom.RequestPeersResp respBody = (Core.Packets.Peerbloom.RequestPeersResp)resp.Body;

                if (respBody.Length > Constants.PEERBLOOM_MAX_PEERS_PER_REQUESTPEERS)
                {
                    Daemon.Logger.Error($"Connection.RequestPeers: Received too many peers (got {respBody.Length}; maximum is set to 10)");
                    return null;
                }

                List<IPEndPoint> remoteNodes = respBody.Elems.Select(x => x.Endpoint).ToList();

                return remoteNodes;
            }
            catch (SocketException e)
            {
                Daemon.Logger.Error($"Connection.RequestPeers: a socket exception was encountered with peer {Receiver}: {e.Message}", e);
            }
            catch (InvalidOperationException)
            {
                Daemon.Logger.Error($"Connection.RequestPeers: socket with peer {Receiver} was unexpectedly closed.");
                _network.RemoveNodeFromPool(this);
            }
            catch (IOException e)
            {
                Daemon.Logger.Error($"Connection.RequestPeers: an IO exception was encountered with peer {Receiver}: {e.Message}", e);
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"Connection.RequestPeers: an exception was encountered with peer {Receiver}: {ex.Message}", ex);
            }

            return null;
        }

        public async Task<bool> SendAsync(Core.Packet p, CancellationToken token = default)
        {
            Daemon.Logger.Debug($"Connection.SendAsync: beginning to send packet to peer {Receiver}");

            try
            {
                var _lockSucceeded = await _sendMutex.WaitAsync(1000);

                if (!_lockSucceeded)
                {
                    throw new Exception($"failed to retrieve access to the connection for peer {Receiver} (Mutex error)");
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"Connection.SendAsync: {ex.Message}", ex);
                _network.RemoveNodeFromPool(this); // this calls dispose on the connection
                return false;
            }

            try
            {
                var _timeout = DateTime.UtcNow.AddMilliseconds(Constants.CONNECTION_WRITE_TIMEOUT).Ticks;

                byte[] _data = p.Serialize();

                var writeHandle = _tcpClient.GetStream().BeginWrite(_data, 0, _data.Length, null, null);

                while (!writeHandle.IsCompleted && _timeout > DateTime.UtcNow.Ticks && !token.IsCancellationRequested)
                {
                    await Task.Delay(50, token);
                }

                if (!writeHandle.IsCompleted)
                {
                    Daemon.Logger.Error($"Connection.SendAsync: failed to send data to {Receiver}");
                    _sendMutex.Release();
                    return false;
                }

                _tcpClient.GetStream().EndWrite(writeHandle);

                if (_timeout <= DateTime.UtcNow.Ticks)
                {
                    Daemon.Logger.Error($"Connection.SendAsync: failed to send data to {Receiver} due to timeout");
                    _sendMutex.Release();
                    return false;
                }

                _sendMutex.Release();
                LastValidSend = DateTime.UtcNow.Ticks;
                return true;
            }
            catch (SocketException e)
            {
                Daemon.Logger.Error($"Connection.SendAsync: a socket exception was encountered with peer {Receiver}: {e.Message}", e);
            }
            catch (InvalidOperationException)
            {
                Daemon.Logger.Error($"Connection.SendAsync: socket with peer {Receiver} was unexpectedly closed.");
                _network.RemoveNodeFromPool(this);
            }
            catch (IOException e)
            {
                Daemon.Logger.Error($"Connection.SendAsync: an IO exception was encountered with peer {Receiver}: {e.Message}", e);
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"Connection.SendAsync: an exception was encountered with peer {Receiver}: {ex.Message}", ex);
            }

            if (!disposed)
            {
                _sendMutex.Release();
            }
            return false;
        }

        public void Send(Core.Packet p)
        {
            WriteQueue.Enqueue(p);
        }

        public async Task<bool> SendAll(CancellationToken token = default)
        {
            Daemon.Logger.Debug("Connection.SendAll: beginning to send packets in WriteQueue");

            while (!WriteQueue.IsEmpty)
            {
                Core.Packet p;
                bool success = WriteQueue.TryDequeue(out p);

                if (!success)
                {
                    Daemon.Logger.Debug("Connection.SendAll: failed to dequeue packet");
                    // TODO: find cases where this fails
                    return true;
                }

                if (p.Header.Length + Constants.PEERBLOOM_PACKET_HEADER_SIZE > Constants.MAX_PEERBLOOM_PACKET_SIZE)
                {
                    Daemon.Logger.Error($"Connection.SendAll: packet size ({p.Header.Length + Constants.PEERBLOOM_PACKET_HEADER_SIZE}) exceeds maximum allowed packet size ({Constants.MAX_PEERBLOOM_PACKET_SIZE}) ");
                    continue;
                }

                bool result = await SendAsync(p, token);

                if (!result)
                {
                    Daemon.Logger.Error($"Connection.SendAll: failed to send packet {p.Header.Command} to peer {Receiver}");

                    if (!_network.OutboundConnectedPeers.ContainsKey(Receiver) && !_network.InboundConnectedPeers.ContainsKey(Receiver))
                    {
                        return false;
                    }

                    continue;
                }
            }

            return true;
        }

        public async Task<Core.Packet> ReadAsync(CancellationToken token = default)
        {
            Daemon.Logger.Debug($"Connection.ReadAsync: beginning to receive packet from peer {Receiver}");

            Core.Packet p = null;

            try
            {
                var _lockSucceeded = await _readMutex.WaitAsync(1000);

                if (!_lockSucceeded)
                {
                    throw new Exception($"failed to retrieve access to the connection for peer {Receiver} (Mutex error)");
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"Connection.ReadAsync: {ex.Message}", ex);
                return null;
            }

            try
            {
                var _timeout = DateTime.UtcNow.AddMilliseconds(Constants.CONNECTION_READ_TIMEOUT).Ticks;

                byte[] _header = new byte[Constants.PEERBLOOM_PACKET_HEADER_SIZE];
                int numReadBytes = 0;

                do
                {
                    var readHeaderHandle = _tcpClient.GetStream().BeginRead(_header, numReadBytes, _header.Length - numReadBytes, null, null);

                    while (!readHeaderHandle.IsCompleted && _timeout > DateTime.UtcNow.Ticks && !token.IsCancellationRequested)
                    {
                        await Task.Delay(50, token);
                    }

                    if (!readHeaderHandle.IsCompleted)
                    {
                        break;
                    }

                    numReadBytes += _tcpClient.GetStream().EndRead(readHeaderHandle);
                }
                while (numReadBytes < Constants.PEERBLOOM_PACKET_HEADER_SIZE && _timeout > DateTime.UtcNow.Ticks && !token.IsCancellationRequested);
                
                if (numReadBytes < Constants.PEERBLOOM_PACKET_HEADER_SIZE)
                {
                    throw new Exception($"received {numReadBytes} during read (less than header length)"); 
                }

                if (_timeout <= DateTime.UtcNow.Ticks)
                {
                    throw new Exception($"failed to receive header from {Receiver} due to timeout");
                }

                Core.PacketHeader header = new(_header);

                if (header.NetworkID != Daemon.DaemonConfig.GetConfig().NetworkID)
                {
                    throw new Exception($"wrong network ID; expected {Daemon.DaemonConfig.GetConfig().NetworkID} but got {header.NetworkID}");
                }

                if ((header.Length + Constants.PEERBLOOM_PACKET_HEADER_SIZE) > Constants.MAX_PEERBLOOM_PACKET_SIZE)
                {
                    throw new Exception($"received packet was larger than allowed {Constants.MAX_PEERBLOOM_PACKET_SIZE} bytes.");
                }

                byte[] _data = new byte[header.Length];
                numReadBytes -= Constants.PEERBLOOM_PACKET_HEADER_SIZE;

                do
                {
                    var readHandle = _tcpClient.GetStream().BeginRead(_data, numReadBytes, _data.Length - numReadBytes, null, null);

                    while (!readHandle.IsCompleted && _timeout > DateTime.UtcNow.Ticks && !token.IsCancellationRequested)
                    {
                        await Task.Delay(50, token);
                    }

                    if (!readHandle.IsCompleted)
                    {
                        break;
                    }

                    numReadBytes += _tcpClient.GetStream().EndRead(readHandle);
                }
                while (numReadBytes < header.Length && _timeout > DateTime.UtcNow.Ticks && !token.IsCancellationRequested);

                if (numReadBytes < header.Length)
                {
                    throw new Exception($"received {numReadBytes} during read (less than specified packet body length in header, which was {header.Length})");
                }

                if (_timeout <= DateTime.UtcNow.Ticks)
                {
                    throw new Exception($"failed to receive header from {Receiver} due to timeout");
                }

                try
                {
                    p = new Core.Packet();

                    p.Header = header;
                    p.Body = Core.Packet.DecodePacketBody(header.Command, _data, 0);
                }
                catch (Exception)
                {
                    p = null;
                    throw new Exception($"malformed packet received from {Receiver}");
                }
            }
            catch (SocketException e)
            {
                Daemon.Logger.Error($"Connection.ReadAsync: a socket exception was encountered with peer {Receiver}: {e.Message}", e);
            }
            catch (InvalidOperationException)
            {
                Daemon.Logger.Error($"Connection.ReadAsync: socket with peer {Receiver} was unexpectedly closed.");
                _network.RemoveNodeFromPool(this);
            }
            catch (IOException e)
            {
                Daemon.Logger.Error($"Connection.ReadAsync: an IO exception was encountered with peer {Receiver}: {e.Message}", e);
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"Connection.ReadAsync: {ex.Message}", ex);
            }

            if (p != null)
            {
                if (!disposed)
                {
                    _readMutex.Release();
                }
                LastValidReceive = DateTime.UtcNow.Ticks;
                return p;
            }

            try
            {
                await _tcpClient.GetStream().FlushAsync(token);
                if (!ConnectionAcknowledged)
                {
                    Daemon.Logger.Info($"Connection.ReadAsync: failed to read from unacknowledged peer {Receiver}; ending connection");
                    _network.RemoveNodeFromPool(this);
                }
            }
            catch
            {
                Daemon.Logger.Error($"Connection.ReadAsync: failed to flush stream after error with peer {Receiver}; ending connection");
                _network.RemoveNodeFromPool(this);
            }

            if (!disposed)
            {
                _readMutex.Release();
            }
            return null;
        }

        public async Task Persistent(CancellationToken token)
        {
            _ = Task.Run(() => PersistentRead(token)).ConfigureAwait(false);
            _ = Task.Run(() => PersistentWrite(token)).ConfigureAwait(false);
        }

        private async Task PersistentRead(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_tcpClient.GetStream().DataAvailable)
                    {
                        var p = await ReadAsync(token);

                        if (p == null)
                        {
                            if (!_network.OutboundConnectedPeers.ContainsKey(Receiver) && !_network.InboundConnectedPeers.ContainsKey(Receiver) && !_network.ConnectingPeers.ContainsKey(Receiver))
                            {
                                Daemon.Logger.Error($"Connection.Persistent: An error was encountered during read from peer {Receiver}; ending connection");
                                Dispose();
                                return;
                            }
                            else if (!ConnectionAcknowledged && !IsOutbound)
                            {
                                Daemon.Logger.Error($"Connection.Persistent: malformed packet received from non-acknowledged inbound peer {Receiver}; ending connection");
                                Dispose();
                                return;
                            }
                        }
                        else
                        {
                            Daemon.Logger.Debug("Connection.Persistent: queueing data");
                            _network.AddPacketToQueue(p, this);
                        }
                    }

                    await Task.Delay(100, token);

                }
                catch (Exception ex)
                {
                    Daemon.Logger.Error($"Connection.Persistent: {ex.Message}; ending connection", ex);
                    Dispose();
                    break;
                }
            }
        }

        private async Task PersistentWrite(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!WriteQueue.IsEmpty)
                    {
                        bool res = await SendAll(token);

                        if (!res)
                        {
                            Daemon.Logger.Error($"Connection.Persistent: An error was encountered during write to peer {Receiver}; ending connection");
                            _tcpClient.Dispose();
                            return;
                        }
                    }

                    await Task.Delay(100, token);

                }
                catch (Exception ex)
                {
                    Daemon.Logger.Error($"Connection.Persistent: {ex.Message}; ending connection", ex);
                    _tcpClient.Dispose();
                    break;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Daemon.Logger.Debug($"Connection.Dispose: disposing of connection to peer {Receiver}");
                    _tcpClient.Dispose();
                    _readMutex.Dispose();
                    _sendMutex.Dispose();
                    _network.RemoveNodeFromPool(this);
                    _network.Cache.BadVersions.TryRemove(Receiver, out _);
                    _network.Cache.Versions.TryRemove(Receiver, out _);
                }

                disposed = true;
            }
        }

        public async Task Disconnect(bool notify, Core.Packets.Peerbloom.DisconnectCode code = Core.Packets.Peerbloom.DisconnectCode.CLEAN)
        {
            if (notify)
            {
                bool success = await SendAsync(new Core.Packet(Core.PacketType.DISCONNECT, new Core.Packets.Peerbloom.Disconnect { Code = code }));
                if (!success)
                {
                    Daemon.Logger.Error($"Connection.Disconnect: could not send disconnect packet to peer {Receiver}");
                }
                // delay the disconnect to ensure the packet reaches the peer
                await Task.Delay(500);
            }

            Dispose();
        }

        ~Connection()
        {
            Dispose(false);
        }
    }
}
