﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom
{
    public class Feeler
    {
        public enum FeelerPriorityLevel: int
        {
            MIN = 0,
            NEW = 1,
            COLLISION = 2,
            MAX = 3,
        }

        public ConcurrentDictionary<FeelerPriorityLevel, ConcurrentQueue<Peer>> FeelerPriorityQueue = new();

        private Network _network;
        private Peerlist _peerlist;

        public Feeler(Network network, Peerlist peerlist) 
        {
            foreach (var lvl in Enum.GetValues(typeof(FeelerPriorityLevel)).Cast<FeelerPriorityLevel>())
            {
                FeelerPriorityQueue[lvl] = new();
            }

            _network = network;
            _peerlist = peerlist;
        }

        public bool Enqueue(FeelerPriorityLevel level, Peer peer)
        {
            if (FeelerPriorityQueue[level] == null) return false;

            if (FeelerPriorityQueue[level].Contains(peer)) return false;

            FeelerPriorityQueue[level].Enqueue(peer);
            peer.LastAttempt = 0;

            return true;
        }

        public async Task Feel(Peer p, CancellationToken token = default)
        {
            Daemon.Logger.Debug($"Feeler.Feel: testing peer {p.Endpoint}...");
            
            Connection conn = new Connection(p.Endpoint, _network, _network.LocalNode, _peer: p);

            bool success = await conn.Connect(false, token, true);

            if (!success)
            {
                Daemon.Logger.Debug($"Feeler.Feel: peer {p.Endpoint} failed feel attempt");
                if (p.InTried)
                {
                    _peerlist.Attempt(p.Endpoint, true);

                    if (p.IsTerrible())
                    {
                        _peerlist.ClearTried(p);
                    }

                    return;
                }

                uint bucket = _peerlist.GetNewBucket(p.Endpoint, p.Source);
                // for now, we evict the peer from new instead of lazily allowing for it to become bad
                _peerlist.ClearNew(bucket, _peerlist.GetBucketPosition(true, bucket, p.Endpoint));
            }
            else
            {
                Daemon.Logger.Debug($"Feeler.Feel: peer {p.Endpoint} succeeded feel attempt; adding to tried");
                _peerlist.Good(p.Endpoint, true);
            }
        }

        public Peer Select()
        {
            Peer rv;
            for (int i = (int)FeelerPriorityLevel.MAX - 1; i > (int)FeelerPriorityLevel.MIN; i--)
            {
                FeelerPriorityQueue[(FeelerPriorityLevel)i].TryDequeue(out rv);

                if (rv != null) return rv;
            }

            long _lastAttempt;
            do
            {
                (rv, _lastAttempt) = _peerlist.Select(true);

                if (rv == null) return null;
            }
            while (DateTime.UtcNow.Ticks - _lastAttempt < Constants.PEERLIST_RECENT_TRY * 10_000_000L);

            return rv;
        }

        /// <summary>
        /// Starts the Feeler.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task Start(CancellationToken token)
        {
            while (!token.IsCancellationRequested && (Handler.GetHandler() == null || Handler.GetHandler().State != PeerState.Normal))
            {
                await Task.Delay(500, token);
            }

            while (!token.IsCancellationRequested)
            {
                var timer = DateTime.UtcNow.AddSeconds(Constants.PEERBLOOM_FEELER_INTERVAL).Ticks;

                while (timer > DateTime.UtcNow.Ticks && !token.IsCancellationRequested)
                {
                    await Task.Delay(500, token);
                }

                if (token.IsCancellationRequested) return;

                Daemon.Logger.Debug($"Feeler: beginning feelers...");

                int newFeelers = Constants.PEERBLOOM_MAX_FEELERS - _network.Feelers.Count;
                List<Peer> selected = new List<Peer>();
                int tried = 0; // used to prevent infinite loops, happens in the case of too few peers in tried or new

                while (selected.Count < newFeelers && tried < Constants.PEERBLOOM_MAX_FEELERS + 2)
                {
                    var peer = Select();
                    if (peer == null) break;
                    tried++;

                    // we disallow selecting already connected endpoints AND addresses.
                    if (selected.Contains(peer) || _network.GetPeer(peer.Endpoint) != null || _network.GetPeerByAddress(peer.Endpoint.Address) != null) continue;

                    // don't test peers recently attempted, i.e. in the last hour
                    if ((peer.LastAttempt + 10_000_000L * 3600L) > DateTime.UtcNow.Ticks) { tried++; continue; }

                    selected.Add(peer);
                    _ = Task.Run(() => Feel(peer, token), token).ConfigureAwait(false);

                    await Task.Delay(100, token);
                }
            }
        }
    }
}
