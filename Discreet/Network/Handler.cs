using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discreet.Network.Core;
using Discreet.Network.Core.Packets;

namespace Discreet.Network
{
    public class TransactionReceivedEventArgs : EventArgs
    {
        public Coin.FullTransaction Tx;
        public bool Success;
    }

    public class BlockSuccessEventArgs: EventArgs
    {
        public Coin.Block Block;
    }

    public delegate void TransactionReceivedEventHandler(TransactionReceivedEventArgs e);

    public delegate void BlockSuccessEventHandler(BlockSuccessEventArgs e);

    public class Handler
    {
        public PeerState State { get; private set; }
        public ServicesFlag Services { get; private set; }

        public ulong PingID { get; set; }

        private static Handler _handler;

        private static object handler_lock = new object();

        private Peerbloom.Network _network;

        private CancellationToken _token;

        public long LastSeenHeight { get; set; }

        public ConcurrentQueue<(Core.Packet, Peerbloom.Connection)> InboundPacketQueue = new();

        /* back reference to the Daemon */
        public Daemon.Daemon daemon;

        public event TransactionReceivedEventHandler OnTransactionReceived;
        public event BlockSuccessEventHandler OnBlockSuccess;

        public static Handler GetHandler()
        {
            lock (handler_lock)
            {
                /* should not be called prior to the handler being set */
                if (_handler == null) return null;

                return _handler;
            }
        }

        public static Handler Initialize(Peerbloom.Network network)
        {
            lock (handler_lock)
            {
                if (_handler == null)
                {
                    _handler = new Handler(network);
                }
            }

            return _handler;
        }

        public Handler(Peerbloom.Network network) 
        {
            State = PeerState.Startup;

            Services = ServicesFlag.Full;

            LastSeenHeight = -1;

            _network = network;
        }

        public void SetState(PeerState state)
        {
            State = state;
        }

        public void SetServices(ServicesFlag flag)
        {
            Services |= flag;
        }

        public void Start(CancellationToken token)
        {
            _token = token;
            _ = Task.Run(() => Handle(token)).ConfigureAwait(false);
        }

        public async Task Handle(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                while (!InboundPacketQueue.IsEmpty)
                {
                    bool success = InboundPacketQueue.TryDequeue(out var packet);

                    if (success)
                    {
                        await Handle(packet.Item1, packet.Item2);
                    }
                }

                await Task.Delay(100, token);
            }
        }

        public void AddPacketToQueue(Packet p, Peerbloom.Connection conn)
        {
            InboundPacketQueue.Enqueue((p, conn));
        }

        /* handles incoming packets */
        public async Task Handle(Packet p, Peerbloom.Connection conn)
        {
            try
            {
                if (conn == null)
                {
                    throw new Exception("null connection!");
                }

                if (State == PeerState.Startup && p.Header.Command != PacketType.VERSION)
                {
                    Daemon.Logger.Warn($"Ignoring message from {conn.Receiver} during startup");
                }

                Daemon.Logger.Info($"Discreet.Network.Handler.Handle: received packet {p.Header.Command} from {conn.Receiver}");

                /* Packet header and structure is already verified prior to this function. */
                switch (p.Header.Command)
                {
                    case PacketType.ALERT:
                        await HandleAlert((AlertPacket)p.Body);
                        break;
                    case PacketType.NONE:
                        Daemon.Logger.Error($"Discreet.Network.Handler.Handle: invalid packet with type NONE found");
                        break;
                    case PacketType.REJECT:
                        await HandleReject((RejectPacket)p.Body);
                        break;
                    case PacketType.VERSION:
                        await HandleVersion((Core.Packets.Peerbloom.VersionPacket)p.Body, conn);
                        break;
                    case PacketType.VERACK:
                        await HandleVerAck((Core.Packets.Peerbloom.VerAck)p.Body, conn);
                        break;
                    case PacketType.GETBLOCKS:
                        await HandleGetBlocks((GetBlocksPacket)p.Body, conn.Receiver);
                        break;
                    case PacketType.SENDMSG:
                        await HandleMessage((SendMessagePacket)p.Body, conn.Receiver);
                        break;
                    case PacketType.SENDTX:
                        await HandleSendTx((SendTransactionPacket)p.Body, conn.Receiver);
                        break;
                    case PacketType.SENDBLOCK:
                        await HandleSendBlock((SendBlockPacket)p.Body, conn.Receiver);
                        break;
                    case PacketType.INVENTORY:
                        /* currently unused */
                        break;
                    case PacketType.GETTXS:
                        await HandleGetTxs((GetTransactionsPacket)p.Body, conn.Receiver);
                        break;
                    case PacketType.TXS:
                        /* currently there is no handler for this packet */
                        break;
                    case PacketType.BLOCKS:
                        await HandleBlocks((BlocksPacket)p.Body);
                        break;
                    case PacketType.NOTFOUND:
                        await HandleNotFound((NotFoundPacket)p.Body);
                        break;
                    case PacketType.NETPING:
                        await HandleNetPing((Core.Packets.Peerbloom.NetPing)p.Body, conn);
                        break;
                    case PacketType.NETPONG:
                        await HandleNetPong((Core.Packets.Peerbloom.NetPong)p.Body, conn);
                        break;
                    case PacketType.REQUESTPEERS:
                        await HandleRequestPeers((Core.Packets.Peerbloom.RequestPeers)p.Body, conn.Receiver);
                        break;
                    case PacketType.REQUESTPEERSRESP:
                        await HandleRequestPeersResp((Core.Packets.Peerbloom.RequestPeersResp)p.Body, conn);
                        break;
                    default:
                        Daemon.Logger.Error($"Discreet.Network.Handler.Handle: received unsupported packet from {conn.Receiver} with type {p.Header.Command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"Handler.Handle: handling packet {p.Header.Command} got an exception: {ex.Message}");
            }
        }

        public async Task HandleNetPing(Core.Packets.Peerbloom.NetPing p, Peerbloom.Connection conn)
        {
            _network.Send(conn, new Packet(PacketType.NETPONG, new Core.Packets.Peerbloom.NetPong { Data = p.Data }));
        }

        public async Task HandleNetPong(Core.Packets.Peerbloom.NetPong p, Peerbloom.Connection conn)
        {
            if (!conn.WasPinged)
            {
                Daemon.Logger.Warn($"HandleNetPong: received unsolicited pong from peer {conn.Receiver}");
                return;
            }

            if (p.Data == null || p.Data.Length != 8)
            {
                Daemon.Logger.Error($"HandleNetPong: peer {conn.Receiver} responded with missing or malformed pong");
                return;
            }

            if (p.Data == null || p.Data.Length != 8 || Coin.Serialization.GetUInt64(p.Data, 0) != PingID)
            {
                Daemon.Logger.Warn($"HandleNetPong: ping ID mismatch for peer {conn.Receiver}: expected {PingID}, but got {Coin.Serialization.GetUInt64(p.Data, 0)}");
                return;
            }

            conn.PingLatency = DateTime.UtcNow.Ticks - conn.PingStart;

            var start = new DateTime(conn.PingStart).ToLocalTime().ToString("hh:mm:ss.fff tt");
            var end = DateTime.Now.ToString("hh:mm:ss.fff tt");
            var latency = conn.PingLatency / 10000;
            Daemon.Logger.Info($"Peer {conn.Receiver} pinged at {start} responded at {end} with latency {latency} ms");

            // reset ping status
            conn.WasPinged = false;
            conn.PingStart = 0;
        }

        public async Task HandleRequestPeers(Core.Packets.Peerbloom.RequestPeers p, IPEndPoint endpoint)
        {
            Daemon.Logger.Info($"Received `RequestPeers` from: {endpoint}");

            var nodes = _network.GetPeers(p.MaxPeers);
            Core.Packets.Peerbloom.RequestPeersResp respBody = new Core.Packets.Peerbloom.RequestPeersResp { Length = nodes.Count, Elems = new Core.Packets.Peerbloom.FindNodeRespElem[nodes.Count] };

            for (int i = 0; i < nodes.Count; i++)
            {
                respBody.Elems[i] = new Core.Packets.Peerbloom.FindNodeRespElem(nodes[i]);
            }

            Packet resp = new Packet(PacketType.REQUESTPEERSRESP, respBody);

            _network.Send(endpoint, resp);
        }

        public async Task HandleRequestPeersResp(Core.Packets.Peerbloom.RequestPeersResp p, Peerbloom.Connection conn)
        {
            foreach (var endpoint in p.Elems.Select(x => x.Endpoint))
            {
                if (conn.Receiver.Port < 49152 && !_network.ReflectedAddress.Equals(endpoint.Address))
                {
                    _network.peerlist.AddNew(endpoint, conn.Receiver, 60L * 60L * 10_000_000L);
                }
            }
        }

        public async Task HandleAlert(AlertPacket p)
        {
            var _checksum = Cipher.SHA256.HashData(Cipher.SHA256.HashData(Encoding.UTF8.GetBytes(p.Message)).Bytes);
            if (_checksum != p.Checksum)
            {
                Daemon.Logger.Error($"Discreet.Network.Handler.HandleAlert: alert received with invalid checksum ({p.Checksum.ToHexShort()} != {_checksum.ToHexShort()})");
                return;
            }

            if (!p.Sig.Verify(p.Checksum))
            {
                Daemon.Logger.Error($"Discreet.Network.Handler.HandleAlert: alert received with invalid signature \"{p.Sig.ToHexShort()}\"");
                return;
            }

            Daemon.Logger.Info($"Alert received from {p.Sig.y.ToHexShort()}: {p.Message}");

            MessageCache.GetMessageCache().Alerts.Add(p);
        }

        public async Task HandleReject(RejectPacket p)
        {
            var mCache = MessageCache.GetMessageCache();

            mCache.Rejections.Add(p);

            string valueString = null;

            if (p.Data != null && p.Data.Length > 0)
            {
                valueString = Common.Printable.Hexify(p.Data);

                if (valueString.Length > 8)
                {
                    valueString = valueString.Substring(0, 8);
                    valueString += "...";
                }
            }

            Daemon.Logger.Error($"Packet {p.RejectedType} {(valueString == null ? "" : $"(data { valueString})")} was rejected with code {p.Code} {(p.Reason == null || p.Reason.Length == 0 ? "" : ": " + p.Reason)}");
        }

        public Core.Packets.Peerbloom.VersionPacket MakeVersionPacket()
        {
            return new Core.Packets.Peerbloom.VersionPacket
            {
                Version = Daemon.DaemonConfig.GetConfig().NetworkVersion.Value,
                Services = Services,
                Timestamp = DateTime.UtcNow.Ticks,
                Height = DB.DisDB.GetDB().GetChainHeight(),
                Port = Daemon.DaemonConfig.GetConfig().Port.Value,
                Syncing = State == PeerState.Syncing
            };
        }

        public async Task HandleVersion(Core.Packets.Peerbloom.VersionPacket p, Peerbloom.Connection conn)
        {
            var mCache = MessageCache.GetMessageCache();

            if (mCache.Versions.ContainsKey(conn.Receiver))
            {
                Daemon.Logger.Warn($"Discreet.Network.Handler.HandleVersion: already received version from {conn.Receiver}");
            }
            else
            {
                if (p.Version != Daemon.DaemonConfig.GetConfig().NetworkVersion)
                {
                    Daemon.Logger.Error($"Discreet.Network.Handler.HandleVersion: Bad network version for peer {conn.Receiver}; expected {Daemon.DaemonConfig.GetConfig().NetworkVersion}, but got {p.Version}");
                    mCache.BadVersions[conn.Receiver] = p;
                    return;
                }

                if (p.Timestamp > DateTime.UtcNow.Add(TimeSpan.FromHours(2)).Ticks || p.Timestamp < DateTime.UtcNow.Subtract(TimeSpan.FromHours(2)).Ticks)
                {
                    Daemon.Logger.Error($"Discreet.Network.Handler.HandleVersion: version packet timestamp for peer {conn.Receiver} is either too old or too far in the future!");
                    mCache.BadVersions[conn.Receiver] = p;
                    return;
                }

                if (p.Port < 0 || p.Port > 65535)
                {
                    Daemon.Logger.Warn($"Discreet.Network.Handler.HandleVersion: version packet for peer {conn.Receiver} specified an invalid port ({p.Port})");
                    mCache.BadVersions[conn.Receiver] = p;
                    return;
                }

                conn.Port = p.Port;

                if (mCache.BadVersions.ContainsKey(conn.Receiver))
                {
                    mCache.BadVersions.Remove(conn.Receiver, out _);
                }

                if (!mCache.Versions.TryAdd(conn.Receiver, p))
                {
                    Daemon.Logger.Error($"Discreet.Network.Handler.HandleVersion: failed to add version for {conn.Receiver}");
                }
            }

            /* we reply with our own version */
            _network.Send(conn, new Packet(PacketType.VERSION, MakeVersionPacket()));
        }

        public async Task HandleVerAck(Core.Packets.Peerbloom.VerAck p, Peerbloom.Connection conn)
        {
            if (p.Counter > 0) return;

            // we use counter == -1 for a test connection; we simply send back 1 as usual but don't persist the connection
            if (p.Counter == -1)
            {
                p.ReflectedEndpoint = conn.Receiver;
                p.Counter = 1;

                conn.SetConnectionAcknowledged();
                _network.Send(conn, new Packet(PacketType.VERACK, p));

                _network.ConnectingPeers.Remove(conn.Receiver, out _);

                return;
            }

            if (p.Counter < 0) return; // something has gone wrong if this is true.

            p.ReflectedEndpoint = conn.Receiver;
            p.Counter++;

            conn.SetConnectionAcknowledged();
            _network.IncomingTester.Enqueue(new IPEndPoint(conn.Receiver.Address, conn.Port));

            _network.Send(conn, new Packet(PacketType.VERACK, p));
            _network.AddInboundConnection(conn);
        }

        public async Task HandleGetBlocks(GetBlocksPacket p, IPEndPoint senderEndpoint)
        {
            DB.DisDB db = DB.DisDB.GetDB();

            List<Coin.Block> blocks = new List<Coin.Block>();

            try
            {
                foreach (var h in p.Blocks)
                {
                    if (h.IsLong())
                    {
                        blocks.Add(db.GetBlock(h.ToInt64()));
                    }
                    else
                    {
                        blocks.Add(db.GetBlock(h));
                    }
                }

                Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.BLOCKS, new BlocksPacket { BlocksLen = (uint)blocks.Count, Blocks = blocks.ToArray() }));
            }
            catch (Exception e)
            {
                Daemon.Logger.Log(e.Message);

                NotFoundPacket resp = new NotFoundPacket
                {
                    Count = p.Count,
                    Inventory = new InventoryVector[p.Count],
                };
                
                for (int i = 0; i < p.Count; i++)
                {
                    resp.Inventory[i] = new InventoryVector { Hash = p.Blocks[i], Type = ObjectType.Block };
                }

                Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.NOTFOUND, resp));
            }
        }

        public async Task HandleGetTxs(GetTransactionsPacket p, IPEndPoint senderEndpoint)
        {
            DB.DisDB db = DB.DisDB.GetDB();

            List<Coin.FullTransaction> txs = new List<Coin.FullTransaction>();

            try
            {
                foreach (var h in p.Transactions)
                {
                    if (h.IsLong())
                    {
                        txs.Add(db.GetTransaction(h.ToUInt64()));
                    }
                    else
                    {
                        try
                        {
                            txs.Add(db.GetTransaction(h));
                        }
                        catch
                        {
                            txs.Add(db.GetTXFromPool(h));
                        }
                    }
                }

                Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.TXS, new TransactionsPacket { TxsLen = (uint)txs.Count, Txs = txs.ToArray() }));
            }
            catch (Exception e)
            {
                Daemon.Logger.Log(e.Message);

                NotFoundPacket resp = new NotFoundPacket
                {
                    Count = p.Count,
                    Inventory = new InventoryVector[p.Count],
                };

                for (int i = 0; i < p.Count; i++)
                {
                    resp.Inventory[i] = new InventoryVector { Hash = p.Transactions[i], Type = ObjectType.Transaction };
                }

                Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.NOTFOUND, resp));
            }
        }

        public async Task HandleMessage(SendMessagePacket p, IPEndPoint senderEndpoint)
        {
            if (!MessageCache.GetMessageCache().Messages.Contains(p.Message))
            {
                Daemon.Logger.Info($"Message received from {senderEndpoint}: {p.Message}");

                MessageCache.GetMessageCache().Messages.Add(p.Message);
            }
        }

        public async Task HandleSendTx(SendTransactionPacket p, IPEndPoint senderEndpoint)
        {
            if (p.Error != null && p.Error != "")
            {
                RejectPacket resp = new RejectPacket
                {
                    RejectedType = PacketType.SENDTX,
                    Reason = p.Error,
                    ReasonLen = (uint)Encoding.UTF8.GetBytes(p.Error).Length,
                    DataLen = 0,
                    Data = Array.Empty<byte>(),
                    Code = RejectionCode.MALFORMED,
                };

                Daemon.Logger.Error($"Malformed transaction received from peer {senderEndpoint}: {p.Error}");

                Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.REJECT, resp));
                return;
            }

            var txhash = p.Tx.Hash();

            /* sometimes a SendTx can occur as propagation for a recently added block */
            if (DB.DisDB.GetDB().ContainsTransaction(txhash))
            {
                Daemon.Logger.Debug($"HandleSendTx: Transaction received was in a previous block");
                return;
            }

            if (!Daemon.TXPool.GetTXPool().Contains(p.Tx.Hash()) && !DB.DisDB.GetDB().TXPoolContains(p.Tx.Hash()))
            {
                var err = Daemon.TXPool.GetTXPool().ProcessIncoming(p.Tx);

                if (err != null)
                {
                    RejectPacket resp = new RejectPacket
                    {
                        RejectedType = PacketType.SENDTX,
                        Reason = err.Message,
                        ReasonLen = (uint)Encoding.UTF8.GetBytes(err.Message).Length,
                        DataLen = 32,
                        Data = p.Tx.Hash().Bytes,
                        Code = RejectionCode.INVALID,
                    };

                    Daemon.Logger.Error($"Malformed transaction received from peer {senderEndpoint}: {err.Message}");

                    Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.REJECT, resp));
                    return;
                }
                else
                {
                    Peerbloom.Network.GetNetwork().Broadcast(new Packet(PacketType.SENDTX, p));
                }

                // the event is raised only if (1) tx hasn't been seen before and (2) tx isn't malformed.
                OnTransactionReceived?.Invoke(new TransactionReceivedEventArgs { Tx = p.Tx, Success = err == null });

                return;
            }
        }

        public async Task HandleSendBlock(SendBlockPacket p, IPEndPoint senderEndpoint)
        {
            if (p.Error != null && p.Error != "")
            {
                RejectPacket resp = new RejectPacket
                {
                    RejectedType = PacketType.SENDBLOCK,
                    Reason = p.Error,
                    ReasonLen = (uint)Encoding.UTF8.GetBytes(p.Error).Length,
                    DataLen = 0,
                    Data = Array.Empty<byte>(),
                    Code = RejectionCode.MALFORMED,
                };

                Daemon.Logger.Error($"Malformed block received from peer {senderEndpoint}: {p.Error}");

                Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.REJECT, resp));
                return;
            }

            if (State == PeerState.Syncing)
            {
                if (!DB.DisDB.GetDB().BlockCacheHas(p.Block.Header.BlockHash))
                {
                    try
                    {
                        lock (DB.DisDB.DBLock)
                        {
                            DB.DisDB.GetDB().AddBlockToCache(p.Block);
                        }

                        Peerbloom.Network.GetNetwork().Broadcast(new Packet(PacketType.SENDBLOCK, p));
                        return;
                    }
                    catch (Exception e)
                    {
                        RejectPacket resp = new RejectPacket
                        {
                            RejectedType = PacketType.SENDBLOCK,
                            Reason = e.Message,
                            ReasonLen = (uint)Encoding.UTF8.GetBytes(e.Message).Length,
                            DataLen = 32,
                            Data = p.Block.Header.BlockHash.Bytes,
                            Code = RejectionCode.INVALID,
                        };

                        Daemon.Logger.Error($"Malformed block received from peer {senderEndpoint}: {e.Message}");

                        Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.REJECT, resp));
                        return;
                    }
                }

                LastSeenHeight = p.Block.Header.Height;
            }
            else if (State == PeerState.Processing)
            {
                MessageCache.GetMessageCache().BlockCache[p.Block.Header.Height] = p.Block;

                Peerbloom.Network.GetNetwork().Broadcast(new Packet(PacketType.SENDBLOCK, p));
                return;
            }
            else
            {
                try
                {
                    DB.DisDB.GetDB().GetBlockHeight(p.Block.Header.BlockHash);
                }
                catch 
                {
                    var err = p.Block.Verify();

                    if (err != null)
                    {
                        RejectPacket resp = new RejectPacket
                        {
                            RejectedType = PacketType.SENDBLOCK,
                            Reason = err.Message,
                            ReasonLen = (uint)Encoding.UTF8.GetBytes(err.Message).Length,
                            DataLen = 32,
                            Data = p.Block.Header.BlockHash.Bytes,
                            Code = RejectionCode.INVALID,
                        };

                        Daemon.Logger.Error($"Malformed block received from peer {senderEndpoint}: {err.Message}");

                        Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.REJECT, resp));
                        return;
                    }

                    Peerbloom.Network.GetNetwork().Broadcast(new Packet(PacketType.SENDBLOCK, p));

                    DB.DisDB db = DB.DisDB.GetDB();

                    try
                    {
                        lock (DB.DisDB.DBLock)
                        {
                            db.AddBlock(p.Block);
                        }
                    }
                    catch (Exception e)
                    {
                        Daemon.Logger.Error(new Common.Exceptions.DatabaseException("Discreet.Daemon.Daemon.ProcessBlock", e.Message).Message);
                    }

                    try
                    {
                        daemon.ProcessBlock(p.Block);
                    }
                    catch (Exception e)
                    {
                        Daemon.Logger.Error(e.Message);
                    }

                    OnBlockSuccess?.Invoke(new BlockSuccessEventArgs { Block = p.Block });
                }
            }
        }

        public async Task HandleBlocks(BlocksPacket p)
        {
            if (State == PeerState.Syncing)
            {
                DB.DisDB db = DB.DisDB.GetDB();

                foreach (var block in p.Blocks)
                {
                    if (!db.BlockCacheHas(block.Header.BlockHash))
                    {
                        lock (DB.DisDB.DBLock)
                        {
                            db.AddBlockToCache(block);
                        }
                    }
                    if (!MessageCache.GetMessageCache().BlockCache.ContainsKey(block.Header.Height))
                    {
                        MessageCache.GetMessageCache().BlockCache.TryAdd(block.Header.Height, block);
                    }
                }

                LastSeenHeight = p.Blocks.Select(x => x.Header.Height).Max();
            }
        }

        public async Task HandleNotFound(NotFoundPacket p)
        {
            string items = "";

            for (int i = 0; i < p.Count; i++)
            {
                var h = p.Inventory[i];
                items += (h.Type == ObjectType.Transaction ? "Tx " : "Block ") + (h.Hash.IsLong() ? h.Hash.ToUInt64().ToString() : h.Hash.ToHexShort());

                if (i < p.Count - 1)
                {
                    items += ", ";
                }
            }

            Daemon.Logger.Error($"Could not find objects: {items}");
        }

        public void Handle(string s)
        {
            Daemon.Logger.Log(s);
        }
    }
}
