using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discreet.Coin.Models;
using Discreet.DB;
using Discreet.Network.Core;
using Discreet.Network.Core.Packets;

namespace Discreet.Network
{
    internal class InventoryComparer : IComparer<InventoryVectorRef>
    {
        public int Compare(InventoryVectorRef x, InventoryVectorRef y) => x.vector.Compare(y.vector);
    }

    internal class InventoryEqualityComparer : IEqualityComparer<InventoryVectorRef>
    {
        public bool Equals(InventoryVectorRef x, InventoryVectorRef y) => x.vector == y.vector && x.peer.Equals(y.peer);

        public int GetHashCode(InventoryVectorRef obj) => obj.vector.GetHashCode();
    }

    internal class InventoryPureEqualityComparer : IEqualityComparer<InventoryVectorRef>
    {
        public bool Equals(InventoryVectorRef x, InventoryVectorRef y) => x.vector == y.vector;

        public int GetHashCode(InventoryVectorRef obj) => obj.vector.GetHashCode();
    }

    internal class InventoryVectorRef
    {
        public InventoryVector vector;
        public HashSet<InventoryVectorRef> container;
        public Action<IPEndPoint, InventoryVector, bool, RequestCallbackContext> callback;
        public IPEndPoint peer;
        public FullTransaction tx = null;
        public Block block = null;
        public BlockHeader header = null;

        public InventoryVectorRef() { }

        public InventoryVectorRef(InventoryVector vector) { this.vector = vector; }

        public InventoryVectorRef(InventoryVector vector, HashSet<InventoryVectorRef> container) : this(vector) { this.container = container; }

        public InventoryVectorRef(InventoryVector vector, HashSet<InventoryVectorRef> container, Action<IPEndPoint, InventoryVector, bool, RequestCallbackContext> callback) : this(vector, container) { this.callback = callback; }

        public InventoryVectorRef(InventoryVector vector, HashSet<InventoryVectorRef> container, Action<IPEndPoint, InventoryVector, bool, RequestCallbackContext> callback, IPEndPoint peer) : this(vector, container, callback) { this.peer = peer; }
    }

    public class TransactionReceivedEventArgs : EventArgs
    {
        public FullTransaction Tx;
        public bool Success;
    }

    public class BlockSuccessEventArgs: EventArgs
    {
        public Block Block;
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

        private long _lastSeenHeight;
        public long LastSeenHeight { get { return _lastSeenHeight; } set { _lastSeenHeight = value; } }

        public ConcurrentQueue<(Core.Packet, Peerbloom.Connection)> InboundPacketQueue = new();

        /* back reference to the Daemon */
        public Daemon.Daemon daemon;

        public event TransactionReceivedEventHandler OnTransactionReceived;
        public event BlockSuccessEventHandler OnBlockSuccess;

        // used for unorganized inventory requests
        private ConcurrentDictionary<IPEndPoint, HashSet<InventoryVectorRef>> NeededInventory = new();
        private ConcurrentDictionary<InventoryVectorRef, (long, long)> InventoryTimeouts = new(Environment.ProcessorCount, 25000, new InventoryEqualityComparer());

        public async Task NeededInventoryStart(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(30000, token);

                var curTimestamp = DateTime.UtcNow.Ticks;

                // NeededInventory will remain small.
                foreach(var kv in InventoryTimeouts)
                {
                    // remove stale requests (i.e. requests older than one hour)
                    var dur = kv.Value.Item2;
                    if (dur <= 0)
                    {
                        dur = 3600_0_000_000L;
                    }
                    if (curTimestamp >=  dur + kv.Value.Item1)
                    {
                        kv.Key.container?.Remove(kv.Key);
                        kv.Key.callback?.Invoke(kv.Key.peer, kv.Key.vector, false, RequestCallbackContext.STALE);
                    }
                }
            }
        }

        public bool RegisterNeeded(IPacketBody packet, IPEndPoint req, long durMilliseconds = 0, Action<IPEndPoint, InventoryVector, bool, RequestCallbackContext> callback = null)
        {
            bool success = NeededInventory.TryGetValue(req, out var reqset);
            if (!success || reqset == null)
            {
                var len = packet.GetType() == typeof(GetTransactionsPacket) ? (packet as GetTransactionsPacket).Count :
                           (packet.GetType() == typeof(GetBlocksPacket) ? (packet as GetBlocksPacket).Count :
                            (packet.GetType() == typeof(GetHeadersPacket) ? (int)(packet as GetHeadersPacket).Count :
                             0));
                reqset = new((int)len, new InventoryPureEqualityComparer());
                NeededInventory.TryAdd(req, reqset);
            }

            var timestamp = DateTime.UtcNow.Ticks;
            var durTicks = durMilliseconds * 10_000L;
            bool needed = false;

            if (packet.GetType() == typeof(GetTransactionsPacket))
            {
                var gettx = packet as GetTransactionsPacket;
                var newGettxs = new List<Cipher.SHA256>();
                foreach (var tx in gettx.Transactions)
                {
                    var ivref = new InventoryVectorRef(new InventoryVector(ObjectType.Transaction, tx), reqset, callback, req);
                    if (!reqset.Contains(ivref))
                    {
                        newGettxs.Add(tx);
                        needed = true;
                        reqset.Add(ivref);
                        InventoryTimeouts.TryAdd(ivref, (timestamp, durTicks));
                    }
                }

                gettx.Transactions = newGettxs.ToArray();
            }
            else if (packet.GetType() == typeof(GetBlocksPacket))
            {
                var gettx = packet as GetBlocksPacket;
                var newGettxs = new List<Cipher.SHA256>();
                foreach (var block in gettx.Blocks)
                {
                    var ivref = new InventoryVectorRef(new InventoryVector(ObjectType.Block, block), reqset, callback, req);
                    if (!reqset.Contains(ivref))
                    {
                        newGettxs.Add(block);
                        needed = true;
                        reqset.Add(ivref);
                        InventoryTimeouts.TryAdd(ivref, (timestamp, durTicks));
                    }
                }

                gettx.Blocks = newGettxs.ToArray();
            }
            else if (packet.GetType() == typeof(GetHeadersPacket))
            {
                var gettx = packet as GetHeadersPacket;
                var newGettxs = new List<Cipher.SHA256>();
                if (gettx.Headers == null)
                {
                    for (long i = gettx.StartingHeight; i < gettx.StartingHeight + gettx.Count; i++)
                    {
                        var ivref = new InventoryVectorRef(new InventoryVector(ObjectType.BlockHeader, new Cipher.SHA256(i)), reqset, callback, req);
                        if (!reqset.Contains(ivref))
                        {
                            newGettxs.Add(new Cipher.SHA256(i));
                            needed = true;
                            reqset.Add(ivref);
                            InventoryTimeouts.TryAdd(ivref, (timestamp, durTicks));
                        }
                    }

                    if (newGettxs.Count == gettx.Count)
                    {
                        newGettxs = null;
                    }
                }
                else
                {
                    foreach (var header in gettx.Headers)
                    {
                        var ivref = new InventoryVectorRef(new InventoryVector(ObjectType.BlockHeader, header), reqset, callback, req);
                        if (!reqset.Contains(ivref))
                        {
                            newGettxs.Add(header);
                            needed = true;
                            reqset.Add(ivref);
                            InventoryTimeouts.TryAdd(ivref, (timestamp, durTicks));
                        }
                    }
                }

                if (newGettxs != null)
                {
                    gettx.Headers = newGettxs.ToArray();
                }
            }
            else
            {
                Daemon.Logger.Error($"Handler.RegisterNeeded: cannot accept packet of type {packet.GetType()}");
            }

            return needed;
        }

        internal (bool, List<InventoryVectorRef>) CheckFulfillment(IPacketBody packet, IPEndPoint resp)
        {
            bool success = NeededInventory.TryGetValue(resp, out var reqset);
            if (!success || reqset == null)
            {
                return (false, null);
            }

            if (packet.GetType() == typeof(TransactionsPacket))
            {
                var txs = packet as TransactionsPacket;
                var txinvs = txs.Txs.Select(x => new InventoryVectorRef(new InventoryVector(ObjectType.Transaction, x.TxID)) { tx = x});
                // it's best to assume the optimal case that all items are present
                List<InventoryVectorRef> fulfilled = new(capacity: txs.Txs.Length + 1);
                foreach (var txinv in txinvs)
                {
                    bool remsuccess = reqset.TryGetValue(txinv, out var trueinv);
                    if (!remsuccess || trueinv == null) return (false, null);
                    fulfilled.Add(trueinv);
                    trueinv.tx = txinv.tx;
                }

                fulfilled.ForEach(x =>
                {
                    reqset.Remove(x);
                    InventoryTimeouts.Remove(x, out _);
                });
                return (true, fulfilled);
            }
            else if (packet.GetType() == typeof(BlocksPacket))
            {
                var blocks = packet as BlocksPacket;
                List<InventoryVectorRef> fulfilled = new(capacity: blocks.Blocks.Length + 1);

                foreach (Block block in blocks.Blocks)
                {
                    var binv = new InventoryVectorRef(new InventoryVector(ObjectType.Block, block.Header.BlockHash)) { block = block };
                    bool remsuccess = reqset.TryGetValue(binv, out var trueinv);
                    if (!remsuccess || trueinv == null)
                    {
                        binv = new InventoryVectorRef(new InventoryVector(ObjectType.Block, new Cipher.SHA256(block.Header.Height))) { block = block };
                        remsuccess = reqset.TryGetValue(binv, out trueinv);

                        if (!remsuccess || trueinv == null) return (false, null);
                        fulfilled.Add(trueinv);
                        trueinv.block = block;
                    }
                    else
                    {
                        fulfilled.Add(trueinv);
                        trueinv.block = block;
                    }
                }

                fulfilled.ForEach(x =>
                {
                    reqset.Remove(x);
                    InventoryTimeouts.Remove(x, out _);
                });
                return (true, fulfilled);
            }
            else if (packet.GetType() == typeof(HeadersPacket))
            {
                var headers = packet as HeadersPacket;
                List<InventoryVectorRef> fulfilled = new(capacity: headers.Headers.Length + 1);

                foreach (BlockHeader header in headers.Headers)
                {
                    var hinv = new InventoryVectorRef(new InventoryVector(ObjectType.BlockHeader, header.BlockHash)) { header = header };
                    bool remsuccess = reqset.TryGetValue(hinv, out var trueinv);
                    if (!remsuccess || trueinv == null)
                    {
                        hinv = new InventoryVectorRef(new InventoryVector(ObjectType.BlockHeader, new Cipher.SHA256(header.Height))) { header = header };
                        remsuccess = reqset.TryGetValue(hinv, out trueinv);

                        if (!remsuccess || trueinv == null) return (false, null);
                        fulfilled.Add(trueinv);
                        trueinv.header = header;
                    }
                    else
                    {
                        fulfilled.Add(trueinv);
                        trueinv.header = header;
                    }
                }

                fulfilled.ForEach(x =>
                {
                    reqset.Remove(x);
                    InventoryTimeouts.Remove(x, out _);
                });
                return (true, fulfilled);
            }
            else if (packet is NotFoundPacket p)
            {
                List<InventoryVectorRef> notFounds = new List<InventoryVectorRef>();
                foreach (var vec in p.Inventory)
                {
                    var binv = new InventoryVectorRef(vec);
                    bool remsuccess = reqset.TryGetValue(binv, out var trueinv);
                    if (!remsuccess || trueinv == null) return (false, null);
                    notFounds.Add(trueinv);
                }

                notFounds.ForEach(x =>
                {
                    reqset.Remove(x);
                    InventoryTimeouts.Remove(x, out _);
                });

                return (true, notFounds);
            }
            else
            {
                Daemon.Logger.Error($"Handler.CheckFulfillment: cannot check packet of type {packet.GetType()}");
                return (false, null);
            }
        }

        internal (bool, List<InventoryVectorRef>) CheckNotFound(NotFoundPacket p, IPEndPoint resp)
        {
            bool success = NeededInventory.TryGetValue(resp, out var reqset);
            if (!success || reqset == null)
            {
                return (false, null);
            }

            List<InventoryVectorRef> exists = new();

            foreach (InventoryVector vec in p.Inventory)
            {
                var ivref = new InventoryVectorRef { vector = vec, peer = resp };
                bool remsuccess = reqset.TryGetValue(ivref, out var trueinv);

                if (!remsuccess || trueinv == null) return (false, null);
                else
                {
                    exists.Add(trueinv);
                }
            }

            exists.ForEach(x =>
            {
                reqset.Remove(x);
                InventoryTimeouts.Remove(x, out _);
            });
            return (true, exists);
        }

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
            _ = Task.Run(() => Handle(token), token).ConfigureAwait(false);
            _ = Task.Run(() => NeededInventoryStart(token), token).ConfigureAwait(false);
        }

        public async Task Handle(CancellationToken token)
        {
            Dictionary<Peerbloom.Connection, List<Packet>> proute = new();

            List<(Packet, Peerbloom.Connection)> parallel = new();
            List<(Packet, Peerbloom.Connection)> sequential = new();

            while (!token.IsCancellationRequested)
            {
                while (!InboundPacketQueue.IsEmpty)
                {
                    bool success = InboundPacketQueue.TryDequeue(out var packet);

                    if (success)
                    {
                        // route the packages accordingly
                        if (IsOrderDependent(packet.Item1))
                        {
                            bool prsucc = proute.TryGetValue(packet.Item2, out _);

                            if (!prsucc)
                            {
                                proute[packet.Item2] = new();
                                proute[packet.Item2].Add(packet.Item1);
                            }
                            else
                            {
                                proute[packet.Item2].Add(packet.Item1);
                            }
                        }
                        else if (IsParallel(packet.Item1))
                        {
                            parallel.Add(packet);
                        }
                        else
                        {
                            sequential.Add(packet);
                        }                        
                    }
                }

                // execute each packet handler
                foreach (var pritem in proute)
                {
                    var prlist = new List<Packet>(pritem.Value);
                    _ = Task.Run(() => Handle(prlist, pritem.Key), token).ConfigureAwait(false);
                }

                foreach (var packet in parallel)
                {
                    _ = Task.Run(() => Handle(packet.Item1, packet.Item2), token).ConfigureAwait(false);
                }

                if (sequential.Count > 0)
                {
                    var seqlist = new List<(Packet, Peerbloom.Connection)>(sequential);
                    _ = Task.Run(() => Handle(seqlist), token).ConfigureAwait(false);
                }

                // clear the routed packet structures
                proute.Clear();
                parallel.Clear();
                sequential.Clear();

                // delay
                await Task.Delay(5, token);
            }
        }

        public bool IsOrderDependent(Packet p)
        {
            switch (p.Header.Command)
            {
                case PacketType.REJECT:
                case PacketType.VERSION:
                case PacketType.VERACK:
                case PacketType.SENDMSG:
                case PacketType.NOTFOUND:
                case PacketType.NETPING:
                case PacketType.NETPONG:
                case PacketType.REQUESTPEERS:
                case PacketType.REQUESTPEERSRESP:
                case PacketType.DISCONNECT:
                    return true;
                default:
                    return false;
            }
        }

        public bool IsParallel(Packet p)
        {
            switch (p.Header.Command)
            {
                case PacketType.GETBLOCKS:
                case PacketType.SENDTX:
                case PacketType.GETTXS:
                case PacketType.TXS:
                case PacketType.GETHEADERS:
                case PacketType.GETPOOL:
                case PacketType.POOL:
                    return true;
                case PacketType.SENDBLOCK:
                case PacketType.SENDBLOCKS:
                case PacketType.BLOCKS:
                case PacketType.HEADERS:
                case PacketType.ALERT:
                case PacketType.NONE:
                case PacketType.INVENTORY:
                default:
                    return false;
            }
        }

        public void AddPacketToQueue(Packet p, Peerbloom.Connection conn)
        {
            InboundPacketQueue.Enqueue((p, conn));
        }

        public async Task Handle(IEnumerable<(Packet, Peerbloom.Connection)> ps)
        {
            foreach (var packet in ps) await Handle(packet.Item1, packet.Item2);
        }

        public async Task Handle(IEnumerable<Packet> ps, Peerbloom.Connection conn)
        {
            foreach (Packet packet in ps) await Handle(packet, conn);
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
                    Daemon.Logger.Warn($"Ignoring message from {conn.Receiver} during startup", verbose: 1);
                }

                Daemon.Logger.Info($"Discreet.Network.Handler.Handle: received packet {p.Header.Command} from {conn.Receiver}", verbose: 1);

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
                        await HandleReject((RejectPacket)p.Body, conn);
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
                    case PacketType.GETHEADERS:
                        await HandleGetHeaders((GetHeadersPacket)p.Body, conn);
                        break;
                    case PacketType.HEADERS:
                        await HandleHeaders((HeadersPacket)p.Body, conn);
                        break;
                    case PacketType.SENDMSG:
                        await HandleMessage((SendMessagePacket)p.Body, conn.Receiver);
                        break;
                    case PacketType.SENDTX:
                        await HandleSendTx((SendTransactionPacket)p.Body, conn.Receiver);
                        break;
                    case PacketType.SENDBLOCK:
                        await HandleSendBlock((SendBlockPacket)p.Body, conn);
                        break;
                    case PacketType.SENDBLOCKS:
                        await HandleSendBlocks((SendBlocksPacket)p.Body, conn);
                        break;
                    case PacketType.GETPOOL:
                        await HandleGetPool((GetPoolPacket)p.Body, conn);
                        break;
                    case PacketType.POOL:
                        await HandlePool((PoolPacket)p.Body, conn);
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
                        await HandleBlocks((BlocksPacket)p.Body, conn);
                        break;
                    case PacketType.NOTFOUND:
                        await HandleNotFound((NotFoundPacket)p.Body, conn);
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
                    case PacketType.DISCONNECT:
                        await HandleDisconnect((Core.Packets.Peerbloom.Disconnect)p.Body, conn);
                        break;
                    default:
                        Daemon.Logger.Error($"Discreet.Network.Handler.Handle: received unsupported packet from {conn.Receiver} with type {p.Header.Command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Daemon.Logger.Error($"Handler.Handle: handling packet {p.Header.Command} got an exception: {ex.Message}", ex);
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

            if (p.Data == null || p.Data.Length != 8 || Common.Serialization.GetUInt64(p.Data, 0) != PingID)
            {
                Daemon.Logger.Warn($"HandleNetPong: ping ID mismatch for peer {conn.Receiver}: expected {PingID}, but got {Common.Serialization.GetUInt64(p.Data, 0)}");
                return;
            }

            conn.PingLatency = DateTime.UtcNow.Ticks - conn.PingStart;

            var start = new DateTime(conn.PingStart).ToLocalTime().ToString("hh:mm:ss.fff tt");
            var end = DateTime.Now.ToString("hh:mm:ss.fff tt");
            var latency = conn.PingLatency / 10000;
            Daemon.Logger.Info($"Peer {conn.Receiver} pinged at {start} responded at {end} with latency {latency} ms", verbose: 2);

            // reset ping status
            conn.WasPinged = false;
            conn.PingStart = 0;
        }

        public async Task HandleRequestPeers(Core.Packets.Peerbloom.RequestPeers p, IPEndPoint endpoint)
        {
            Daemon.Logger.Info($"Received `RequestPeers` from: {endpoint}", verbose: 2);

            var nodes = _network.GetPeers(p.MaxPeers);
            Core.Packets.Peerbloom.RequestPeersResp respBody = new Core.Packets.Peerbloom.RequestPeersResp { Peers = nodes.ToArray() };

            Packet resp = new Packet(PacketType.REQUESTPEERSRESP, respBody);

            _network.Send(endpoint, resp);
        }

        public async Task HandleRequestPeersResp(Core.Packets.Peerbloom.RequestPeersResp p, Peerbloom.Connection conn)
        {
            foreach (var endpoint in p.Peers)
            {
                if (conn.Receiver.Port < 49152 && !_network.ReflectedAddress.Equals(endpoint.Address))
                {
                    _network.peerlist.AddNew(endpoint, conn.Receiver, 2L * 60L * 60L * 10_000_000L);
                }
            }
        }

        public async Task HandleAlert(AlertPacket p)
        {
            if (MessageCache.GetMessageCache().Alerts.Contains(p))
            {
                return;
            }

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

            Daemon.Logger.Critical($"Alert received from {p.Sig.y.ToHexShort()}: {p.Message}");

            MessageCache.GetMessageCache().AddAlert(p);

            _network.Broadcast(new Packet(PacketType.ALERT, p));
        }

        public async Task HandleReject(RejectPacket p, Peerbloom.Connection conn)
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

            Daemon.Logger.Error($"Packet {p.RejectedType} {(valueString == null ? "" : $"(data { valueString})")} was rejected by peer {conn.Receiver} with code {p.Code} {(p.Reason == null || p.Reason.Length == 0 ? "" : ": " + p.Reason)}");
        }

        public Core.Packets.Peerbloom.VersionPacket MakeVersionPacket()
        {
            return new Core.Packets.Peerbloom.VersionPacket
            {
                Version = Daemon.DaemonConfig.GetConfig().NetworkVersion.Value,
                Services = Services,
                Timestamp = DateTime.UtcNow.Ticks,
                Height = BlockBuffer.Instance.GetChainHeight(),
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

                Peerbloom.Connection _dupe = _network.GetPeer(new IPEndPoint(conn.Receiver.Address, conn.Port), true);

                if (_dupe != null)
                {
                    // duplicate connection; end
                    Daemon.Logger.Warn($"Discreet.Network.Handler.HandleVersion: duplicate peer connection found for {conn.Receiver} (currently at {_dupe.Receiver}); ending connection");
                    await conn.Disconnect(true, Core.Packets.Peerbloom.DisconnectCode.CLEAN);
                    return;
                }

                if (!mCache.Versions.TryAdd(conn.Receiver, p))
                {
                    Daemon.Logger.Error($"Discreet.Network.Handler.HandleVersion: failed to add version for {conn.Receiver}");
                }
            }

            /* we reply with our own version */
            await conn.SendAsync(new Packet(PacketType.VERSION, MakeVersionPacket()));
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
                await conn.SendAsync(new Packet(PacketType.VERACK, p));

                _network.ConnectingPeers.Remove(conn.Receiver, out _);
                MessageCache.GetMessageCache().Versions.Remove(conn.Receiver, out _);
                MessageCache.GetMessageCache().BadVersions.Remove(conn.Receiver, out _);

                // update lastSeen


                await conn.Disconnect(true);
                return;
            }

            if (p.Counter < 0) return; // something has gone wrong if this is true.

            // set our reflected address
            if (_network.ReflectedAddress == null) _network.ReflectedAddress = p.ReflectedEndpoint.Address;

            p.ReflectedEndpoint = conn.Receiver;
            p.Counter++;

            conn.SetConnectionAcknowledged();
            _network.IncomingTester.Enqueue(new IPEndPoint(conn.Receiver.Address, conn.Port));

            _network.Send(conn, new Packet(PacketType.VERACK, p));
            _network.AddInboundConnection(conn);
        }

        public async Task HandleGetBlocks(GetBlocksPacket p, IPEndPoint senderEndpoint)
        {
            IView dataView = BlockBuffer.Instance;

            List<Block> blocks = new();
            List<InventoryVector> notFound = new();
            uint totalBlockSize = 0;

            foreach (var h in p.Blocks)
            {
                Block blockToAdd = null;
                if (h.IsLong())
                {
                    bool success = dataView.TryGetBlock(h.ToInt64(), out var block);

                    if (!success || block == null)
                    {
                        notFound.Add(new InventoryVector { Hash = h, Type = ObjectType.Block });
                    }
                    else
                    {
                        blocks.Add(block);
                        blockToAdd = block;
                    }
                }
                else
                {
                    bool success = dataView.TryGetBlock(h, out var block);

                    if (!success || block == null)
                    {
                        notFound.Add(new InventoryVector { Hash = h, Type = ObjectType.Block });
                    }
                    else
                    {
                        blocks.Add(block);
                        blockToAdd = block;
                    }
                }

                if (blockToAdd != null)
                {
                    totalBlockSize += blockToAdd.Header.BlockSize;

                    // break into chunks if request is too large
                    if (totalBlockSize > 15_000_000)
                    {
                        Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.BLOCKS, new BlocksPacket { Blocks = blocks.ToArray() }));
                        blocks.Clear();
                        totalBlockSize = 0;
                    }
                }
            }

            if (blocks.Count > 0)
            {
                Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.BLOCKS, new BlocksPacket { Blocks = blocks.ToArray() }));
            }

            if (notFound.Count > 0)
            {
                NotFoundPacket resp = new NotFoundPacket
                {
                    Inventory = notFound.ToArray(),
                };

                Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.NOTFOUND, resp));
            }
        }

        public async Task HandleGetTxs(GetTransactionsPacket p, IPEndPoint senderEndpoint)
        {
            IView dataView = BlockBuffer.Instance;

            List<FullTransaction> txs = new();
            List<InventoryVector> notFound = new();

            foreach (var h in p.Transactions)
            {
                if (h.IsLong())
                {
                    bool success = dataView.TryGetTransaction(h.ToUInt64(), out var tx);

                    if (!success || tx == null)
                    {
                        success = dataView.TryGetTransaction(h, out tx);

                        if (!success || tx == null)
                        {
                            success = Daemon.TXPool.GetTXPool().TryGetTransaction(h, out tx);

                            if (!success || tx == null)
                            {
                                notFound.Add(new InventoryVector { Hash = h, Type = ObjectType.Transaction });
                            }
                            else
                            {
                                txs.Add(tx);
                            }
                        }
                        else
                        {
                            txs.Add(tx);
                        }
                    }
                    else
                    {
                        txs.Add(tx);
                    }
                }
                else
                {
                    bool success = dataView.TryGetTransaction(h, out var tx);

                    if (!success || tx == null)
                    {
                        success = Daemon.TXPool.GetTXPool().TryGetTransaction(h, out tx);

                        if (!success || tx == null)
                        {
                            notFound.Add(new InventoryVector { Hash = h, Type = ObjectType.Transaction });
                        }
                        else
                        {
                            txs.Add(tx);
                        }
                    }
                    else
                    {
                        txs.Add(tx);
                    }
                }
            }

            if (txs.Count > 0)
            {
                Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.TXS, new TransactionsPacket { Txs = txs.ToArray() }));
            }

            if (notFound.Count > 0) 
            {
                NotFoundPacket resp = new NotFoundPacket
                {
                    Inventory = notFound.ToArray(),
                };

                Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.NOTFOUND, resp));
            }
        }

        public async Task HandleGetPool(Core.Packets.GetPoolPacket p, Peerbloom.Connection conn)
        {
            var txpool = Daemon.TXPool.GetTXPool();
            var txs = txpool.GetTransactions();
            Peerbloom.Network.GetNetwork().Send(conn.Receiver, new Packet(PacketType.TXS, new TransactionsPacket { Txs = txs.ToArray() }));
        }

        public async Task HandlePool(Core.Packets.PoolPacket p, Peerbloom.Connection conn)
        {
            var txpool = Daemon.TXPool.GetTXPool();

            if (p.TxsLen > 0)
            {
                foreach (var tx in p.Txs)
                {
                    var exc = txpool.ProcessTx(tx);
                    if (exc != null)
                    {
                        Daemon.Logger.Error($"HandlePool: Processing transaction resulted in error: {exc.Message}", exc);
                    }
                }
            }
        }

        public void Handle(string s)
        {
            Daemon.Logger.Info(s);
        }

        public async Task HandleMessage(SendMessagePacket p, IPEndPoint senderEndpoint)
        {
            if (!MessageCache.GetMessageCache().Messages.Contains(p.Message))
            {
                Daemon.Logger.Info($"Message received from {senderEndpoint}: {p.Message}", verbose: 2);

                MessageCache.GetMessageCache().Messages.Add(p.Message);
            }
        }

        public async Task HandleSendTx(SendTransactionPacket p, IPEndPoint senderEndpoint)
        {
            if (State == PeerState.Startup)
            {
                return;
            }

            if (p.Error != null && p.Error != "")
            {
                RejectPacket resp = new RejectPacket
                {
                    RejectedType = PacketType.SENDTX,
                    Reason = p.Error,
                    Data = Array.Empty<byte>(),
                    Code = RejectionCode.MALFORMED,
                };

                Daemon.Logger.Error($"Malformed transaction received from peer {senderEndpoint}: {p.Error}");

                Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.REJECT, resp));
                return;
            }

            var txhash = p.Tx.TxID;

            /* sometimes a SendTx can occur as propagation for a recently added block */
            if (BlockBuffer.Instance.ContainsTransaction(txhash))
            {
                Daemon.Logger.Debug($"HandleSendTx: Transaction received was in a previous block");
                return;
            }

            if (!Daemon.TXPool.GetTXPool().Contains(p.Tx.Hash()))
            {
                var err = Daemon.TXPool.GetTXPool().ProcessTx(p.Tx);

                if (err != null)
                {
                    RejectPacket resp = new RejectPacket
                    {
                        RejectedType = PacketType.SENDTX,
                        Reason = err.Message,
                        Data = p.Tx.Hash().Bytes,
                        Code = RejectionCode.INVALID,
                    };

                    Daemon.Logger.Error($"Malformed transaction received from peer {senderEndpoint}: {err.Message}", err);

                    Peerbloom.Network.GetNetwork().Send(senderEndpoint, new Packet(PacketType.REJECT, resp));
                    return;
                }
                else
                {
                    Peerbloom.Network.GetNetwork().Broadcast(new Packet(PacketType.SENDTX, p));
                }

                // the event is raised only if (1) tx hasn't been seen before and (2) tx isn't malformed.
                //OnTransactionReceived?.Invoke(new TransactionReceivedEventArgs { Tx = p.Tx, Success = err == null });

                ZMQ.Publisher.Instance.Publish("txhash", p.Tx.Hash().ToHex());
                ZMQ.Publisher.Instance.Publish("txraw", p.Tx.Readable());

                return;
            }
        }

        // TODO: finish preblock
        public async Task HandleSendPreblock(SendPreblockPacket p, Peerbloom.Connection conn, bool doBroadcast = true)
        {
            if (State != PeerState.Normal) return;

            if (p.Error != null && p.Error != "")
            {
                RejectPacket resp = new RejectPacket
                {
                    RejectedType = PacketType.SENDBLOCK,
                    Reason = p.Error,
                    Data = Array.Empty<byte>(),
                    Code = RejectionCode.MALFORMED,
                };

                Daemon.Logger.Error($"Malformed block received from peer {conn.Receiver}: {p.Error}");

                Peerbloom.Network.GetNetwork().Send(conn.Receiver, new Packet(PacketType.REJECT, resp));
                return;
            }

            //if ()
        }

        public async Task HandleSendBlock(SendBlockPacket p, Peerbloom.Connection conn, bool doBroadcast = true)
        {
            if (State == PeerState.Startup)
            {
                return;
            }

            if (p.Error != null && p.Error != "")
            {
                RejectPacket resp = new RejectPacket
                {
                    RejectedType = PacketType.SENDBLOCK,
                    Reason = p.Error,
                    Data = Array.Empty<byte>(),
                    Code = RejectionCode.MALFORMED,
                };

                Daemon.Logger.Error($"Malformed block received from peer {conn.Receiver}: {p.Error}");

                Peerbloom.Network.GetNetwork().Send(conn.Receiver, new Packet(PacketType.REJECT, resp));
                return;
            }

            if (State == PeerState.Syncing)
            {
                (bool succ, string reason) = MessageCache.GetMessageCache().AddBlockToCache(p.Block);
                if (!succ)
                {
                    RejectPacket resp = new RejectPacket
                    {
                        RejectedType = PacketType.SENDBLOCK,
                        Reason = reason,
                        Data = p.Block.Header.BlockHash.Bytes,
                        Code = RejectionCode.INVALID,
                    };

                    Daemon.Logger.Error($"Malformed block received from peer {conn.Receiver}: {reason}");
                    Peerbloom.Network.GetNetwork().Send(conn.Receiver, new Packet(PacketType.REJECT, resp));

                    return;
                }

                LastSeenHeight = p.Block.Header.Height;

                // we don't broadcast blocks while syncing
                // Peerbloom.Network.GetNetwork().Broadcast(new Packet(PacketType.SENDBLOCK, p));
                return;
            }
            else if (State == PeerState.Processing)
            {
                MessageCache.GetMessageCache().BlockCache[p.Block.Header.Height] = p.Block;

                if (doBroadcast) Peerbloom.Network.GetNetwork().Broadcast(new Packet(PacketType.SENDBLOCK, p));
                return;
            }
            else
            {
                if (!BlockBuffer.Instance.BlockExists(p.Block.Header.BlockHash))
                {
                    /* create validation cache and perform check */
                    DB.ValidationCache vCache = new DB.ValidationCache(p.Block);
                    var err = vCache.Validate();

                    if (err is DB.OrphanBlockException orphanErr)
                    {
                        if (!MessageCache.GetMessageCache().OrphanBlockParents.ContainsKey(p.Block.Header.PreviousBlock))
                        {
                            await MessageCache.GetMessageCache().OrphanLock.WaitAsync();

                            Daemon.Logger.Warn($"HandleSendBlock: orphan block ({p.Block.Header.BlockHash.ToHexShort()}, height {p.Block.Header.Height}) added; querying {conn.Receiver} for previous block", verbose: 3);
                            MessageCache.GetMessageCache().OrphanBlocks[p.Block.Header.PreviousBlock] = p.Block;
                            MessageCache.GetMessageCache().OrphanBlockParents[p.Block.Header.BlockHash] = p.Block.Header.PreviousBlock;
                            Peerbloom.Network.GetNetwork().SendRequest(conn, new Packet(PacketType.GETBLOCKS, new GetBlocksPacket { Blocks = new Cipher.SHA256[] { p.Block.Header.PreviousBlock } }), durationMilliseconds: 60000);
                            
                            MessageCache.GetMessageCache().OrphanLock.Release();
                            return;
                        }
                        else
                        {
                            await MessageCache.GetMessageCache().OrphanLock.WaitAsync();

                            Daemon.Logger.Warn($"HandleSendBlock: orphan block ({p.Block.Header.BlockHash.ToHexShort()}, height {p.Block.Header.Height}) added", verbose: 3);
                            MessageCache.GetMessageCache().OrphanBlocks[p.Block.Header.PreviousBlock] = p.Block;
                            MessageCache.GetMessageCache().OrphanBlockParents[p.Block.Header.BlockHash] = p.Block.Header.PreviousBlock;
                            CheckRoot(p.Block, conn);

                            MessageCache.GetMessageCache().OrphanLock.Release();
                            return;
                        }
                    }
                    else if (err != null)
                    {
                        RejectPacket resp = new RejectPacket
                        {
                            RejectedType = PacketType.SENDBLOCK,
                            Reason = err.Message,
                            Data = p.Block.Header.BlockHash.Bytes,
                            Code = RejectionCode.INVALID,
                        };

                        Daemon.Logger.Error($"Malformed block received from peer {conn.Receiver}: {err.Message}", err);

                        Peerbloom.Network.GetNetwork().Send(conn.Receiver, new Packet(PacketType.REJECT, resp));
                        return;
                    }

                    /* accept block and propagate */
                    try
                    {
                        await vCache.Flush();
                    }
                    catch (Exception e)
                    {
                        Daemon.Logger.Error($"HandleSendBlock: An error was encountered while flushing validation cache for block at height {p.Block.Header.Height}: {e.Message}", e);
                        return;
                    }

                    if (doBroadcast) Peerbloom.Network.GetNetwork().Broadcast(new Packet(PacketType.SENDBLOCK, p));

                    try
                    {
                        daemon.ProcessBlock(p.Block);
                    }
                    catch (Exception e)
                    {
                        Daemon.Logger.Error($"HandleSendBlock: An error was encountered while processing block at height {p.Block.Header.Height}: {e.Message}", e);
                        return;
                    }

                    OnBlockSuccess?.Invoke(new BlockSuccessEventArgs { Block = p.Block });
                    // invoke ontxsuccess
                    foreach (var tx in p.Block.Transactions)
                    {
                        if (tx.Version == 0)
                        {
                            continue;
                        }

                        // the event is raised only if (1) tx hasn't been seen before and (2) tx isn't malformed.
                        OnTransactionReceived?.Invoke(new TransactionReceivedEventArgs { Tx = tx, Success = true });
                    }
                    /* check orphan data and process accordingly */
                    await AcceptOrphans(p.Block.Header.BlockHash);
                }
                else
                {
                    Daemon.Logger.Info($"HandleSendBlock: already have block at height {p.Block.Header.Height} ({p.Block.Header.BlockHash.ToHexShort()}, prev block {p.Block.Header.PreviousBlock.ToHexShort()})", verbose: 3);
                }
            }
        }

        public async Task HandleSendBlocks(SendBlocksPacket p, Peerbloom.Connection conn)
        {
            // ensure blocks are sorted
            if (p.Error != null)
            {
                await HandleSendBlock(new SendBlockPacket { Block = null, Error = p.Error }, conn, false);
                return;
            }

            // don't propagate endlessly
            var propagate = p.Blocks.Where(x => !BlockBuffer.Instance.BlockExists(x.Hash()));
            if (propagate.Any())
            {
                propagate = propagate.Where(x => !MessageCache.GetMessageCache().OrphanBlocks.ContainsKey(x.Header.BlockHash));
            }
            foreach (var block in p.Blocks.OrderBy(x => x.Header.Height))
            {
                await HandleSendBlock(new SendBlockPacket { Block = block }, conn, false);
            }

            if (propagate.Any()) Peerbloom.Network.GetNetwork().Broadcast(new Packet(PacketType.SENDBLOCKS, p));
        }

        public async Task HandleBlocks(BlocksPacket p, Peerbloom.Connection conn)
        {
            // check if request was fulfilled
            (bool good, var fulfilled) = CheckFulfillment(p, conn.Receiver);
            if (!good)
            {
                Daemon.Logger.Warn($"Handler.HandleBlocks: received unrequested data from peer {conn.Receiver}; potentially malicious behavior");
                return;
            }
            if (State == PeerState.Syncing)
            {
                IView dataView = BlockBuffer.Instance;
                fulfilled.Sort((x, y) => x.block.Header.Height.CompareTo(y.block.Header.Height));

                foreach (var ivref in fulfilled)
                {
                    (bool succ, _) = MessageCache.GetMessageCache().AddBlockToCache(ivref.block);

                    if (!succ)
                    {
                        Daemon.Logger.Warn($"Handler.HandleBlocks: block from peer {conn.Receiver} at height {ivref.block.Header.Height} was invalid");
                        ivref.callback?.Invoke(ivref.peer, ivref.vector, false, RequestCallbackContext.INVALID);
                    }
                    else
                    {
                        LastSeenHeight = Math.Max(LastSeenHeight, ivref.block.Header.Height);
                        ivref.callback?.Invoke(ivref.peer, ivref.vector, true, RequestCallbackContext.SUCCESS);
                    }
                }
            }
            else if (State == PeerState.Processing)
            {
                fulfilled.Sort((x, y) => x.block.Header.Height.CompareTo(y.block.Header.Height));

                foreach (var ivref in fulfilled)
                {
                    LastSeenHeight = Math.Max(LastSeenHeight, ivref.block.Header.Height);
                    MessageCache.GetMessageCache().BlockCache.TryAdd(ivref.block.Header.Height, ivref.block);
                    ivref.callback?.Invoke(ivref.peer, ivref.vector, true, RequestCallbackContext.SUCCESS);
                }
            }
            else
            {
                /* we asked for this block due to orphan */
                if (p.Blocks.Count() != 1)
                {
                    Daemon.Logger.Error("HandleBlocks: queried peer returned more than one block (not syncing; orphan block most likely)");
                    return;
                }
                if (BlockBuffer.Instance.BlockExists(p.Blocks[0].Header.BlockHash))
                {
                    Daemon.Logger.Error("HandleBlocks: queried peer returned an existing block; ignoring");
                }
                else
                {
                    Daemon.Logger.Info($"HandleBlocks: received missing block at height {p.Blocks[0].Header.Height}", verbose: 2);

                    DB.ValidationCache vCache = new DB.ValidationCache(p.Blocks[0]);
                    var err = vCache.Validate();
                    if (err is DB.OrphanBlockException orphanErr)
                    {
                        if (!MessageCache.GetMessageCache().OrphanBlockParents.ContainsKey(p.Blocks[0].Header.PreviousBlock))
                        {
                            Daemon.Logger.Warn($"HandleBlocks: orphan block ({p.Blocks[0].Header.BlockHash.ToHexShort()}, height {p.Blocks[0].Header.Height}) added; querying {conn.Receiver} for previous block", verbose: 3);

                            await MessageCache.GetMessageCache().OrphanLock.WaitAsync();

                            MessageCache.GetMessageCache().OrphanBlocks[p.Blocks[0].Header.PreviousBlock] = p.Blocks[0];
                            MessageCache.GetMessageCache().OrphanBlockParents[p.Blocks[0].Header.BlockHash] = p.Blocks[0].Header.PreviousBlock;
                            Peerbloom.Network.GetNetwork().SendRequest(conn, new Packet(PacketType.GETBLOCKS, new GetBlocksPacket { Blocks = new Cipher.SHA256[] { p.Blocks[0].Header.PreviousBlock } }), durationMilliseconds: 60000);
                            
                            MessageCache.GetMessageCache().OrphanLock.Release();

                            return;
                        }
                        else
                        {
                            Daemon.Logger.Warn($"HandleBlocks: orphan block ({p.Blocks[0].Header.BlockHash.ToHexShort()}, height {p.Blocks[0].Header.Height}) added", verbose: 1);

                            await MessageCache.GetMessageCache().OrphanLock.WaitAsync();

                            MessageCache.GetMessageCache().OrphanBlocks[p.Blocks[0].Header.PreviousBlock] = p.Blocks[0];
                            MessageCache.GetMessageCache().OrphanBlockParents[p.Blocks[0].Header.BlockHash] = p.Blocks[0].Header.PreviousBlock;
                            CheckRoot(p.Blocks[0], conn);

                            MessageCache.GetMessageCache().OrphanLock.Release();

                            return;
                        }
                    }
                    else if (err != null)
                    {
                        Daemon.Logger.Error($"HandleBlocks: Malformed or invalid block received from peer {conn.Receiver}: {err.Message} (bogus block for orphan requirement)", err);

                        /* for now assume invalid root always has invalid leaves */

                        await MessageCache.GetMessageCache().OrphanLock.WaitAsync();
                        TossOrphans(p.Blocks[0].Header.BlockHash);
                        MessageCache.GetMessageCache().OrphanLock.Release();

                        return;
                    }

                    /* orphan data is valid; validate branch and publish changes */
                    Daemon.Logger.Info($"HandleBlocks: Root found for orphan branch beginning with block {p.Blocks[0].Header.BlockHash.ToHexShort()}", verbose: 1);
                    try
                    {
                        await vCache.Flush();
                    }
                    catch (Exception e)
                    {
                        Daemon.Logger.Error($"HandleBlocks: An error was encountered while flushing validation cache for blocks: {e.Message}", e);
                        return;
                    }

                    try
                    {
                        daemon.ProcessBlock(p.Blocks[0]);
                    }
                    catch (Exception e)
                    {
                        Daemon.Logger.Error($"HandleBlocks: An error was encountered while processing blocks: {e.Message}", e);
                        return;
                    }

                    /* recursively accept orphan blocks from message cache */
                    await AcceptOrphans(p.Blocks[0].Header.BlockHash);

                    //success
                    fulfilled.ForEach(x => x.callback?.Invoke(x.peer, x.vector, true, RequestCallbackContext.SUCCESS));
                }
            }
        }

        public async Task HandleGetHeaders(GetHeadersPacket p, Peerbloom.Connection conn)
        {
            IView dataView = BlockBuffer.Instance;

            List<BlockHeader> headers = new();
            List<InventoryVector> notFound = new();

            if (p.StartingHeight >= 0 && (p.Headers == null || p.Headers.Length == 0))
            {
                // retrieve by height
                for (long i = p.StartingHeight; i < p.StartingHeight + p.Count; i++)
                {
                    bool success = dataView.TryGetBlockHeader(i, out var header);
                    if (!success || header == null)
                    {
                        notFound.Add(new InventoryVector { Hash = new Cipher.SHA256(i), Type = ObjectType.BlockHeader });
                    }
                    else
                    {
                        headers.Add(header);
                    }
                }
            }
            else
            {
                foreach (var h in p.Headers)
                {
                    if (h.IsLong())
                    {
                        bool success = dataView.TryGetBlockHeader(h.ToInt64(), out var header);
                        if (!success || header == null)
                        {
                            notFound.Add(new InventoryVector { Hash = h, Type = ObjectType.BlockHeader });
                        }
                        else
                        {
                            headers.Add(header);
                        }
                    }
                    else
                    {
                        bool success = dataView.TryGetBlockHeader(h, out var header);
                        if (!success || header == null)
                        {
                            notFound.Add(new InventoryVector { Hash = h, Type = ObjectType.BlockHeader });
                        }
                        else
                        {
                            headers.Add(header);
                        }
                    }
                }
            }

            if (headers.Count > 0)
            {
                Peerbloom.Network.GetNetwork().Send(conn.Receiver, new Packet(PacketType.HEADERS, new HeadersPacket { Headers = headers.ToArray() }));
            }

            if (notFound.Count > 0)
            {
                NotFoundPacket resp = new NotFoundPacket
                {
                    Inventory = notFound.ToArray(),
                };  

                Peerbloom.Network.GetNetwork().Send(conn.Receiver, new Packet(PacketType.NOTFOUND, resp));
            }
        }

        public async Task HandleHeaders(HeadersPacket p, Peerbloom.Connection conn)
        {
            // check if request was fulfilled
            (bool good, var fulfilled) = CheckFulfillment(p, conn.Receiver);
            if (!good)
            {
                Daemon.Logger.Error($"Handler.HandleHeaders: received unrequested data from peer {conn.Receiver}; potentially malicious behavior");
                return;
            }
            if (State == PeerState.Syncing)
            {
                fulfilled.Sort((x,y) => x.header.Height.CompareTo(y.header.Height));

                foreach (var ivref in fulfilled)
                {
                    bool succ = MessageCache.GetMessageCache().AddHeaderToCache(ivref.header);
                    if (!succ)
                    {
                        Daemon.Logger.Warn($"Handler.HandleHeaders: header from peer {conn.Receiver} at height {ivref.header.Height} was invalid");
                        ivref.callback?.Invoke(ivref.peer, ivref.vector, false, RequestCallbackContext.INVALID);
                    }
                    else
                    {
                        LastSeenHeight = ivref.header.Height;
                        ivref.callback?.Invoke(ivref.peer, ivref.vector, true, RequestCallbackContext.SUCCESS);
                    }
                }
            }
            else
            {
                Daemon.Logger.Warn($"Handler.HandleHeaders: Received unsolicited headers packet from peer {conn.Receiver}");
            }
        }

        public async Task HandleNotFound(NotFoundPacket p, Peerbloom.Connection conn)
        {
            (bool good, var items) = CheckFulfillment(p, conn.Receiver);
            if (!good)
            {
                Daemon.Logger.Error($"Handler.HandleNotFound: received unrequested NOTFOUND from peer {conn.Receiver}; potentially malicious behavior");
                return;
            }

            string itemsStr = "";

            for (int i = 0; i < p.Count; i++)
            {
                var h = p.Inventory[i];
                itemsStr += (h.Type == ObjectType.Transaction ? "Tx " : (h.Type == ObjectType.Block ? "Block " : "Header ")) + (h.Hash.IsLong() ? h.Hash.ToUInt64().ToString() : h.Hash.ToHexShort());

                if (i < p.Count - 1)
                {
                    itemsStr += ", ";
                }
            }

            Daemon.Logger.Error($"Could not find objects: {itemsStr}");

            items.ForEach(item => item.callback?.Invoke(item.peer, item.vector, false, RequestCallbackContext.NOTFOUND));
        }

        public async Task HandleDisconnect(Core.Packets.Peerbloom.Disconnect p, Peerbloom.Connection conn)
        {
            Daemon.Logger.Info($"Handler.HandleDisconnect: Peer at {conn.Receiver} disconnected with the following reason: {p.Code}", verbose: 1);
            await conn.Disconnect(false);
        }

        /// <summary>
        /// Removes a bad root and branch up to orphan leaf in the orphan block structure
        /// </summary>
        /// <param name="bHash"></param>
        public void TossOrphans(Cipher.SHA256 bHash)
        {
            MessageCache mCache = MessageCache.GetMessageCache();
            while (mCache.OrphanBlocks.ContainsKey(bHash))
            {
                mCache.OrphanBlocks.Remove(bHash, out var block);
                bHash = block.Header.BlockHash;
                mCache.OrphanBlockParents.Remove(bHash, out _);
            }
        }

        public void CheckRoot(Block block, Peerbloom.Connection conn)
        {
            MessageCache mCache = MessageCache.GetMessageCache();
            var hash = block.Header.PreviousBlock;
            while (mCache.OrphanBlockParents.ContainsKey(hash))
            {
                hash = mCache.OrphanBlockParents[hash];
            }

            // request previous block
            Peerbloom.Network.GetNetwork().SendRequest(conn, new Packet(PacketType.GETBLOCKS, new GetBlocksPacket { Blocks = new Cipher.SHA256[] { hash } }), durationMilliseconds: 60000);
        }

        /// <summary>
        /// Accepts all blocks on an orphan branch, if any.
        /// </summary>
        /// <param name="bHash"></param>
        public async Task AcceptOrphans(Cipher.SHA256 bHash)
        {
            MessageCache mCache = MessageCache.GetMessageCache();

            await mCache.OrphanLock.WaitAsync();

            try
            {
                while (mCache.OrphanBlocks.ContainsKey(bHash))
                {
                    mCache.OrphanBlocks.Remove(bHash, out var block);
                    DB.ValidationCache vCache = new DB.ValidationCache(block);
                    var err = vCache.Validate();
                    if (err is OrphanBlockException)
                    {
                        // simply return
                        return;
                    }
                    if (err != null)
                    {
                        Daemon.Logger.Error($"AcceptOrphans: Malformed or invalid block in orphan branch {bHash.ToHexShort()} (height {block.Header.Height}): {err.Message}; tossing branch", err);
                        TossOrphans(bHash);
                        return;
                    }

                    try
                    {
                        await vCache.Flush();
                    }
                    catch (Exception e)
                    {
                        Daemon.Logger.Error($"AcceptOrphans: an error was encountered while flushing validation cache for block at height {block.Header.Height}: {e.Message}", e);
                    }

                    try
                    {
                        daemon.ProcessBlock(block);
                    }
                    catch (Exception e)
                    {
                        Daemon.Logger.Error($"AcceptOrphans: an error was encountered while processing block at height {block.Header.Height}: {e.Message}", e);
                    }

                    OnBlockSuccess?.Invoke(new BlockSuccessEventArgs { Block = block });
                    bHash = block.Header.BlockHash;
                    mCache.OrphanBlockParents.Remove(bHash, out _);
                }
            }
            finally
            {
                mCache.OrphanLock.Release();
            }
        }
    }
}
