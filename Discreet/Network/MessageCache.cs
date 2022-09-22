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
            for (long i = _headerMin; i < _headerMin + max && i <= _headerMax; i++)
            {
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
