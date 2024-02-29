using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Network.Core.Packets;
using System.Net;
using System.Collections.Concurrent;
using Discreet.Coin.Models;
using Discreet.DB;
using System.Threading;

namespace Discreet.Network
{
    public class MessageCache
    {
        private static MessageCache _messageCache;
        private static object _messageCacheLock = new object();

        public static MessageCache GetMessageCache()
        {
            lock (_messageCacheLock)
            {
                if (_messageCache == null) _messageCache = new MessageCache();

                return _messageCache;
            }
        }

        // TODO: February 27 2024 9:30 PM - look into LRU caching for these; simple finite size ConcurrentQueue would work.
        public ConcurrentBag<string> Messages;
        public ConcurrentBag<RejectPacket> Rejections;
        public HashSet<AlertPacket> Alerts;
        public ConcurrentDictionary<IPEndPoint, Core.Packets.Peerbloom.VersionPacket> Versions;
        public ConcurrentDictionary<IPEndPoint, Core.Packets.Peerbloom.VersionPacket> BadVersions;
        public ConcurrentDictionary<long, Block> BlockCache;
        public ConcurrentDictionary<long, BlockHeader> HeaderCache;
        private long _headerMin = -1;
        private long _headerMax = -1;

        public ConcurrentDictionary<Cipher.SHA256, Block> OrphanBlocks;
        public ConcurrentDictionary<Cipher.SHA256, Cipher.SHA256> OrphanBlockParents = new(new Cipher.SHA256EqualityComparer());
        public readonly SemaphoreSlim OrphanLock = new SemaphoreSlim(1, 1);

        public MessageCache()
        {
            Messages = new ConcurrentBag<string>();
            Rejections = new ConcurrentBag<RejectPacket>();
            Alerts = new HashSet<AlertPacket>(new Core.Packets.Comparers.AlertEqualityComparer());
            Versions = new ConcurrentDictionary<IPEndPoint, Core.Packets.Peerbloom.VersionPacket>();
            BadVersions = new ConcurrentDictionary<IPEndPoint, Core.Packets.Peerbloom.VersionPacket>();
            BlockCache = new ConcurrentDictionary<long, Block>();
            OrphanBlocks = new ConcurrentDictionary<Cipher.SHA256, Block>(new Cipher.SHA256EqualityComparer());
            HeaderCache = new ConcurrentDictionary<long, BlockHeader>();
        }

        public bool AddHeaderToCache(BlockHeader header)
        {
            var dataView = BlockBuffer.Instance;
            var _curHeight = dataView.GetChainHeight();

            if (header == null) return false;

            if (HeaderCache.IsEmpty)
            {
                if (header.Height != _curHeight + 1) return false;

                if (_curHeight == -1)
                {
                    // genesis header rules
                    if (!header.PreviousBlock.Equals(new Cipher.SHA256(new byte[32], false))) return false;
                }
                else
                {
                    var prev = dataView.GetBlockHeader(_curHeight);

                    if (prev.BlockHash != header.PreviousBlock) return false;
                }
            }
            else
            {
                bool succ = HeaderCache.TryGetValue(_headerMax, out var prev);
                if (!succ || prev == null) return false;

                if (prev.BlockHash != header.PreviousBlock) return false;
            }

            if ((long)header.Timestamp > DateTime.UtcNow.AddHours(2).Ticks) return false;
            if (header.NumTXs == 0 || header.BlockSize == 0) return false;
            if (!header.CheckSignature()) return false;

            if (HeaderCache.IsEmpty)
            {
                _headerMax = _headerMin = header.Height;
            }
            else
            {
                _headerMax = header.Height;
            }

            if (HeaderCache.ContainsKey(header.Height)) return false;

            bool asucc = HeaderCache.TryAdd(header.Height, header);
            if (!asucc) return false;

            return true;
        }

        public (bool, string) AddBlockToCache(Block blk)
        {
            if (BlockCache.ContainsKey(blk.Header.Height))
            {
                Daemon.Logger.Error($"AddBlockToCache: Block {blk.Header.BlockHash.ToHexShort()} (height {blk.Header.Height}) already in database!", verbose: 3);
                return (true, "");
            }

            if (blk.Transactions == null || blk.Transactions.Length == 0 || blk.Header.NumTXs == 0)
            {
                Daemon.Logger.Error($"AddBlockToCache: Block {blk.Header.BlockHash.ToHexShort()} has no transactions!", verbose: 2);
                return (false, "block has no transactions");
            }

            if ((long)blk.Header.Timestamp > DateTime.UtcNow.AddHours(2).Ticks)
            {
                Daemon.Logger.Error($"AddBlockToCache: Block {blk.Header.BlockHash.ToHexShort()} from too far in the future!", verbose: 1);
                return (false, "block too far from future");
            }

            /* unfortunately, we can't check the transactions yet, since some output indices might not be present. We check a few things though. */
            foreach (FullTransaction tx in blk.Transactions)
            {
                if ((!tx.HasInputs() || !tx.HasOutputs()) && (tx.Version != 0))
                {
                    Daemon.Logger.Error($"AddBlockToCache: Block {blk.Header.BlockHash.ToHexShort()} has a transaction without inputs or outputs!", verbose: 1);
                    return (false, "invalid transactions");
                }
            }

            if (blk.GetMerkleRoot() != blk.Header.MerkleRoot)
            {
                Daemon.Logger.Error($"AddBlockToCache: Block {blk.Header.BlockHash.ToHexShort()} has invalid Merkle root", verbose: 1);
                return (false, "invalid merkle root");
            }

            if (!blk.CheckSignature())
            {
                Daemon.Logger.Error($"AddBlockToCache: Block {blk.Header.BlockHash.ToHexShort()} has missing or invalid signature!");
                return (false, "missing or invalid signature");
            }

            BlockCache[blk.Header.Height] = blk;

            return (true, "");
        }

        public List<Block> GetAllCachedBlocks(long startingHeight, long endingHeight)
        {
            List<Block> blocks = new();

            for (long i = startingHeight; i <= endingHeight; i++)
            {
                bool success = BlockCache.Remove(i, out var block);

                if (!success) Daemon.Logger.Debug($"MessageCache.GetAllCachedBlocks: error in syncer implementation; could not get block at height {i}");

                blocks.Add(block);
            }

            return blocks;
        }

        public Queue<BlockHeader> PopHeaders(long max)
        {
            if (_headerMin == -1)
            {
                _headerMin = HeaderCache.Keys.Min();
            }

            if (_headerMax == -1)
            {
                _headerMax = HeaderCache.Keys.Max();
            }

            Queue<BlockHeader> headers = new();
            var maxHeight = _headerMin + max;
            for (long i = _headerMin; i < maxHeight; i++)
            {
                if (i > _headerMax) break;

                var success = HeaderCache.Remove(i, out var header);
                if (!success)
                {
                    Daemon.Logger.Warn($"MessageCache.PopHeaders: missing header found; error in header syncing");
                }

                headers.Enqueue(header);
                _headerMin++;
            }

            return headers;
        }

        public void AddAlert(AlertPacket p)
        {
            lock (Alerts)
            {
                Alerts.Add(p);
            }
        }
    }
}
