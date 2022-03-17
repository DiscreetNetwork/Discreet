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
    /* Manages all blockchain operations. Contains the wallet manager, logger, Network manager, RPC manager, DB manager and TXPool manager. */
    public class Daemon
    {
        public ConcurrentBag<Wallet> wallets;
        TXPool txpool;

        Network.Handler handler;
        Network.Peerbloom.Network network;
        Network.MessageCache messageCache;
        DB.DisDB db;
        DaemonConfig config;

        RPC.RPCServer _rpcServer;
        CancellationToken _cancellationToken;
        CancellationTokenSource _tokenSource = new CancellationTokenSource();

        ConcurrentDictionary<object, ConcurrentQueue<SHA256>> syncerQueues;

        public static bool DebugMode { get; set; } = false;

        public bool IsMasternode;

        private Key signingKey;

        public long Uptime { get; private set; }

        public Daemon()
        {
            wallets = new ConcurrentBag<Wallet>();

            txpool = TXPool.GetTXPool();
            network = Network.Peerbloom.Network.GetNetwork();
            messageCache = Network.MessageCache.GetMessageCache();
            db = DB.DisDB.GetDB();

            config = new DaemonConfig();

            signingKey = new Key(Common.Printable.Byteify("90933561d294e3125c98a90263e1331fc337be71ee3ac9b0d7269728849ac00a"));

            syncerQueues = new ConcurrentDictionary<object, ConcurrentQueue<SHA256>>();

            _cancellationToken = _tokenSource.Token;
        }

        public Network.Handler GetHandler()
        {
            return handler;
        }

        public void Shutdown()
        {
            network.Shutdown();

            _rpcServer.Stop();

            _tokenSource.Cancel();
        }

        public async Task Start()
        {
            Uptime = DateTime.Now.Ticks;

            if (IsMasternode && db.GetChainHeight() < 0)
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

                Logger.Log((block.Verify() == null).ToString());

                Mint(block);

                Logger.Log("Successfully created the genesis block.");
            }

            await network.Start();
            await network.Bootstrap();
            handler = network.handler;
            handler.daemon = this;

            handler.SetState(Network.PeerState.Startup);

            Logger.Log($"Starting RPC server...");
            _rpcServer = new RPC.RPCServer(DaemonConfig.GetConfig().RPCPort.Value);
            _ = _rpcServer.Start();

            if (!IsMasternode)
            {
                /* in startup, after bootstrap, the visor must collect version info from all connected peers */
                int nPeers = network.Broadcast(new Network.Core.Packet(Network.Core.PacketType.GETVERSION, new Network.Core.Packets.GetVersionPacket()));

                if (nPeers == 0)
                {
                    throw new Exception("FATAL: cannot find any online peers. Exiting.");
                }

                long timeout = DateTime.UtcNow.AddSeconds(5).Ticks;

                await Task.Delay(500);

                while (nPeers != messageCache.Versions.Count + messageCache.BadVersions.Count && timeout > DateTime.UtcNow.Ticks)
                {
                    await Task.Delay(500);

                    Logger.Log("Discreet.Visor: waiting for peers to respond with versions...");

                    network.Broadcast(new Network.Core.Packet(Network.Core.PacketType.GETVERSION, new Network.Core.Packets.GetVersionPacket()));

                    Logger.Log($"Discreet.Visor: {messageCache.Versions.Count} peers found so far...");
                }

                if (messageCache.Versions.Count == 0)
                {
                    if (messageCache.BadVersions.Count > 0)
                    {
                        throw new Exception($"FATAL: could not connect to any valid peers. {messageCache.BadVersions.Count} invalid peers found.");
                    }

                    throw new Exception("FATAL: no valid peers online. Exiting.");
                }

                /* get height of chain */
                long bestHeight = db.GetChainHeight();
                IPEndPoint bestPeer = null;

                foreach (var ver in messageCache.Versions)
                {
                    if (ver.Value.Height > bestHeight)
                    {
                        bestPeer = ver.Key;
                        bestHeight = ver.Value.Height;
                    }
                }

                handler.SetState(Network.PeerState.Syncing);

                long beginningHeight = 0;

                if (bestPeer != null)
                {
                    long _height = db.GetChainHeight();
                    beginningHeight = _height;

                    while (_height != bestHeight)
                    {
                        Logger.Log($"Attemtping to get block {_height + 1}...");
                        _height += 1;
                        network.Send(bestPeer, new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Count = 1, Blocks = new SHA256[] { new SHA256(_height) } }));

                        while (handler.LastSeenHeight < _height)
                        {
                            Logger.Log($"Waiting to receive block {_height}...");
                            await Task.Delay(1000);
                            network.Send(bestPeer, new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Count = 1, Blocks = new SHA256[] { new SHA256(_height) } }));
                        }
                    }
                }

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

                beginningHeight += 1;

                while (!messageCache.BlockCache.IsEmpty)
                {
                    Block block;
                    messageCache.BlockCache.Remove(beginningHeight, out block);

                    Logger.Log($"Processing block at height {beginningHeight}...");

                    var err = block.Verify();
                    if (err != null)
                    {
                        Logger.Log($"Block received is invalid ({err.Message}); requesting new block at height {beginningHeight}");
                        network.Send(bestPeer, new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Count = 1, Blocks = new SHA256[] { new SHA256(beginningHeight) } }));
                    }

                    try
                    {
                        lock (DB.DisDB.DBLock)
                        {
                            db.AddBlock(block);
                        }
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

            Logger.Log($"Starting peer exchanger...");
            network.StartPeerExchanger();

            Logger.Log($"Starting heartbeater...");
            network.StartHeartbeater();

            Logger.Log($"Visor startup complete.");

            while (!_cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, _cancellationToken);
            }
        }

        public void ProcessBlock(Block block)
        {
            txpool.UpdatePool(block.Transactions);

            foreach (var toq in syncerQueues)
            {
                toq.Value.Enqueue(block.Header.BlockHash);
            }
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

                var initialChainHeight = db.GetChainHeight();

                while (wallet.LastSeenHeight < initialChainHeight && !_tokenSource.IsCancellationRequested)
                {
                    wallet.ProcessBlock(db.GetBlock(wallet.LastSeenHeight + 1));
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

                    wallet.ProcessBlock(db.GetBlock(blockHash));
                }
            }

            wallet.Synced = !_tokenSource.IsCancellationRequested;

            while (!_tokenSource.IsCancellationRequested)
            {
                while (!queue.IsEmpty && !_tokenSource.IsCancellationRequested)
                {
                    SHA256 blockHash;
                    bool success = queue.TryDequeue(out blockHash);

                    if (!success)
                    {
                        Logger.Fatal("Discreet.Visor.Visor.WalletSyncer: fatal error encountered while trying to dequeue");
                        return;
                    }

                    wallet.ProcessBlock(db.GetBlock(blockHash));
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

            var initialChainHeight = db.GetChainHeight();

            while (address.LastSeenHeight < initialChainHeight && address.LastSeenHeight < address.wallet.LastSeenHeight && _tokenSource.IsCancellationRequested)
            {
                address.ProcessBlock(db.GetBlock(address.LastSeenHeight + 1));
            }

            if (address.LastSeenHeight == address.wallet.LastSeenHeight)
            {
                syncerQueues.Remove(address, out _);

                address.Synced = true;
                address.Syncer = false;
                return;
            }
            else if (_tokenSource.IsCancellationRequested)
            {
                return;
            }

            while ((!queue.IsEmpty || handler.State != Network.PeerState.Normal) && address.LastSeenHeight < address.wallet.LastSeenHeight && _tokenSource.IsCancellationRequested)
            {
                SHA256 blockHash;
                bool success = queue.TryDequeue(out blockHash);

                if (!success)
                {
                    await Task.Delay(100, _tokenSource.Token);
                    continue;
                }

                address.ProcessBlock(db.GetBlock(blockHash));
            }

            if (address.LastSeenHeight == address.wallet.LastSeenHeight)
            {
                syncerQueues.Remove(address, out _);

                address.Synced = true;
                address.Syncer = false;
                return;
            }
            else if (_tokenSource.IsCancellationRequested)
            {
                return;
            }

            while (address.LastSeenHeight < address.wallet.LastSeenHeight && _tokenSource.IsCancellationRequested)
            {
                while (!queue.IsEmpty && _tokenSource.IsCancellationRequested && address.LastSeenHeight < address.wallet.LastSeenHeight)
                {
                    SHA256 blockHash;
                    bool success = queue.TryDequeue(out blockHash);

                    if (!success)
                    {
                        await Task.Delay(100, _tokenSource.Token);
                        continue;
                    }

                    address.ProcessBlock(db.GetBlock(blockHash));
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
            return;
        }

        public async Task SendTransaction(FullTransaction tx)
        {
            network.Broadcast(new Network.Core.Packet(Network.Core.PacketType.SENDTX, new Network.Core.Packets.SendTransactionPacket { Tx = tx }));
        }

        public async Task Minter()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, _cancellationToken);

                if (txpool.GetTransactionsForBlock().Count > 0)
                {
                    Logger.Log($"Discreet.Visor: Minting new block...");

                    await Mint();
                }
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
                    lock (DB.DisDB.DBLock)
                    {
                        db.AddBlock(blk);
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(new DatabaseException("Discreet.Visor.Visor.ProcessBlock", e.Message).Message);
                }

                ProcessBlock(blk);
            }
            catch (Exception e)
            {
                Logger.Log("Minting block failed: " + e.Message);
            }
        }

        public void Mint(Block blk)
        {
            //if (wallet.Addresses[0].Type != 0) throw new Exception("Discreet.Visor.Visor.Mint: Cannot mint a block with transparent wallet!");

            DB.DisDB db = DB.DisDB.GetDB();

            try
            {
                lock (DB.DisDB.DBLock)
                {
                    db.AddBlock(blk);
                }
            }
            catch (Exception e)
            {
                Logger.Log(new DatabaseException("Discreet.Visor.Visor.ProcessBlock", e.Message).Message);
            }

            ProcessBlock(blk);
        }
    }
}
