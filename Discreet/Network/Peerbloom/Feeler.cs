using System;
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
            Connection conn = new Connection(p.Endpoint, _network, _network.LocalNode);

            bool success = await conn.Connect(false, token);

            if (!success)
            {
                if (p.InTried)
                {
                    _peerlist.Attempt(p.Endpoint, true);
                    return;
                }

                uint bucket = _peerlist.GetNewBucket(p.Endpoint, p.Source);
                // for now, we evict the peer from new instead of lazily allowing for it to become bad
                _peerlist.ClearNew(bucket, _peerlist.GetBucketPosition(true, bucket, p.Endpoint));
            }
            else
            {
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

        public async Task Start(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var timer = DateTime.UtcNow.AddSeconds(Constants.PEERBLOOM_FEELER_INTERVAL).Ticks;

                while (timer > DateTime.UtcNow.Ticks && !token.IsCancellationRequested)
                {
                    await Task.Delay(500);
                }

                if (token.IsCancellationRequested) return;

                Daemon.Logger.Debug($"Feeler: beginning feelers...");

                while (_network.Feelers.Count < Constants.PEERBLOOM_MAX_FEELERS)
                {
                    var peer = Select();

                    if (peer == null) break;

                    _ = Task.Run(() => Feel(peer, token)).ConfigureAwait(false);
                }
            }
        }
    }
}
