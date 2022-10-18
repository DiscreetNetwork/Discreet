using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Network.Core.Packets;
using System.Net;
using System.Collections.Concurrent;

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

        public ConcurrentBag<string> Messages;
        public ConcurrentBag<RejectPacket> Rejections;
        public ConcurrentBag<AlertPacket> Alerts;
        public ConcurrentDictionary<IPEndPoint, Core.Packets.Peerbloom.VersionPacket> Versions;
        public ConcurrentDictionary<IPEndPoint, Core.Packets.Peerbloom.VersionPacket> BadVersions;
        public ConcurrentDictionary<long, Coin.Block> BlockCache;
        public ConcurrentDictionary<long, Coin.BlockHeader> HeaderCache;
        private long _headerMin = -1;
        private long _headerMax = -1;

        public ConcurrentDictionary<Cipher.SHA256, Coin.Block> OrphanBlocks;
        public ConcurrentDictionary<Cipher.SHA256, int> OrphanBlockParents = new(new Cipher.SHA256EqualityComparer());

        public MessageCache()
        {
            Messages = new ConcurrentBag<string>();
            Rejections = new ConcurrentBag<RejectPacket>();
            Alerts = new ConcurrentBag<AlertPacket>();
            Versions = new ConcurrentDictionary<IPEndPoint, Core.Packets.Peerbloom.VersionPacket>();
            BadVersions = new ConcurrentDictionary<IPEndPoint, Core.Packets.Peerbloom.VersionPacket>();
            BlockCache = new ConcurrentDictionary<long, Coin.Block>();
            OrphanBlocks = new ConcurrentDictionary<Cipher.SHA256, Coin.Block>();
            HeaderCache = new ConcurrentDictionary<long, Coin.BlockHeader>();
        }

        public bool AddHeaderToCache(Coin.BlockHeader header)
        {
            var dataView = DB.DataView.GetView();
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

        public List<Coin.Block> GetAllCachedBlocks(long startingHeight, long endingHeight)
        {
            List<Coin.Block> blocks = new();

            for (long i = startingHeight; i <= endingHeight; i++)
            {
                bool success = BlockCache.Remove(i, out var block);

                if (!success) Daemon.Logger.Debug($"MessageCache.GetAllCachedBlocks: error in syncer implementation; could not get block at height {i}");

                blocks.Add(block);
            }

            return blocks;
        }

        public Queue<Coin.BlockHeader> PopHeaders(long max)
        {
            if (_headerMin == -1)
            {
                _headerMin = HeaderCache.Keys.Min();
            }

            if (_headerMax == -1)
            {
                _headerMax = HeaderCache.Keys.Max();
            }

            Queue<Coin.BlockHeader> headers = new();
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
    }
}
