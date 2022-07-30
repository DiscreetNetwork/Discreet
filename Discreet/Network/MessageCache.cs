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
        }
    }
}
