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
    public class Handler
    {
        public PeerState State { get; private set; }
        public ServicesFlag Services { get; private set; }

        private static Handler _handler;

        private static object handler_lock = new object();

        private Peerbloom.Network _network;

        public long LastSeenHeight { get; set; }

        public ConcurrentQueue<(Core.Packet, Peerbloom.Connection)> InboundPacketQueue = new();

        /* back reference to the Visor */
        public Daemon.Daemon daemon;

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
                    Daemon.Logger.Debug($"Handler..ctor: calling Initialize for handler!");
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
                case PacketType.CONNECT:
                    await HandleConnect(conn);
                    break;
                case PacketType.CONNECTACK:
                    Daemon.Logger.Error($"Discreet.Network.Handler.Handle: invalid packet received with type {p.Header.Command}; should not be visible to handler");
                    break;
                case PacketType.NONE:
                    Daemon.Logger.Error($"Discreet.Network.Handler.Handle: invalid packet with type NONE found");
                    break;
                case PacketType.REJECT:
                    await HandleReject((RejectPacket)p.Body);
                    break;
                case PacketType.GETVERSION:
                    await HandleGetVersion(conn.Receiver);
                    break;
                case PacketType.VERSION:
                    await HandleVersion((VersionPacket)p.Body, conn.Receiver);
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
                    await HandleNetPing((Core.Packets.Peerbloom.NetPing)p.Body);
                    break;
                case PacketType.NETPONG:
                    await HandleNetPong((Core.Packets.Peerbloom.NetPong)p.Body);
                    break;
                case PacketType.REQUESTPEERS:
                    await HandleRequestPeers((Core.Packets.Peerbloom.RequestPeers)p.Body, conn.Receiver);
                    break;
                case PacketType.REQUESTPEERSRESP:
                    await HandleRequestPeersResp((Core.Packets.Peerbloom.RequestPeersResp)p.Body);
                    break;
                default:
                    Daemon.Logger.Error($"Discreet.Network.Handler.Handle: received unsupported packet from {conn.Receiver} with type {p.Header.Command}");
                    break;
            }
        }

        public async Task HandleConnect(Peerbloom.Connection conn)
        {
            if (_network.IsBootstrapping)
            {
                Daemon.Logger.Warn($"Handler.HandleConnect: Ignoring connection request from peer {conn.Receiver} during bootstrapping");
                _network.RemoveNodeFromPool(conn);
                return;
            }

            if (_network.LocalNode.IsPublic)
            {
                if (!_network.ConnectingPeers.ContainsKey(conn.Receiver))
                {
                    Daemon.Logger.Error($"Handler.HandleConnect: cannot fulfill connection handshake for peer {conn.Receiver}; not currently connecting");
                    return;
                }

                bool success = await _network.SendAsync(conn, new Packet(PacketType.CONNECTACK, new Core.Packets.Peerbloom.ConnectAck { Acknowledged = true, IsPublic = false, ReflectedEndpoint = conn.Receiver }));

                if (!success)
                {
                    Daemon.Logger.Error($"Handler.HandleConnect: could not send connect ACK to peer {conn.Receiver}; ending connection");
                    _network.RemoveNodeFromPool(conn);
                    return;
                }

                Daemon.Logger.Info($"Handler.HandleConnect: successfully connected to peer {conn.Receiver}");
                conn.SetConnectionAcknowledged();

                _network.AddInboundConnection(conn);
            }
        }

        public async Task HandleNetPing(Core.Packets.Peerbloom.NetPing p)
        {
            throw new NotImplementedException();
        }

        public async Task HandleNetPong(Core.Packets.Peerbloom.NetPong p)
        {
            throw new NotImplementedException();
        }

        public async Task HandleRequestPeers(Core.Packets.Peerbloom.RequestPeers p, IPEndPoint endpoint)
        {
            Daemon.Logger.Info($"Received `RequestPeers` from: {endpoint.Address}");

            var nodes = _network.GetPeers(p.MaxPeers);
            Core.Packets.Peerbloom.RequestPeersResp respBody = new Core.Packets.Peerbloom.RequestPeersResp { Length = nodes.Count, Elems = new Core.Packets.Peerbloom.FindNodeRespElem[nodes.Count] };

            for (int i = 0; i < nodes.Count; i++)
            {
                respBody.Elems[i] = new Core.Packets.Peerbloom.FindNodeRespElem(nodes[i]);
            }

            Packet resp = new Packet(PacketType.REQUESTPEERSRESP, respBody);

            _network.Send(endpoint, resp);
        }

        public async Task HandleRequestPeersResp(Core.Packets.Peerbloom.RequestPeersResp p)
        {
            Daemon.Logger.Error($"Handler currently does not support RequestPeersResp ");
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

        public VersionPacket MakeVersionPacket()
        {
            return new VersionPacket
            {
                Version = Daemon.DaemonConfig.GetConfig().NetworkVersion.Value,
                Services = Services,
                Timestamp = DateTime.UtcNow.Ticks,
                Height = DB.DisDB.GetDB().GetChainHeight(),
                Address = Daemon.DaemonConfig.GetConfig().Endpoint,
                Syncing = State == PeerState.Syncing
            };
        }

        public async Task HandleGetVersion(IPEndPoint senderEndpoint)
        {
            VersionPacket vp = MakeVersionPacket();

            Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.VERSION, vp));
        }

        public async Task HandleVersion(VersionPacket p, IPEndPoint senderEndpoint)
        {
            if (State != PeerState.Startup)
            {
                Daemon.Logger.Warn($"Discreet.Network.Handler.HandleVersion: received a version packet from peer {senderEndpoint} after startup; ignoring");
                return;
            }

            var mCache = MessageCache.GetMessageCache();

            if (mCache.Versions.ContainsKey(senderEndpoint))
            {
                Daemon.Logger.Warn($"Discreet.Network.Handler.HandleVersion: already received version from {senderEndpoint}");
                return;
            }

            /*if (!senderEndpoint.Address.Equals(p.Address.Address))
            {
                Daemon.Logger.Log($"Discreet.Network.Handler.HandleVersion: endpoint and address mismatch (received from {senderEndpoint.Address}; specified {p.Address.Address})");
                mCache.BadVersions[senderEndpoint] = p;
                return;
            }*/

            /*var node = Peerbloom.Network.GetNetwork().GetNode(senderEndpoint);

            if (node != null)
            {
                Visor.Logger.Log($"Discreet.Network.Handler.HandleVersion: could not find peer {p.Address} in connection pool");
                mCache.BadVersions[senderEndpoint] = p;
                return;
            }*/

            if (p.Version != Daemon.DaemonConfig.GetConfig().NetworkVersion)
            {
                Daemon.Logger.Error($"Discreet.Network.Handler.HandleVersion: Bad network version for peer {p.Address}; expected {Daemon.DaemonConfig.GetConfig().NetworkVersion}, but got {p.Version}");
                mCache.BadVersions[senderEndpoint] = p;
                return;
            }

            if (mCache.Versions.ContainsKey(senderEndpoint) || mCache.BadVersions.ContainsKey(senderEndpoint))
            {
                Daemon.Logger.Warn($"Discreet.Network.Handler.HandleVersion: version packet already recieved for peer {p.Address}");
                mCache.BadVersions[senderEndpoint] = p;
                return;
            }

            if (p.Timestamp > DateTime.UtcNow.Add(TimeSpan.FromHours(2)).Ticks || p.Timestamp < DateTime.UtcNow.Subtract(TimeSpan.FromHours(2)).Ticks)
            {
                Daemon.Logger.Error($"Discreet.Network.Handler.HandleVersion: version packet timestamp for peer {p.Address} is either too old or too far in the future!");
                mCache.BadVersions[senderEndpoint] = p;
                return;
            }

            if (mCache.BadVersions.ContainsKey(senderEndpoint))
            {
                mCache.BadVersions.Remove(senderEndpoint, out _);
            }

            if (!mCache.Versions.TryAdd(senderEndpoint, p))
            {
                Daemon.Logger.Error($"Discreet.Network.Handler.HandleVersion: failed to add version for {senderEndpoint}");
            }
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

                Peerbloom.Network.GetNetwork().Broadcast(new Packet(PacketType.SENDTX, p));
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
                        Daemon.Logger.Error(new Common.Exceptions.DatabaseException("Discreet.Visor.Visor.ProcessBlock", e.Message).Message);
                    }

                    try
                    {
                        daemon.ProcessBlock(p.Block);
                    }
                    catch (Exception e)
                    {
                        Daemon.Logger.Error(e.Message);
                    }
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

                LastSeenHeight = p.Blocks[0].Header.Height;
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
