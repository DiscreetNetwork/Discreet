using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin;
using Discreet.Wallets;
using Discreet.Common.Exceptions;
using Discreet.Cipher;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;

namespace Discreet.Daemon
{
    // utility things
    internal class IPEndPointComparer: IComparer<IPEndPoint>
    {
        public int Compare(IPEndPoint a, IPEndPoint b)
        {
            return a.ToString().CompareTo(b.ToString());
        }
    }

    /* Manages all blockchain operations. Contains the wallet manager, logger, Network manager, RPC manager, DB manager and TXPool manager. */
    public class Daemon
    {
        public ConcurrentBag<Wallet> wallets;
        TXPool txpool;

        Network.Handler handler;
        Network.Peerbloom.Network network;
        Network.MessageCache messageCache;
        DB.DataView dataView;
        DaemonConfig config;

        RPC.RPCServer _rpcServer;
        CancellationToken _cancellationToken;
        CancellationTokenSource _tokenSource = new CancellationTokenSource();

        ConcurrentDictionary<object, ConcurrentQueue<SHA256>> syncerQueues;

        public static bool DebugMode { get; set; } = false;

        public bool IsMasternode;

        private Key signingKey;

        public long Uptime { get; private set; }

        public bool RPCLive { get; set; }

        public Daemon()
        {
            wallets = new ConcurrentBag<Wallet>();

            txpool = TXPool.GetTXPool();
            network = Network.Peerbloom.Network.GetNetwork();
            messageCache = Network.MessageCache.GetMessageCache();
            dataView = DB.DataView.GetView();

            config = DaemonConfig.GetConfig();
            
            signingKey = Key.FromHex(config.SigningKey);

            if (KeyOps.ScalarmultBase(ref signingKey).Equals(Key.FromHex("806d68717bcdffa66ba465f906c2896aaefc14756e67381f1b9d9772d03fd97d")))
            {
                IsMasternode = true;
            }

            syncerQueues = new ConcurrentDictionary<object, ConcurrentQueue<SHA256>>();

            _cancellationToken = _tokenSource.Token;
        }

        public async Task<bool> Restart()
        {
            Logger.Fatal("Daemon could not complete initialization and requires restart.");
            Shutdown();
            await Task.Delay(5000);

            // re-create everything
            wallets = new ConcurrentBag<Wallet>();

            txpool = TXPool.GetTXPool();
            network = Network.Peerbloom.Network.GetNetwork();
            messageCache = Network.MessageCache.GetMessageCache();
            dataView = DB.DataView.GetView();

            config = DaemonConfig.GetConfig();

            signingKey = Key.FromHex(config.SigningKey);

            if (KeyOps.ScalarmultBase(ref signingKey).Equals(Key.FromHex("806d68717bcdffa66ba465f906c2896aaefc14756e67381f1b9d9772d03fd97d")))
            {
                IsMasternode = true;
            }

            syncerQueues = new ConcurrentDictionary<object, ConcurrentQueue<SHA256>>();

            _cancellationToken = _tokenSource.Token;
            RPC.RPCEndpointResolver.ClearEndpoints();

            Logger.Info("Restarting Daemon...");
            bool success = await Start();
            return success;
        }

        public async Task MainLoop()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000000, _cancellationToken);
            }
        }

        public Network.Handler GetHandler()
        {
            return handler;
        }

        public void Shutdown()
        {
            network.Shutdown();

            _rpcServer.Stop();
            ZMQ.Publisher.Instance.Stop();
            _tokenSource.Cancel();
        }

        public async Task<bool> Start()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => Logger.CrashLog(sender, e);
            Logger.Debug("Attached global exception handler.");

            Uptime = DateTime.Now.Ticks;

            if (IsMasternode && dataView.GetChainHeight() < 0)
            {
                BuildGenesis();
            }

            RPCLive = false;
            Logger.Log($"Starting RPC server...");
            _rpcServer = new RPC.RPCServer(DaemonConfig.GetConfig().RPCPort.Value, this);
            _ = _rpcServer.Start();
            _ = Task.Factory.StartNew(async () => await ZMQ.Publisher.Instance.Start(DaemonConfig.GetConfig().ZMQPort.Value));

            await network.Start();
            await network.Bootstrap();
            handler = network.handler;
            handler.daemon = this;

            handler.SetState(Network.PeerState.Startup);
            RPCLive = true;

            if (!IsMasternode)
            {
                /* get height of chain */
                long bestHeight = dataView.GetChainHeight();
                IPEndPoint bestPeer = null;

                foreach (var ver in messageCache.Versions)
                {
                    if (ver.Value.Height > bestHeight)
                    {
                        bestPeer = ver.Key;
                        bestHeight = ver.Value.Height;
                    }
                }

                /* among peers, find each of their associated heights */
                List<(IPEndPoint, long)> peersAndHeights = new();
                foreach (var ver in messageCache.Versions)
                {
                    if (ver.Value.Height > dataView.GetChainHeight())
                    {
                        peersAndHeights.Add((ver.Key, ver.Value.Height));
                    }
                }

                handler.SetState(Network.PeerState.Syncing);

                long beginningHeight = dataView.GetChainHeight();

                List<Network.Core.Packets.InventoryVector> missedItems = new();
                long toBeFulfilled = 0;
                var baseUsablePeers = peersAndHeights.Where(x => x.Item2 >= bestHeight).Select(x => x.Item1).ToList();
                var usablePeers = new List<IPEndPoint>(baseUsablePeers);
                long numTotalFailures = 0;
                var callback = (IPEndPoint peer, Network.Core.Packets.InventoryVector missedItem, bool success, Network.RequestCallbackContext ctx) => 
                {
                    if (!success)
                    {
                        lock (missedItems)
                        {
                            missedItems.Add(missedItem);
                        }

                        lock (usablePeers)
                        {
                            usablePeers.Remove(peer);

                            if (usablePeers.Count == 0)
                            {
                                Interlocked.Increment(ref numTotalFailures);
                                usablePeers.Clear();
                                usablePeers.AddRange(baseUsablePeers);
                            }
                        }
                    }
                    else
                    {
                        Interlocked.Decrement(ref toBeFulfilled);
                    }
                };
                /**
                 * A note on how Header-First Syncing works:
                 *  - node finds best node to query with longest chain and requests as many headers as possible
                 *  - node caches the headers (we use in-memory caching for now, since the chain is small and well within memory limits)
                 *  - node then queries among many peers to sync the blocks themselves
                 */
                // perform header-first syncing
                if (bestPeer != null)
                {
                    long _hheight = dataView.GetChainHeight();
                    beginningHeight = _hheight;
                    //var bestConn = network.GetPeer(bestPeer);

                    while (_hheight < bestHeight)
                    {
                        var _newHheight = (_hheight + 25000) <= bestHeight ? _hheight + 25000 : bestHeight;
                        Logger.Info($"Fetching block headers {_hheight + 1} to {_newHheight}");

                        toBeFulfilled = _newHheight - _hheight;
                        Network.Peerbloom.Connection curConn;
                        lock (usablePeers)
                        {
                            curConn = (usablePeers.Count > 0) ? network.GetPeer(usablePeers[0]) : network.GetPeer(bestPeer);
                        }

                        network.SendRequest(curConn, new Network.Core.Packet(Network.Core.PacketType.GETHEADERS, new Network.Core.Packets.GetHeadersPacket { StartingHeight = _hheight + 1, Count = (uint)(_newHheight - _hheight) }), durationMilliseconds: 60000, callback: callback);

                        while (handler.LastSeenHeight < _newHheight && missedItems.Count == 0)
                        {
                            await Task.Delay(100);
                        }

                        while (Interlocked.Read(ref toBeFulfilled) > 0 && Interlocked.Read(ref numTotalFailures) < 10)
                        {
                            lock (missedItems)
                            {
                                if (missedItems.Count > 0)
                                {
                                    lock (usablePeers)
                                    {
                                        curConn = (usablePeers.Count > 0) ? network.GetPeer(usablePeers[0]) : network.GetPeer(bestPeer);
                                    }

                                    network.SendRequest(curConn, new Network.Core.Packet(Network.Core.PacketType.GETHEADERS, new Network.Core.Packets.GetHeadersPacket { StartingHeight = -1, Count = (uint)missedItems.Count, Headers = missedItems.Select(x => x.Hash).ToArray() }), durationMilliseconds: 60000, callback: callback);
                                    missedItems.Clear();
                                }
                            }
                            
                            await Task.Delay(100);
                        }

                        if (Interlocked.Read(ref numTotalFailures) >= 10)
                        {
                            Logger.Fatal($"Daemon.Start: During header syncing, received too many invalid header responses from all connected peers.");
                            return false;
                        }

                        _hheight = _newHheight;
                    }
                }

                Logger.Info($"Grabbed headers; grabbing blocks");

                // restart all data
                handler.LastSeenHeight = dataView.GetChainHeight();
                toBeFulfilled = 0;
                missedItems.Clear();
                numTotalFailures = 0;
                usablePeers.Clear();
                usablePeers.AddRange(baseUsablePeers);
                // block syncing
                /**
                 * Rationale for syncing is as follows:
                 *  - split block grabbing into chunks of 1024
                 *  - grab next 1024 blocks in 15 block steps from peers; track missing blocks
                 *  - if any blocks are missing or invalid ask another peer, tracking invalid blocks
                 *  - if no blocks are valid, wait on that set until valid blocks are found (network will try to discover valid peers)
                 *  - disconnect from peers which serve any invalid blocks
                 */
                if (bestPeer != null)
                {
                    //var bestConn = network.GetPeer(bestPeer);

                    for (long chunk = beginningHeight; chunk < bestHeight; chunk = ((chunk + 1024) <= bestHeight ? chunk + 1024 : bestHeight))
                    {
                        var _height = chunk;
                        var _newHeight = ((chunk + 1024) <= bestHeight ? chunk + 1024 : bestHeight);
                        Logger.Info($"Fetching blocks {chunk + 1} to {_newHeight}");

                        Network.Peerbloom.Connection curConn;
                        lock (usablePeers)
                        {
                            curConn = (usablePeers.Count > 0) ? network.GetPeer(usablePeers[0]) : network.GetPeer(bestPeer);
                        }

                        var headers = messageCache.PopHeaders(_newHeight - chunk);
                        toBeFulfilled = _newHeight - chunk;

                        while (_height < _newHeight)
                        {
                            List<SHA256> blocksToGet = new();

                            var totBytes = 0;
                            long _nextHeight = 0;
                            while (totBytes < 15_000_000 && _nextHeight < _newHeight && headers.Count > 0)
                            {
                                var header = headers.Dequeue();
                                totBytes += (int)header.BlockSize;
                                blocksToGet.Add(header.BlockHash);
                                _nextHeight = header.Height;
                            }

                            if (_nextHeight == _newHeight || headers.Count == 0)
                            {
                                _height = _newHeight;
                            }
                            else
                            {
                                _height = _nextHeight;
                            }

                            var getBlocksPacket = new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Blocks = blocksToGet.ToArray(), Count = (uint)blocksToGet.Count });
                            network.SendRequest(curConn, getBlocksPacket, durationMilliseconds: 60000, callback: callback);

                            while (handler.LastSeenHeight < _nextHeight && missedItems.Count == 0)
                            {
                                await Task.Delay(100);
                            }

                            while (Interlocked.Read(ref toBeFulfilled) > 0 && Interlocked.Read(ref numTotalFailures) < 10)
                            {
                                lock (missedItems)
                                {
                                    if (missedItems.Count > 0)
                                    {
                                        lock (usablePeers)
                                        {
                                            curConn = (usablePeers.Count > 0) ? network.GetPeer(usablePeers[0]) : network.GetPeer(bestPeer);
                                        }

                                        network.SendRequest(curConn, new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Count = (uint)missedItems.Count, Blocks = missedItems.Select(x => x.Hash).ToArray() }), durationMilliseconds: 60000, callback: callback);
                                        missedItems.Clear();
                                    }
                                }

                                await Task.Delay(100);
                            }

                            if (Interlocked.Read(ref numTotalFailures) >= 10)
                            {
                                Logger.Fatal($"Daemon.Start: During block syncing, received too many invalid block responses from all connected peers.");
                                return false;
                            }
                        }

                        // process dem blocks

                        Exception exc = null;
                        var _beginHeight = chunk + 1;
                        do
                        {
                            var vcache = new DB.ValidationCache(messageCache.GetAllCachedBlocks(_beginHeight, _newHeight));
                            (exc, _beginHeight, var goodBlocks, var reget) = vcache.ValidateReturnFailures();

                            if (exc != null && reget != null)
                            {
                                // first, flush valid blocks
                                Logger.Error(exc.Message, exc);
                                Logger.Error($"An invalid block was found during syncing (height {_beginHeight}). Re-requesting all future blocks (up to height {_newHeight}) and flushing valid blocks to disk");
                                vcache.Flush();

                                // publish to ZMQ and syncer queues 
                                if (goodBlocks != null && goodBlocks.Count > 0)
                                {
                                    foreach (var block in goodBlocks)
                                    {
                                        foreach (var toq in syncerQueues)
                                        {
                                            toq.Value.Enqueue(block.Header.BlockHash);
                                        }

                                        ZMQ.Publisher.Instance.Publish("blockhash", block.Header.BlockHash.ToHex());
                                        ZMQ.Publisher.Instance.Publish("blockraw", block.Readable());
                                    }
                                }
                                
                                // begin re-sending requests for blocks
                                toBeFulfilled = reget.Count;

                                var getBlocksPacket = new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Blocks = reget.ToArray(), Count = (uint)reget.Count });
                                network.SendRequest(curConn, getBlocksPacket, durationMilliseconds: 600000, callback: callback);

                                var _nextHeight = reget.Select(x => x.ToInt64()).Max();
                                while (handler.LastSeenHeight < _nextHeight && missedItems.Count == 0)
                                {
                                    await Task.Delay(100);
                                }

                                while (Interlocked.Read(ref toBeFulfilled) > 0 && Interlocked.Read(ref numTotalFailures) < 10)
                                {
                                    lock (missedItems)
                                    {
                                        if (missedItems.Count > 0)
                                        {
                                            lock (usablePeers)
                                            {
                                                curConn = (usablePeers.Count > 0) ? network.GetPeer(usablePeers[0]) : network.GetPeer(bestPeer);
                                            }

                                            network.SendRequest(curConn, new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Count = (uint)missedItems.Count, Blocks = missedItems.Select(x => x.Hash).ToArray() }), durationMilliseconds: 60000, callback: callback);
                                            missedItems.Clear();
                                        }
                                    }

                                    await Task.Delay(100);
                                }

                                if (Interlocked.Read(ref numTotalFailures) >= 10)
                                {
                                    Logger.Fatal($"Daemon.Start: During block syncing, received too many invalid block responses from all connected peers.");
                                    return false;
                                }
                            }
                            else
                            {
                                vcache.Flush();
                            }
                        } while (exc != null);
                    }
                }
                
                /*
                if (bestPeer != null)
                {
                    long _height = dataView.GetChainHeight();
                    beginningHeight = _height;

                    while (_height < bestHeight)
                    {
                        var _newHeight = (_height + 15) <= bestHeight ? _height + 15 : bestHeight;
                        Logger.Info($"Fetching blocks {_height + 1} to {_newHeight}");

                        List<SHA256> blocksToGet = new();

                        while (_height < _newHeight)
                        {
                            blocksToGet.Add(new SHA256(++_height));
                        }
                        
                        network.Send(bestPeer, new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, 
                            new Network.Core.Packets.GetBlocksPacket { Count = (uint)blocksToGet.Count, Blocks = blocksToGet.ToArray() }));

                        while (handler.LastSeenHeight < _newHeight)
                        {
                            //Logger.Log($"Waiting to receive block {_height}...");
                            await Task.Delay(50);
                            //network.Send(bestPeer, new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Count = 1, Blocks = new SHA256[] { new SHA256(_height) } }));
                        }
                    }
                }*/

                // process any new blocks minted during startup
                handler.SetState(Network.PeerState.Processing);

                //var blocks = db.GetBlockCache();

                /*while (blocks.Count > 0)
                {
                    Block block;
                    blocks.Remove(beginningHeight, out block);

                    Logger.Log($"Processing block at height {beginningHeight}...");

                    var err = block.Verify();
                    if (err != null)
                    {
                        Logger.Log($"Block received is invalid ({err.Message}); requesting new block at height {beginningHeight}");
                        await network.Send(bestPeer, new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Count = 1, Blocks = new SHA256[] { new SHA256(beginningHeight) } }));
                    }

                    try
                    {
                        lock (DB.DB.DBLock)
                        {
                            db.AddBlock(block);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log(e.Message);
                    }

                    wallet.ProcessBlock(block);

                    beginningHeight++;
                }*/

                beginningHeight = dataView.GetChainHeight() + 1;

                while (!messageCache.BlockCache.IsEmpty)
                {
                    Block block;
                    messageCache.BlockCache.Remove(beginningHeight, out block);

                    Logger.Log($"Processing block at height {beginningHeight}...");

                    DB.ValidationCache vCache = new DB.ValidationCache(block);
                    var err = vCache.Validate();
                    if (err != null)
                    {
                        Logger.Log($"Block received is invalid ({err.Message}); requesting new block at height {beginningHeight}");
                        network.Send(bestPeer, new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Count = 1, Blocks = new SHA256[] { new SHA256(beginningHeight) } }));
                    }

                    try
                    {
                        vCache.Flush();
                    }
                    catch (Exception e)
                    {
                        Logger.Log(e.Message);
                    }

                    foreach (var toq in syncerQueues)
                    {
                        toq.Value.Enqueue(block.Header.BlockHash);
                    }

                    beginningHeight++;
                }
            }
            else
            {
                Logger.Log($"Starting minter...");
                _ = Minter();

                if (wallets.IsEmpty)
                {
                    Wallet wallet = WalletDB.GetDB().TryGetWallet("DBG_MASTERNODE");

                    wallet.Decrypt("password");

                    wallets.Add(wallet);
                }

                _ = Task.Run(() => WalletSyncer(wallets.First(), true)).ConfigureAwait(false);
            }

            Logger.Log($"Starting handler...");
            handler.SetState(Network.PeerState.Normal);
            ZMQ.Publisher.Instance.Publish("daemonstatechanged", "ready");

            Logger.Log($"Starting peer exchanger...");
            network.StartPeerExchanger();

            Logger.Log($"Starting heartbeater...");
            network.StartHeartbeater();

            Logger.Log($"Daemon startup complete.");

            return true;
        }

        public void ProcessBlock(Block block)
        {
            txpool.UpdatePool(block.Transactions);

            foreach (var toq in syncerQueues)
            {
                toq.Value.Enqueue(block.Header.BlockHash);
            }
            
            ZMQ.Publisher.Instance.Publish("blockhash", block.Header.BlockHash.ToHex());
            ZMQ.Publisher.Instance.Publish("blockraw", block.Readable());
        }

        public async Task WalletSyncer(Wallet wallet, bool scanForFunds)
        {
            CancellationTokenSource _tokenSource = new CancellationTokenSource();
            wallet.syncer = _tokenSource;

            ConcurrentQueue<SHA256> queue = new ConcurrentQueue<SHA256>();

            syncerQueues[wallet] = queue;

            if (scanForFunds)
            {
                wallet.Synced = false;

                var initialChainHeight = dataView.GetChainHeight();

                while (wallet.LastSeenHeight < initialChainHeight && !_tokenSource.IsCancellationRequested)
                {
                    wallet.ProcessBlock(dataView.GetBlock(wallet.LastSeenHeight + 1));
                }

                while ((!queue.IsEmpty || handler.State != Network.PeerState.Normal) && !_tokenSource.IsCancellationRequested)
                {
                    SHA256 blockHash;
                    bool success = queue.TryDequeue(out blockHash);

                    if (!success)
                    {
                        await Task.Delay(100, _tokenSource.Token);
                        continue;
                    }

                    wallet.ProcessBlock(dataView.GetBlock(blockHash));
                }
            }

            wallet.Synced = !_tokenSource.IsCancellationRequested;

            while (!_tokenSource.IsCancellationRequested)
            {
                long _height = dataView.GetChainHeight();

                if (_height > wallet.LastSeenHeight)
                {
                    wallet.Synced = false;

                    for (long k = wallet.LastSeenHeight + 1; k <= _height; k++)
                    {
                        wallet.ProcessBlock(dataView.GetBlock(k));
                    }

                    wallet.Synced = true;
                }

                await Task.Delay(100, _tokenSource.Token);
            }

            syncerQueues.TryRemove(wallet, out _);
        }

        public async Task AddressSyncer(WalletAddress address)
        {
            CancellationTokenSource _tokenSource = new CancellationTokenSource();
            address.syncerSource = _tokenSource;

            ConcurrentQueue<SHA256> queue = new ConcurrentQueue<SHA256>();

            syncerQueues[address] = queue;

            address.Synced = false;
            address.Syncer = true;

            var initialChainHeight = dataView.GetChainHeight();

            while (address.LastSeenHeight < initialChainHeight && address.LastSeenHeight < address.wallet.LastSeenHeight && !_tokenSource.IsCancellationRequested)
            {
                address.ProcessBlock(dataView.GetBlock(address.LastSeenHeight + 1));
            }

            if (address.LastSeenHeight == address.wallet.LastSeenHeight)
            {
                syncerQueues.Remove(address, out _);

                address.Synced = true;
                address.Syncer = false;
                ZMQ.Publisher.Instance.Publish("addresssynced", address.Address);
                return;
            }
            else if (_tokenSource.IsCancellationRequested)
            {
                return;
            }

            while ((!queue.IsEmpty || handler.State != Network.PeerState.Normal) && address.LastSeenHeight < address.wallet.LastSeenHeight && !_tokenSource.IsCancellationRequested)
            {
                SHA256 blockHash;
                bool success = queue.TryDequeue(out blockHash);

                if (!success)
                {
                    await Task.Delay(100, _tokenSource.Token);
                    continue;
                }

                address.ProcessBlock(dataView.GetBlock(blockHash));
            }

            if (address.LastSeenHeight == address.wallet.LastSeenHeight)
            {
                syncerQueues.Remove(address, out _);

                address.Synced = true;
                address.Syncer = false;
                ZMQ.Publisher.Instance.Publish("addresssynced", address.Address);
                return;
            }
            else if (_tokenSource.IsCancellationRequested)
            {
                return;
            }

            while (address.LastSeenHeight < address.wallet.LastSeenHeight && !_tokenSource.IsCancellationRequested)
            {
                while (!queue.IsEmpty && !_tokenSource.IsCancellationRequested && address.LastSeenHeight < address.wallet.LastSeenHeight)
                {
                    SHA256 blockHash;
                    bool success = queue.TryDequeue(out blockHash);

                    if (!success)
                    {
                        await Task.Delay(100, _tokenSource.Token);
                        continue;
                    }

                    address.ProcessBlock(dataView.GetBlock(blockHash));
                }

                await Task.Delay(100, _tokenSource.Token);
            }

            if (_tokenSource.IsCancellationRequested)
            {
                return;
            }

            syncerQueues.Remove(address, out _);

            address.Synced = true;
            address.Syncer = false;
            ZMQ.Publisher.Instance.Publish("addresssynced", address.Address);
            return;
        }

        public async Task Minter()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(5000, _cancellationToken);

                //if (txpool.GetTransactionsForBlock().Count > 0)
                //{
                //    Logger.Log($"Discreet.Daemon: Minting new block...");
                //
                //    Mint();
                //}

                _ = Task.Run(async () => await MintTestnet()).ConfigureAwait(false);
            }
        }

        public void Mint()
        {
            /*if (wallet.Addresses[0].Type != 0)
            {
                Logger.Log("Discreet.Visor.Visor.Mint: Cannot mint a block with transparent wallet!");
                Shutdown();
            }*/

            try
            {
                var txs = txpool.GetTransactionsForBlock();
                //Block blk = Block.Build(txs, (StealthAddress)wallet.Addresses[0].GetAddress(), signingKey);
                Block blk = Block.Build(txs, null, signingKey);

                network.Broadcast(new Network.Core.Packet(Network.Core.PacketType.SENDBLOCK, new Network.Core.Packets.SendBlockPacket { Block = blk }));

                try
                {
                    DB.ValidationCache vCache = new DB.ValidationCache(blk);
                    var vErr = vCache.Validate();
                    if (vErr != null)
                    {
                        Logger.Error($"Discreet.Mint: validating minted block resulted in error: {vErr.Message}", vErr);
                    }
                    vCache.Flush();
                }
                catch (Exception e)
                {
                    Logger.Error(new DatabaseException("Daemon.Mint", e.Message).Message, e);
                }

                ProcessBlock(blk);
            }
            catch (Exception e)
            {
                Logger.Error("Minting block failed: " + e.Message, e);
            }
        }

        public async Task MintTestnet()
        {
            try
            {
                Logger.Info($"Discreet.Daemon: Minting new block...");

                var txs = txpool.GetTransactionsForBlock();
                var blk = Block.Build(txs, (StealthAddress)wallets.First().Addresses[0].GetAddress(), signingKey);

                try
                {
                    DB.ValidationCache vCache = new(blk);
                    var vErr = vCache.Validate();
                    if (vErr != null)
                    {
                        Logger.Error($"Discreet.Mint: validating minted block resulted in error: {vErr.Message}", vErr);
                    }
                    vCache.Flush();
                }
                catch (Exception e)
                {
                    Logger.Error(new DatabaseException("Daemon.Mint", e.Message).Message, e);
                }

                ProcessBlock(blk);

                network.Broadcast(new Network.Core.Packet(Network.Core.PacketType.SENDBLOCK, new Network.Core.Packets.SendBlockPacket { Block = blk }));
            }
            catch (Exception e)
            {
                Logger.Error("Minting block failed: " + e.Message, e);
            }
        }

        public void BuildGenesis()
        {
            /* time to build the megablock */
            //Console.WriteLine("Please, enter the wallets and amounts (input stop token EXIT when finished)");

            //string _input = "";

            List<StealthAddress> addresses = new List<StealthAddress>();
            List<ulong> coins = new List<ulong>();

            Wallet wallet = new Wallet("DBG_MASTERNODE", "password");

            wallet.Save(true);

            wallets.Add(wallet);

            /*while (_input != "EXIT")
            {
                Console.Write("Wallet: ");
                _input = Console.ReadLine();

                if (_input == "EXIT") break;

                addresses.Add(new StealthAddress(_input));

                Console.Write("Coins: ");
                _input = Console.ReadLine();

                if (_input == "EXIT") break;

                coins.Add(ulong.Parse(_input));
            }*/

            addresses.Add(new StealthAddress(wallet.Addresses[0].Address));
            coins.Add(50000000000000000);

            Logger.Log("Creating genesis block...");

            var block = Block.BuildGenesis(addresses.ToArray(), coins.ToArray(), 4096, signingKey);
            DB.ValidationCache vCache = new DB.ValidationCache(block);
            var exc = vCache.Validate();
            if (exc == null)
                Logger.Log("Genesis block successfully created.");
            else
                throw new Exception($"Could not create genesis block: {exc}");

            try
            {
                vCache.Flush();
            }
            catch (Exception e)
            {
                Logger.Log(new DatabaseException("Discreet.Daemon.Daemon.ProcessBlock", e.Message).Message);
            }

            ProcessBlock(block);

            Logger.Log("Successfully created the genesis block.");
        }
    }
}
