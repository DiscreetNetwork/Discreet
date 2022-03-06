using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Discreet.Network.Core;
using Discreet.Network.Core.Packets;

namespace Discreet.Network
{
    public delegate void OnConnectEvent();

    public class Handler
    {
        public PeerState State { get; private set; }
        public ServicesFlag Services { get; private set; }

        private static Handler _handler;

        private static object handler_lock = new object();

        public long LastSeenHeight { get; set; }


        /* back reference to the Visor */
        public Visor.Visor visor;

        public static Handler GetHandler()
        {
            lock (handler_lock)
            {
                if (_handler == null) Initialize();

                return _handler;
            }
        }

        public static void Initialize()
        {
            lock (handler_lock)
            {
                if (_handler == null)
                {
                    _handler = new Handler();
                }
            }
        }

        public Handler() 
        {
            State = PeerState.Startup;

            Services = ServicesFlag.Full;

            LastSeenHeight = -1;
        }

        public void SetState(PeerState state)
        {
            State = state;
        }

        public void SetServices(ServicesFlag flag)
        {
            Services |= flag;
        }

        /* handles incoming packets */
        public async Task Handle(Packet p, Peerbloom.RemoteNode senderEndpoint)
        {
            if (senderEndpoint == null)
            {
                throw new Exception("null remote node!");
            }

            if (State == PeerState.Startup && p.Header.Command != PacketType.VERSION)
            {
                Visor.Logger.Log($"Ignoring message from {senderEndpoint.Endpoint} during startup");
            }

            Visor.Logger.Log($"Discreet.Network.Handler.Handle: received packet {p.Header.Command} from {senderEndpoint.Endpoint}");

            /* Packet header and structure is already verified prior to this function. */
            //WIP
            switch (p.Header.Command)
            {
                case PacketType.ALERT:
                    await HandleAlert((AlertPacket)p.Body);
                    break;
                case PacketType.CONNECT:
                case PacketType.CONNECTACK:
                case PacketType.FINDNODE:
                case PacketType.FINDNODERESP:
                case PacketType.NETPING:
                case PacketType.NETPONG:
                    Visor.Logger.Log($"Discreet.Network.Handler.Handle: invalid packet received with type {p.Header.Command}; should not be visible to handler");
                    break;
                case PacketType.NONE:
                    Visor.Logger.Log($"Discreet.Network.Handler.Handle: invalid packet with type NONE found");
                    break;
                case PacketType.REJECT:
                    await HandleReject((RejectPacket)p.Body);
                    break;
                case PacketType.GETVERSION:
                    await HandleGetVersion(senderEndpoint.Endpoint);
                    break;
                case PacketType.VERSION:
                    await HandleVersion((VersionPacket)p.Body, senderEndpoint.Endpoint);
                    break;
                case PacketType.GETBLOCKS:
                    await HandleGetBlocks((GetBlocksPacket)p.Body, senderEndpoint.Endpoint);
                    break;
                case PacketType.SENDMSG:
                    await HandleMessage((SendMessagePacket)p.Body, senderEndpoint.Endpoint);
                    break;
                case PacketType.SENDTX:
                    await HandleSendTx((SendTransactionPacket)p.Body, senderEndpoint.Endpoint);
                    break;
                case PacketType.SENDBLOCK:
                    await HandleSendBlock((SendBlockPacket)p.Body, senderEndpoint.Endpoint);
                    break;
                case PacketType.INVENTORY:
                    /* currently unused */
                    break;
                case PacketType.GETTXS:
                    await HandleGetTxs((GetTransactionsPacket)p.Body, senderEndpoint.Endpoint);
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
                default:
                    Visor.Logger.Log($"Discreet.Network.Handler.Handle: received unsupported packet from {senderEndpoint.Endpoint} with type {p.Header.Command}");
                    break;
            }
            /* REMOVE IN FUTURE; USED FOR TESTING */
            //if (p.Header.Command == Core.PacketType.OLDMESSAGE)
            //{
            //    var body = (Core.Packets.Peerbloom.OldMessage)p.Body;
            //
            //    Peerbloom.Network.GetNetwork().OnSendMessageReceived(body.MessageID, body.Message);
            //}
            //else
            //{
            //    Visor.Logger.Log($"Unknown/unimplemented packet type received: {p.Header.Command}");
            //    return;
            //}
        }

        public async Task HandleAlert(AlertPacket p)
        {
            var _checksum = Cipher.SHA256.HashData(Cipher.SHA256.HashData(Encoding.UTF8.GetBytes(p.Message)).Bytes);
            if (_checksum != p.Checksum)
            {
                Visor.Logger.Log($"Discreet.Network.Handler.HandleAlert: alert received with invalid checksum ({p.Checksum.ToHexShort()} != {_checksum.ToHexShort()})");
                return;
            }

            if (!p.Sig.Verify(p.Checksum))
            {
                Visor.Logger.Log($"Discreet.Network.Handler.HandleAlert: alert received with invalid signature \"{p.Sig.ToHexShort()}\"");
                return;
            }

            Visor.Logger.Log($"Alert received from {p.Sig.y.ToHexShort()}: {p.Message}");

            MessageCache.GetMessageCache().Alerts.Add(p);
        }

        public async Task HandleReject(RejectPacket p)
        {
            var mCache = MessageCache.GetMessageCache();

            mCache.Rejections.Add(p);
        }

        public VersionPacket MakeVersionPacket()
        {
            return new VersionPacket
            {
                Version = Visor.VisorConfig.GetConfig().NetworkVersion,
                Services = Services,
                Timestamp = DateTime.UtcNow.Ticks,
                Height = DB.DisDB.GetDB().GetChainHeight(),
                Address = Visor.VisorConfig.GetConfig().Endpoint,
                ID = Peerbloom.Network.GetNetwork().GetNodeID(),
                Syncing = State == PeerState.Syncing
            };
        }

        public async Task HandleGetVersion(IPEndPoint senderEndpoint)
        {
            VersionPacket vp = MakeVersionPacket();

            await Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.VERSION, vp));
        }

        public async Task HandleVersion(VersionPacket p, IPEndPoint senderEndpoint)
        {
            if (State != PeerState.Startup)
            {
                Visor.Logger.Log($"Discreet.Network.Handler.HandleVersion: received a version packet from peer {senderEndpoint} after startup; ignoring");
                return;
            }

            var mCache = MessageCache.GetMessageCache();

            if (mCache.Versions.ContainsKey(senderEndpoint))
            {
                Visor.Logger.Log($"Discreet.Network.Handler.HandleVersion: already received version from {senderEndpoint}");
                return;
            }

            /*if (senderEndpoint != p.Address)
            {
                Visor.Logger.Log($"Discreet.Network.Handler.HandleVersion: endpoint and address mismatch (received from {senderEndpoint}; specified {p.Address})");
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

            /*if (node.Id.Value != p.ID)
            {
                Visor.Logger.Log($"Discreet.Network.Handler.HandleVersion: invalid ID for peer {p.Address}; expected {node.Id.Value.ToHexShort()}, but got {p.ID.ToHexShort()}");
                mCache.BadVersions[senderEndpoint] = p;
                return;
            }*/

            if (p.Version != Visor.VisorConfig.GetConfig().NetworkVersion)
            {
                Visor.Logger.Log($"Discreet.Network.Handler.HandleVersion: Bad network version for peer {p.Address}; expected {Visor.VisorConfig.GetConfig().NetworkVersion}, but got {p.Version}");
                mCache.BadVersions[senderEndpoint] = p;
                return;
            }

            if (mCache.Versions.ContainsKey(senderEndpoint) || mCache.BadVersions.ContainsKey(senderEndpoint))
            {
                Visor.Logger.Log($"Discreet.Network.Handler.HandleVersion: version packet already recieved for peer {p.Address}");
                mCache.BadVersions[senderEndpoint] = p;
                return;
            }

            if (p.Timestamp > DateTime.UtcNow.Add(TimeSpan.FromHours(2)).Ticks || p.Timestamp < DateTime.UtcNow.Subtract(TimeSpan.FromHours(2)).Ticks)
            {
                Visor.Logger.Log($"Discreet.Network.Handler.HandleVersion: version packet timestamp for peer {p.Address} is either too old or too far in the future!");
                mCache.BadVersions[senderEndpoint] = p;
                return;
            }

            if (mCache.BadVersions.ContainsKey(senderEndpoint))
            {
                mCache.BadVersions.Remove(senderEndpoint, out _);
            }

            if (!mCache.Versions.TryAdd(senderEndpoint, p))
            {
                Visor.Logger.Log($"Discreet.Network.Handler.HandleVersion: failed to add version for {senderEndpoint}");
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

                await Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.BLOCKS, new BlocksPacket { BlocksLen = (uint)blocks.Count, Blocks = blocks.ToArray() }));
            }
            catch (Exception e)
            {
                Visor.Logger.Log(e.Message);

                NotFoundPacket resp = new NotFoundPacket
                {
                    Count = p.Count,
                    Inventory = new InventoryVector[p.Count],
                };
                
                for (int i = 0; i < p.Count; i++)
                {
                    resp.Inventory[i] = new InventoryVector { Hash = p.Blocks[i], Type = ObjectType.Block };
                }

                await Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.NOTFOUND, resp));
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

                await Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.TXS, new TransactionsPacket { TxsLen = (uint)txs.Count, Txs = txs.ToArray() }));
            }
            catch (Exception e)
            {
                Visor.Logger.Log(e.Message);

                NotFoundPacket resp = new NotFoundPacket
                {
                    Count = p.Count,
                    Inventory = new InventoryVector[p.Count],
                };

                for (int i = 0; i < p.Count; i++)
                {
                    resp.Inventory[i] = new InventoryVector { Hash = p.Transactions[i], Type = ObjectType.Transaction };
                }

                await Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.NOTFOUND, resp));
            }
        }

        public async Task HandleMessage(SendMessagePacket p, IPEndPoint senderEndpoint)
        {
            if (!MessageCache.GetMessageCache().Messages.Contains(p.Message))
            {
                Visor.Logger.Log($"Message received from {senderEndpoint}: {p.Message}");

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

                Visor.Logger.Log($"Malformed transaction received from peer {senderEndpoint}: {p.Error}");

                await Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.REJECT, resp));
                return;
            }

            if (!DB.DisDB.GetDB().TXPoolContains(p.Tx.Hash()))
            {
                var err = Visor.TXPool.GetTXPool().ProcessIncoming(p.Tx);

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

                    Visor.Logger.Log($"Malformed transaction received from peer {senderEndpoint}: {err.Message}");

                    await Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.REJECT, resp));
                    return;
                }

                await Peerbloom.Network.GetNetwork().Broadcast(new Packet(PacketType.SENDTX, p));
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

                Visor.Logger.Log($"Malformed block received from peer {senderEndpoint}: {p.Error}");

                await Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.REJECT, resp));
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

                        await Peerbloom.Network.GetNetwork().Broadcast(new Packet(PacketType.SENDBLOCK, p));
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

                        Visor.Logger.Log($"Malformed block received from peer {senderEndpoint}: {e.Message}");

                        await Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.REJECT, resp));
                        return;
                    }
                }

                LastSeenHeight = p.Block.Header.Height;
            }
            else if (State == PeerState.Processing)
            {
                MessageCache.GetMessageCache().BlockCache[p.Block.Header.Height] = p.Block;

                await Peerbloom.Network.GetNetwork().Broadcast(new Packet(PacketType.SENDBLOCK, p));
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

                        Visor.Logger.Log($"Malformed block received from peer {senderEndpoint}: {err.Message}");

                        await Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.REJECT, resp));
                        return;
                    }

                    /* broadcast can occur in the background */
                    _ = Peerbloom.Network.GetNetwork().Broadcast(new Packet(PacketType.SENDBLOCK, p));

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
                        Visor.Logger.Log(new Common.Exceptions.DatabaseException("Discreet.Visor.Visor.ProcessBlock", e.Message).Message);
                    }

                    try
                    {
                        visor.ProcessBlock(p.Block);
                    }
                    catch (Exception e)
                    {
                        Visor.Logger.Log(e.Message);
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

            Visor.Logger.Log($"Could not find objects: {items}");
        }

        public void Handle(string s)
        {
            Visor.Logger.Log(s);
        }
    }
}
