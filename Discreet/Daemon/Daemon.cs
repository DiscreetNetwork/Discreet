using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Coin;
using Discreet.WalletsLegacy;
using Discreet.Common.Exceptions;
using Discreet.Cipher;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;
using System.Reflection;
using Discreet.RPC.Common;
using Discreet.Wallets;
using Discreet.Coin.Models;
using Discreet.Common.Serialize;
using Discreet.Daemon.BlockAuth;
using System.Threading.Channels;

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
        TXPool txpool;

        Network.Handler handler;
        Network.Peerbloom.Network network;
        Network.MessageCache messageCache;
        DB.DataView dataView;
        DaemonConfig config;
        SQLiteWallet emissionsWallet;

        RPC.RPCServer _rpcServer;
        Version.VersionBackgroundPoller _versionBackgroundPoller;
        public const string ZMQ_DAEMON_SYNC = "daemonsync";

        CancellationToken _cancellationToken;
        CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public static bool DebugMode { get; set; } = false;

        public bool IsBlockAuthority;

        private Key signingKey;

        public long Uptime { get; private set; }

        public bool RPCLive { get; set; } = false;

        private SemaphoreSlim MintLocker = new(1, 1);

        public Daemon()
        {
            txpool = TXPool.GetTXPool();
            network = Network.Peerbloom.Network.GetNetwork();
            messageCache = Network.MessageCache.GetMessageCache();
            dataView = DB.DataView.GetView();

            config = DaemonConfig.GetConfig();

            _versionBackgroundPoller = new Version.VersionBackgroundPoller();
            _versionBackgroundPoller.UpdateAvailable += (localVersion, newVersion) =>
            {
                Logger.Warn($"New version available: {newVersion.ToString(3)} - currently running on version: {localVersion.ToString(3)}");
            };

            signingKey = Key.FromHex(config.SigningKey);

            if (DefaultBlockAuth.Instance.Keyring.Keys.Any(x => DefaultBlockAuth.Instance.Keyring.SigningKeys.Any(y => KeyOps.ScalarmultBase(y).Equals(x))))
            {
                IsBlockAuthority = DaemonConfig.GetConfig().AuConfig.Author.Value;
            }

            _cancellationToken = _tokenSource.Token;
        }

        public async Task<bool> Restart()
        {
            Logger.Fatal("Daemon could not complete initialization and requires restart.");
            Shutdown();
            await Task.Delay(5000);
            _ = _versionBackgroundPoller.StartBackgroundPoller();

            // re-create everything
            txpool = TXPool.GetTXPool();
            network = Network.Peerbloom.Network.GetNetwork();
            messageCache = Network.MessageCache.GetMessageCache();
            dataView = DB.DataView.GetView();

            config = DaemonConfig.GetConfig();

            signingKey = Key.FromHex(config.SigningKey);

            if (DefaultBlockAuth.Instance.Keyring.Keys.Any(x => DefaultBlockAuth.Instance.Keyring.SigningKeys.Any(y => KeyOps.ScalarmultBase(y).Equals(x))))
            {
                IsBlockAuthority = true;
            }

            _cancellationToken = _tokenSource.Token;
            RPCEndpointResolver.ClearEndpoints();

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
            _versionBackgroundPoller.Stop();
            _rpcServer.Stop();
            ZMQ.Publisher.Instance.Stop();
            _tokenSource.Cancel();
            DefaultBlockAuth.Instance.Stop();
        }

        public async Task<bool> Start()
        {
            Logger.GetLogger().Start(_cancellationToken);

            AppDomain.CurrentDomain.UnhandledException += (sender, e) => Logger.CrashLog(sender, e);
            Logger.Debug("Attached global exception handler.");

            Logger.Info($"Running on version: {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}");
            _ = _versionBackgroundPoller.StartBackgroundPoller();


            Uptime = DateTime.Now.Ticks;
            bool syncFromPeers = !IsBlockAuthority;

            if (IsBlockAuthority && dataView.GetChainHeight() < 0)
            {
                if (!config.MintGenesis.Value)
                {
                    Logger.Critical("Cannot find any data on chain for block authority. Begin syncing from peers.");
                    syncFromPeers = true;
                }
                else
                {
                    await BuildGenesis();
                }
            }

            Logger.Info($"Starting RPC server...");
            _rpcServer = new RPC.RPCServer(DaemonConfig.GetConfig().RPCPort.Value, this);
            _ = _rpcServer.Start();
            _ = Task.Factory.StartNew(async () => await ZMQ.Publisher.Instance.Start(DaemonConfig.GetConfig().ZMQPort.Value));

            Logger.Info($"Starting Block Buffer...");
            _ = Task.Factory.StartNew(async () => await DB.BlockBuffer.Instance.Start());

            await network.Start();
            await network.Bootstrap();
            if (syncFromPeers && IsBlockAuthority)
            {
                Logger.Info("Block authority beginning connecting to peers.");
                await network.ConnectToPeers();
            }
            handler = network.handler;
            handler.daemon = this;

            //Logger.Info($"Starting wallet manager...");
            //_ = Task.Run(() => WalletManager.Instance.Start(_cancellationToken)).ConfigureAwait(false);

            handler.SetState(Network.PeerState.Startup);
            RPCLive = true;
            ZMQ.Publisher.Instance.Publish("daemonstatechanged", "ready");

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

            // needed if a block authority goes offline and blocks are minted/propagated that it doesn't have
            if (bestHeight > dataView.GetChainHeight())
            {
                syncFromPeers = true;
            }

            if (syncFromPeers && !(DaemonConfig.GetConfig().DbgConfig.DebugMode.Value && DaemonConfig.GetConfig().DbgConfig.SkipSyncing.Value))
            {
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

                        byte[] zmqSync = new byte[12];
                        Common.Serialization.CopyData(zmqSync, 0, (int)(_hheight + 1));
                        Common.Serialization.CopyData(zmqSync, 4, (int)(_newHheight));
                        Common.Serialization.CopyData(zmqSync, 8, 0.25f * ((float)(_hheight + 1 - beginningHeight) / (float)(bestHeight - beginningHeight)));
                        ZMQ.Publisher.Instance.Publish(ZMQ_DAEMON_SYNC, zmqSync);

                        toBeFulfilled = _newHheight - _hheight;
                        Network.Peerbloom.Connection curConn;
                        lock (usablePeers)
                        {
                            curConn = (usablePeers.Count > 0) ? network.GetPeer(usablePeers[0]) : network.GetPeer(bestPeer);
                        }

                        network.SendRequest(curConn, new Network.Core.Packet(Network.Core.PacketType.GETHEADERS, new Network.Core.Packets.GetHeadersPacket { StartingHeight = _hheight + 1, Count = (uint)(_newHheight - _hheight) }), durationMilliseconds: 600000, callback: callback);

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

                                    network.SendRequest(curConn, new Network.Core.Packet(Network.Core.PacketType.GETHEADERS, new Network.Core.Packets.GetHeadersPacket { StartingHeight = -1, Count = (uint)missedItems.Count, Headers = missedItems.Select(x => x.Hash).ToArray() }), durationMilliseconds: 600000, callback: callback);
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

                byte[] zmqSyncH = new byte[12];
                Common.Serialization.CopyData(zmqSyncH, 0, (int)(bestHeight));
                Common.Serialization.CopyData(zmqSyncH, 4, (int)(bestHeight));
                Common.Serialization.CopyData(zmqSyncH, 8, 0.25f);
                ZMQ.Publisher.Instance.Publish(ZMQ_DAEMON_SYNC, zmqSyncH);

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

                        byte[] zmqSync = new byte[12];
                        Common.Serialization.CopyData(zmqSync, 0, (int)(chunk + 1));
                        Common.Serialization.CopyData(zmqSync, 4, (int)(_newHeight));
                        Common.Serialization.CopyData(zmqSync, 8, 0.25f + 0.74f * ((float)(chunk + 1 - beginningHeight) / (float)(bestHeight - beginningHeight)));
                        ZMQ.Publisher.Instance.Publish(ZMQ_DAEMON_SYNC, zmqSync);

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

                            var getBlocksPacket = new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Blocks = blocksToGet.ToArray() });
                            network.SendRequest(curConn, getBlocksPacket, durationMilliseconds: 300000, callback: callback);

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

                                        network.SendRequest(curConn, new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Blocks = missedItems.Select(x => x.Hash).ToArray() }), durationMilliseconds: 300000, callback: callback);
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

                        // process blocks

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
                                await vcache.Flush(goodBlocks);

                                // publish to ZMQ and syncer queues 
                                if (goodBlocks != null && goodBlocks.Count > 0)
                                {
                                    foreach (var block in goodBlocks)
                                    {
                                        ZMQ.Publisher.Instance.Publish("blockhash", block.Header.BlockHash.ToHex());
                                        ZMQ.Publisher.Instance.Publish("blockraw", block.Readable());
                                    }
                                }
                                
                                // begin re-sending requests for blocks
                                toBeFulfilled = reget.Count;

                                var getBlocksPacket = new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Blocks = reget.ToArray() });
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

                                            network.SendRequest(curConn, new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Blocks = missedItems.Select(x => x.Hash).ToArray() }), durationMilliseconds: 300000, callback: callback);
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
                                await vcache.Flush();
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

                byte[] zmqSyncB = new byte[12];
                Common.Serialization.CopyData(zmqSyncB, 0, (int)(bestHeight));
                Common.Serialization.CopyData(zmqSyncB, 4, (int)(bestHeight));
                Common.Serialization.CopyData(zmqSyncB, 8, 0.99f);
                ZMQ.Publisher.Instance.Publish(ZMQ_DAEMON_SYNC, zmqSyncB);

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

                    if (block == null)
                    {
                        // this means we are missing a recently published block
                        // re-request all blocks up to current minimum height
                        var minheight = messageCache.BlockCache.Keys.Min();

                        handler.LastSeenHeight = dataView.GetChainHeight();
                        toBeFulfilled = 0;
                        missedItems.Clear();
                        usablePeers.Clear();
                        usablePeers.AddRange(baseUsablePeers);

                        List<SHA256> blocksToGet = new();

                        for (long biter = beginningHeight; biter < minheight; biter++)
                        {
                            blocksToGet.Add(new SHA256(biter));
                        }

                        Network.Peerbloom.Connection curConn;
                        lock (usablePeers)
                        {
                            curConn = (usablePeers.Count > 0) ? network.GetPeer(usablePeers[0]) : network.GetPeer(bestPeer);
                        }

                        var getBlocksPacket = new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Blocks = blocksToGet.ToArray() });
                        network.SendRequest(curConn, getBlocksPacket, durationMilliseconds: 300000, callback: callback);

                        while (handler.LastSeenHeight < (minheight - 1) && missedItems.Count == 0)
                        {
                            await Task.Delay(100);
                        }

                        while (Interlocked.Read(ref toBeFulfilled) > 0)
                        {
                            lock (missedItems)
                            {
                                if (missedItems.Count > 0)
                                {
                                    lock (usablePeers)
                                    {
                                        curConn = (usablePeers.Count > 0) ? network.GetPeer(usablePeers[0]) : network.GetPeer(bestPeer);
                                    }

                                    network.SendRequest(curConn, new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Blocks = missedItems.Select(x => x.Hash).ToArray() }), durationMilliseconds: 300000, callback: callback);
                                    missedItems.Clear();
                                }
                            }

                            await Task.Delay(100);
                        }

                        messageCache.BlockCache.Remove(beginningHeight, out block);
                    }

                    Logger.Info($"Processing block at height {beginningHeight}...", verbose: 2);

                    DB.ValidationCache vCache = new DB.ValidationCache(block);
                    var err = vCache.Validate();
                    if (err != null)
                    {
                        Logger.Error($"Block received is invalid ({err.Message}); requesting new block at height {beginningHeight}", err);
                        network.Send(bestPeer, new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Blocks = new SHA256[] { new SHA256(beginningHeight) } }));
                    }

                    try
                    {
                        await vCache.Flush();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e.Message, e);
                    }

                    beginningHeight++;
                }

                byte[] zmqSyncF = new byte[12];
                Common.Serialization.CopyData(zmqSyncF, 0, (int)(bestHeight));
                Common.Serialization.CopyData(zmqSyncF, 4, (int)(bestHeight));
                Common.Serialization.CopyData(zmqSyncF, 8, 1.0f);
                ZMQ.Publisher.Instance.Publish(ZMQ_DAEMON_SYNC, zmqSyncF);

                Logger.Info("Fetching TXPool...");
                network.Send(messageCache.Versions.Keys.First(), new Network.Core.Packet(Network.Core.PacketType.GETPOOL, new Network.Core.Packets.GetPoolPacket()));
            }
            
            if (IsBlockAuthority)
            {
                Logger.Info("Loading Master Wallet...");

                if (emissionsWallet == null)
                {
                    //Wallet wallet = WalletDB.GetDB().TryGetWallet("TESTNET_EMISSIONS");

                    //wallet.Decrypt("password");

                    try
                    {
                        SQLiteWallet wallet = SQLiteWallet.OpenWallet("TESTNET_EMISSIONS", "password");
                        emissionsWallet = wallet;
                    }
                    catch
                    {
                        // we haven't migrated yet
                        Logger.Critical($"Migrating legacy emissions wallet to new wallet framework...");
                        SQLiteWallet.WalletMigration.Migrate("TESTNET_EMISSIONS", "password");
                        emissionsWallet = SQLiteWallet.Wallets["TESTNET_EMISSIONS"];
                    }
                }

                //WalletsLegacy.WalletManager.Instance.Wallets.Add(emissionsWallet);
                Logger.Info($"Starting minter...");
                var pauseChan = Channel.CreateUnbounded<bool>();
                _ = Task.Run(async () => await DefaultBlockAuth.Instance.Start(DaemonConfig.GetConfig().AuConfig.DataSourcePort.Value, DaemonConfig.GetConfig().AuConfig.FinalizePort.Value, pauseChan.Writer));
                _ = Task.Run(async () => await TestnetMinter(TimeSpan.FromSeconds(1), DaemonConfig.GetConfig().AuConfig.NProc.Value, DaemonConfig.GetConfig().AuConfig.Pid.Value, pauseChan.Reader));
                _ = Task.Run(async () => await BlockReceiver());
                //_ = Minter();

                await Task.Delay(500);
            }

            Logger.Info($"Starting handler...");
            handler.SetState(Network.PeerState.Normal);
            ZMQ.Publisher.Instance.Publish("daemonstatechanged", "ready");

            Logger.Info($"Starting peer exchanger...");
            network.StartPeerExchanger();

            Logger.Info($"Starting heartbeater...");
            network.StartHeartbeater();

            if (DaemonConfig.GetConfig().DbgConfig.CheckBlockchain.Value)
            {
                Logger.Debug("Checking for missing blocks...");
                var blockchainHeight = dataView.GetChainHeight();
                List<long> missingBlocks = new();
                for (long i = 0; i < blockchainHeight; i++)
                {
                    bool success = dataView.TryGetBlock(i, out _);
                    if (!success)
                    {
                        missingBlocks.Add(i);
                    }
                }

                if (missingBlocks.Count > 0)
                {
                    string heights = missingBlocks.Select(x => x.ToString()).Aggregate(string.Empty, (s, x) => s + x + ", ");
                    Logger.Debug($"Missing blocks at the following heights: {heights}");
                }
            }

            Logger.Info($"Daemon startup complete.");

            return true;
        }

        public void ProcessBlock(Block block, bool failed = false)
        {
            txpool.UpdatePool(block.Transactions);

            if (!failed)
            {
                ZMQ.Publisher.Instance.Publish("blockhash", block.Header.BlockHash.ToHex());
                ZMQ.Publisher.Instance.Publish("blockraw", block.Readable());
            }
        }

        public async Task TestnetMinter(TimeSpan interval, int n, int pid, ChannelReader<bool> pause)
        {
            DateTime lastProduced = DateTime.MinValue;
            var ts = DateTime.UtcNow.Ticks;
            DateTime currentBeginning = new DateTime(ts - (ts % interval.Ticks));
            DateTime currentEnd = currentBeginning.Add(interval);
            int numProduced = 0;
            bool paused = true;
            bool reloop = false;

            // we start off paused, waiting for the first call to us
            while (paused)
            {
                if (pause.TryRead(out var unpause) && !unpause)
                {
                    paused = false;
                    break;
                }

                await Task.Delay(20);
            }

            while (!_tokenSource.IsCancellationRequested)
            {
                if (lastProduced == DateTime.MinValue)
                {
                    // update the beginning
                    while (currentEnd < DateTime.UtcNow)
                    {
                        currentBeginning += interval;
                        currentEnd += interval;
                    }
                }

                while (DateTime.UtcNow < currentBeginning)
                {
                    if (pause.TryRead(out var doPause) && doPause)
                    {
                        paused = true;
                        break;
                    }

                    await Task.Delay(20);
                }

                while (paused)
                {
                    if (pause.TryRead(out var unpause) && !unpause)
                    {
                        while (currentEnd < DateTime.UtcNow)
                        {
                            currentBeginning += interval;
                            currentEnd += interval;
                        }

                        paused = false;
                        reloop = true;
                    }
                    else
                    {
                        await Task.Delay(20);
                    }
                }

                if (reloop)
                {
                    reloop = false;
                    continue;
                }

                if (numProduced % n == pid)
                {
                    await MintLocker.WaitAsync();
                    await MintTestnetBlock();
                    MintLocker.Release();
                }

                numProduced++;
                currentBeginning += interval;
                currentEnd += interval;
                lastProduced = DateTime.Now;
            }
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
                await MintLocker.WaitAsync();
                await MintTestnet();
                MintLocker.Release();
            }
        }

        public async Task Mint()
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
                    await vCache.Flush();
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
                Logger.Info($"Discreet.Daemon: Minting new block...", verbose: 1);

                var txs = txpool.GetTransactionsForBlock();
                var blk = Block.Build(txs, new StealthAddress(SQLiteWallet.Wallets["TESTNET_EMISSIONS"].Accounts[0].Address), signingKey);

                try
                {
                    DB.ValidationCache vCache = new(blk);
                    var vErr = vCache.Validate();
                    if (vErr != null)
                    {
                        Logger.Error($"Discreet.Mint: validating minted block resulted in error: {vErr.Message}", vErr);
                    }
                    await vCache.Flush();
                }
                catch (Exception e)
                {
                    Logger.Error(new DatabaseException("Daemon.Mint", e.Message).Message, e);
                    ProcessBlock(blk, true);
                    return;
                }

                ProcessBlock(blk);

                network.Broadcast(new Network.Core.Packet(Network.Core.PacketType.SENDBLOCK, new Network.Core.Packets.SendBlockPacket { Block = blk }));
            }
            catch (Exception e)
            {
                Logger.Error("Minting block failed: " + e.Message, e);
            }
        }

        public async Task MintTestnetBlock()
        {
            try
            {
                Logger.Info($"Discreet.Daemon: Minting new block...", verbose: 1);

                var sigKey = DefaultBlockAuth.Instance.Keyring.SigningKeys[(int)((dataView.GetChainHeight() + 1) % DefaultBlockAuth.Instance.Keyring.SigningKeys.Count)];
                var txs = txpool.GetTransactionsForBlock();
                var blk = Block.Build(txs, new StealthAddress(SQLiteWallet.Wallets["TESTNET_EMISSIONS"].Accounts[0].Address), sigKey);

                try
                {
                    DB.ValidationCache vCache = new(blk);
                    var vErr = vCache.Validate();
                    if (vErr != null)
                    {
                        Logger.Error($"Discreet.Mint: validating minted block resulted in error: {vErr.Message}", vErr);
                    }
                    await vCache.Flush();
                }
                catch (Exception e)
                {
                    Logger.Error(new DatabaseException("Daemon.Mint", e.Message).Message, e);
                    ProcessBlock(blk, true);
                    return;
                }

                ProcessBlock(blk);

                DefaultBlockAuth.Instance.PublishBlockToAurem(blk);
            }
            catch (Exception e)
            {
                Logger.Error("Minting block failed: " + e.Message, e);
            }
        }

        public async Task BuildGenesis()
        {
            /* time to build the megablock */
            //Console.WriteLine("Please, enter the wallets and amounts (input stop token EXIT when finished)");

            //string _input = "";

            List<StealthAddress> addresses = new List<StealthAddress>();
            List<ulong> coins = new List<ulong>();

            //Wallet wallet = new Wallet("TESTNET_EMISSIONS", "password");

            //wallet.Save(true);

            //WalletManager.Instance.Wallets.Add(wallet);

            var wallet = SQLiteWallet.CreateWallet(new Wallets.Models.CreateWalletParameters("TESTNET_EMISSIONS", "password").Scan().SetNumStealthAddresses(1).SetNumTransparentAddresses(0).SetDeterministic().SetEncrypted());

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

            //addresses.Add(new StealthAddress(wallet.Addresses[0].Address));
            addresses.Add(new StealthAddress(wallet.Accounts[0].Address));
            coins.Add(45_000_000_0_000_000_000UL);

            Logger.Info("Creating genesis block...");

            var block = Block.BuildGenesis(addresses.ToArray(), coins.ToArray(), 4096, DefaultBlockAuth.Instance.Keyring.SigningKeys.First());
            DB.ValidationCache vCache = new DB.ValidationCache(block);
            var exc = vCache.Validate();
            if (exc == null)
                Logger.Info("Genesis block successfully created.");
            else
                throw new Exception($"Could not create genesis block: {exc}");

            try
            {
                await vCache.Flush();
            }
            catch (Exception e)
            {
                Logger.Error(new DatabaseException("Discreet.Daemon.Daemon.ProcessBlock", e.Message).Message, e);
            }

            ProcessBlock(block);

            Logger.Info("Successfully created the genesis block.");
        }

        public async Task BlockReceiver()
        {
            TimeSpan clusterTime = TimeSpan.FromMilliseconds(200);
            DateTime clusterDT = DateTime.Now;
            List<Block> clusterBlocks = new List<Block>();

            // old logic for receiving. We now try to cluster blocks if multiple blocks are received in a short timespan.
            //await foreach (var blk in DefaultBlockAuth.Instance.Finalized.ReadAllAsync())
            //{
            //    network.Broadcast(new Network.Core.Packet(Network.Core.PacketType.SENDBLOCK, new Network.Core.Packets.SendBlockPacket { Block = blk }));
            //}

            while (!_tokenSource.IsCancellationRequested)
            {
                var success = DefaultBlockAuth.Instance.Finalized.TryRead(out var blk);
                if (success)
                {
                    clusterBlocks.Add(blk);
                }

                if (DateTime.Now.Subtract(clusterDT) < clusterTime)
                {
                    await Task.Delay(1);
                    continue;
                }
                else if (clusterBlocks.Count > 1)
                {
                    network.Broadcast(new Network.Core.Packet(Network.Core.PacketType.SENDBLOCKS, new Network.Core.Packets.SendBlocksPacket { Blocks = clusterBlocks.OrderBy(x => x.Header.Height).ToArray() }));
                    clusterDT = DateTime.Now;
                    clusterBlocks = new();
                }
                else if (clusterBlocks.Count == 1)
                {
                    network.Broadcast(new Network.Core.Packet(Network.Core.PacketType.SENDBLOCK, new Network.Core.Packets.SendBlockPacket { Block = clusterBlocks.First() }));
                    clusterDT = DateTime.Now;
                    clusterBlocks = new();
                }
                else
                {
                    await Task.Delay(50);
                }
            }
        }

        public static Daemon Init()
        {
            var config = DaemonConfig.GetConfig();
            DebugMode = config.DbgConfig.DebugMode.Value;

            if (config.BootstrapNode == null)
            {
                config.BootstrapNode = "testnet.bootstrap.discreet.net";
                bool didResolve = false;
                bool didReject = false;

                do
                {
                    try
                    {
                        IPAddress[] resolvedDNS;
                        if (didReject)
                        {
                            Console.Write("Boostrap node: ");
                            resolvedDNS = Dns.GetHostAddresses(Console.ReadLine());
                        }
                        else
                        {
                            resolvedDNS = Dns.GetHostAddresses(config.BootstrapNode);
                        }

                        config.BootstrapNode = resolvedDNS[0].ToString();
                        didResolve = true;
                    }
                    catch (Exception)
                    {
                        didReject = true;
                        Logger.Error("Not a valid bootstrap node. Network might be temporarily down for maintenance.");
                    }
                } while (!didResolve);
            }

            DaemonConfig.SetConfig(config);

            config.Save();

            var daemon = new Daemon();
            return daemon;
        }
    }
}
