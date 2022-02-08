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

namespace Discreet.Visor
{
    /* Manages all blockchain operations. Contains the wallet manager, logger, Network manager, RPC manager, DB manager and TXPool manager. */
    public class Visor
    {
        // WIP
        Wallet wallet;
        TXPool txpool;

        Network.Handler handler;
        Network.Peerbloom.Network network;
        Network.MessageCache messageCache;
        DB.DB db;
        VisorConfig config;

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
            db = DB.DB.GetDB();

            handler.visor = this;

            config = new VisorConfig();
        }

        public Network.Handler GetHandler()
        {
            return handler;
        }

        public async Task Start()
        {
            handler.SetState(Network.PeerState.Startup);

            network.Start();
            await network.Bootstrap();

            /* in startup, after bootstrap, the visor must collect version info from all connected peers */
            int nPeers = await network.Broadcast(new Network.Core.Packet(Network.Core.PacketType.GETVERSION, new Network.Core.Packets.GetVersionPacket()));

            if (nPeers == 0)
            {
                throw new Exception("FATAL: cannot find any online peers. Exiting.");
            }

            long timeout = DateTime.UtcNow.AddMinutes(5).Ticks;

            while (nPeers != messageCache.Versions.Count + messageCache.BadVersions.Count && timeout > DateTime.UtcNow.Ticks)
            {
                await Task.Delay(10000);

                Logger.Log("Discreet.Visor: waiting for peers to respond with versions...");
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
                    _height += 1;
                    await network.Send(bestPeer, new Network.Core.Packet(Network.Core.PacketType.GETBLOCKS, new Network.Core.Packets.GetBlocksPacket { Count = 1, Blocks = new SHA256[] { new SHA256(_height) } }));
                }
            }

            handler.SetState(Network.PeerState.Processing);

            var blocks = db.GetBlockCache();

            while (blocks.Count > 0)
            {
                Block block;
                blocks.Remove(beginningHeight, out block);

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
            }

            while (!messageCache.BlockCache.IsEmpty)
            {
                Block block;
                messageCache.BlockCache.Remove(beginningHeight, out block);

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
            }

            handler.SetState(Network.PeerState.Normal);

            RPC.RPCServer _rpcServer = new RPC.RPCServer(VisorConfig.GetConfig().RPCPort);

            _ = _rpcServer.Start();

            while (true)
            {
                if (_rpcServer.)
            }
        }

        public void ProcessBlock(Block block)
        {
            wallet.ProcessBlock(block);
        }

        public void Mint()
        {
            if (wallet.Addresses[0].Type != 0) throw new Exception("Discreet.Visor.Visor.Mint: Cannot mint a block with transparent wallet!");

            Block blk = Block.Build(txpool.GetTransactionsForBlock(), (StealthAddress)wallet.Addresses[0].GetAddress());

            //propagate

            ProcessBlock(blk);
        }
    }
}
