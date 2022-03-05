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

namespace Discreet.Visor
{
    /* Manages all blockchain operations. Contains the wallet manager, logger, Network manager, RPC manager, DB manager and TXPool manager. */
    public class Visor
    {
        public Wallet wallet;
        TXPool txpool;

        Network.Handler handler;
        Network.Peerbloom.Network network;
        Network.MessageCache messageCache;
        DB.DisDB db;
        VisorConfig config;

        RPC.RPCServer _rpcServer;
        CancellationToken _cancellationToken;
        CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public static bool DebugMode { get; set; } = false;

        public bool IsMasternode;

        private Key signingKey;

        public Visor(Wallet wallet)
        {
            if (wallet.Addresses == null || wallet.Addresses.Length == 0 || wallet.Addresses[0].Type == 1)
            {
                throw new Exception("Discreet.Visor.Visor: Improper wallet for visor!");
            }

            this.wallet = wallet;

            txpool = TXPool.GetTXPool();
            handler = Network.Handler.GetHandler();
            network = Network.Peerbloom.Network.GetNetwork();
            messageCache = Network.MessageCache.GetMessageCache();
            db = DB.DisDB.GetDB();

            handler.visor = this;

            config = new VisorConfig();

            signingKey = new Key(Common.Printable.Byteify("90933561d294e3125c98a90263e1331fc337be71ee3ac9b0d7269728849ac00a"));

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
            handler.SetState(Network.PeerState.Startup);

            if (IsMasternode && db.GetChainHeight() < 0)
            {
                /* time to build the megablock */
                //Console.WriteLine("Please, enter the wallets and amounts (input stop token EXIT when finished)");

                //string _input = "";

                List<StealthAddress> addresses = new List<StealthAddress>();
                List<ulong> coins = new List<ulong>();

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

                addresses.Add(new StealthAddress("1HhjfqY8MKwYFc8mbkKNyKPHK9zTCCdTF59FdUYkHSneXPvYViW5dfUeVXCvm8tufsHiVDQPudspWA2wzctQGz166LAVHU5"));
                addresses.Add(new StealthAddress("1BUVzU7jfQw5UGgWFfcznXRrcUMHXAgERAiCCmr9La2h3hakNVsEMXR5P9L1LJaacKiJg5EeYvxZkYpGZmj5oS3i77LiYR5"));
                coins.Add(10000000000);
                coins.Add(50000000000000000);

                Logger.Log("Creating genesis block...");

                var block = Block.BuildGenesis(addresses.ToArray(), coins.ToArray(), 4096, signingKey);

                Logger.Log((block.Verify() == null).ToString());

                Mint(block);

                Logger.Log("Successfully created the genesis block.");

                handler.SetState(Network.PeerState.Normal);
            }

            Logger.Log($"Starting RPC server...");
            _rpcServer = new RPC.RPCServer(VisorConfig.GetConfig().RPCPort);
            _ = _rpcServer.Start();

            network.Start();
            await network.Bootstrap();

            if (!IsMasternode)
            {
                /* in startup, after bootstrap, the visor must collect version info from all connected peers */
                int nPeers = await network.Broadcast(new Network.Core.Packet(Network.Core.PacketType.GETVERSION, new Network.Core.Packets.GetVersionPacket()));

                if (nPeers == 0)
                {
                    throw new Exception("FATAL: cannot find any online peers. Exiting.");
                }

                long timeout = DateTime.UtcNow.AddSeconds(30).Ticks;

                while (nPeers != messageCache.Versions.Count + messageCache.BadVersions.Count && timeout > DateTime.UtcNow.Ticks)
                {
                    await Task.Delay(10000);

                    Logger.Log("Discreet.Visor: waiting for peers to respond with versions...");

                    await network.Broadcast(new Network.Core.Packet(Network.Core.PacketType.GETVERSION, new Network.Core.Packets.GetVersionPacket()));

                    Logger.Log($"Discreet.Visor: {messageCache.Versions.Count} peers found so far...");
                }

                if (messageCache.Versions.Count == 0)
                {
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
                        await network.SendSync(bestPeer, new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Count = 1, Blocks = new SHA256[] { new SHA256(_height) } }));

                        while (handler.LastSeenHeight < _height)
                        {
                            Logger.Log($"Waiting to receive block {_height}...");
                            await Task.Delay(1000);
                            await network.SendSync(bestPeer, new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Count = 1, Blocks = new SHA256[] { new SHA256(_height) } }));
                        }
                    }
                }

                await Task.Delay(10000);

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

                while (!messageCache.BlockCache.IsEmpty)
                {
                    beginningHeight += 1;

                    Block block;
                    messageCache.BlockCache.Remove(beginningHeight, out block);

                    Logger.Log($"Processing block at height {beginningHeight}...");

                    var err = block.Verify();
                    if (err != null)
                    {
                        Logger.Log($"Block received is invalid ({err.Message}); requesting new block at height {beginningHeight}");
                        await network.Send(bestPeer, new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Count = 1, Blocks = new SHA256[] { new SHA256(beginningHeight) } }));
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

                    wallet.ProcessBlock(block);

                    beginningHeight++;
                }
            }
            else
            {
                Logger.Log($"Starting minter...");
                _ = Minter();
            }

            Logger.Log($"Starting handler...");
            handler.SetState(Network.PeerState.Normal);

            if (!DebugMode)
            {
                Logger.Log($"Starting heartbeater...");
                _ = network.Heartbeat();
            }

            Logger.Log($"Visor startup complete.");

            while (!_cancellationToken.IsCancellationRequested)
            {
                
            }
        }

        public void ProcessBlock(Block block)
        {
            txpool.UpdatePool(block.Transactions);

            wallet.ProcessBlock(block);
        }

        public async Task SendTransaction(FullTransaction tx)
        {
            await network.Broadcast(new Network.Core.Packet(Network.Core.PacketType.SENDTX, new Network.Core.Packets.SendTransactionPacket { Tx = tx }));
        }

        public async Task Minter()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000);

                if (txpool.GetTransactionsForBlock().Count > 0)
                {
                    Logger.Log($"Discreet.Visor: Minting new block...");

                    await Mint();
                }
            }
        }

        public async Task Mint()
        {
            if (wallet.Addresses[0].Type != 0)
            {
                Logger.Log("Discreet.Visor.Visor.Mint: Cannot mint a block with transparent wallet!");
                Shutdown();
            }

            try
            {
                var txs = txpool.GetTransactionsForBlock();
                Block blk = Block.Build(txs, (StealthAddress)wallet.Addresses[0].GetAddress(), signingKey);

                await network.Broadcast(new Network.Core.Packet(Network.Core.PacketType.SENDBLOCK, new Network.Core.Packets.SendBlockPacket { Block = blk }));

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
            if (wallet.Addresses[0].Type != 0) throw new Exception("Discreet.Visor.Visor.Mint: Cannot mint a block with transparent wallet!");

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
