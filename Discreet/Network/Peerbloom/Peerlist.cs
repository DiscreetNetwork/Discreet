using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom
{
    public class Peerlist
    {
        public class Peer
        {
            public IPEndPoint Endpoint { get; set; }
            public IPEndPoint Source { get; set; }
            public long LastSeen { get; set; }
            public long FirstSeen { get; set; }

            public bool InTried { get; set; }

            public int NumFailedConnectionAttempts { get; set; }

            public long LastSuccess { get; set; }
            public long LastAttempt { get; set; }

            // how many times this occurs in the NEW set
            public int RefCount { get; set; }

            public byte[] Serialize()
            {
                byte[] data = new byte[77];

                Core.Utils.SerializeEndpoint(Endpoint, data, 0);
                Core.Utils.SerializeEndpoint(Source, data, 18);
                Coin.Serialization.CopyData(data, 36, LastSeen);
                Coin.Serialization.CopyData(data, 44, FirstSeen);
                Coin.Serialization.CopyData(data, 52, InTried);
                Coin.Serialization.CopyData(data, 53, NumFailedConnectionAttempts);
                Coin.Serialization.CopyData(data, 57, LastSuccess);
                Coin.Serialization.CopyData(data, 65, LastAttempt);
                Coin.Serialization.CopyData(data, 73, RefCount);
                return data;
            }

            public Peer() { }

            public Peer(byte[] data)
            {
                Deserialize(data);
            }

            public void Deserialize(byte[] data)
            {
                Endpoint = Core.Utils.DeserializeEndpoint(data, 0);
                Source = Core.Utils.DeserializeEndpoint(data, 18);
                LastSeen = Coin.Serialization.GetInt64(data, 36);
                FirstSeen = Coin.Serialization.GetInt64(data, 44);
                InTried = Coin.Serialization.GetBool(data, 52);
                NumFailedConnectionAttempts = Coin.Serialization.GetInt32(data, 53);
                LastSuccess = Coin.Serialization.GetInt64(data, 57);
                LastAttempt = Coin.Serialization.GetInt64(data, 65);
                RefCount = Coin.Serialization.GetInt32(data, 73);
            }

            public Peer(IPEndPoint endpoint, IPEndPoint source, long lastSeen, long firstSeen)
            {
                Endpoint = endpoint;
                Source = source;
                LastSeen = lastSeen;
                FirstSeen = firstSeen;
                InTried = false;
                LastSuccess = 0;
                NumFailedConnectionAttempts = 0;
                LastAttempt = 0;
                RefCount = 0;
            }

            public Peer(IPEndPoint endpoint, IPEndPoint source, long lastSeen)
            {
                Endpoint = endpoint;
                Source = source;
                LastSeen = lastSeen;
                FirstSeen = DateTime.UtcNow.Ticks;
                InTried = false;
                LastSuccess = 0;
                NumFailedConnectionAttempts = 0;
                LastAttempt = 0;
                RefCount = 0;
            }

            public Peer(IPEndPoint endpoint, IPEndPoint source)
            {
                Endpoint = endpoint;
                Source = source;
                LastSeen = DateTime.UtcNow.Ticks - (24L * 3600L * 10_000_000L);
                FirstSeen = DateTime.UtcNow.Ticks;
                InTried = false;
                LastSuccess = 0;
                NumFailedConnectionAttempts = 0;
                LastAttempt = 0;
                RefCount = 0;
            }

            public bool IsTerrible()
            {
                // likely a private peer; should evict if failed to connect.
                if (LastAttempt > 0 && NumFailedConnectionAttempts > 0 && Endpoint.Port > 49151)
                {
                    return true;
                }

                // don't remove things attempted in the last minute
                if (LastAttempt > 0 && LastAttempt >= DateTime.UtcNow.Ticks - 60L * 10_000_000L)
                {
                    return false;
                }

                // has timestamp from too far in the future
                if (LastSeen > DateTime.UtcNow.Ticks + 600L * 10_000_000L)
                {
                    return true;
                }

                // not seen in recent history, or never seen
                if (LastSeen == 0 || DateTime.UtcNow.Ticks - LastSeen > Constants.PEERLIST_HORIZON_DAYS * 24L * 3600L * 10_000_000L)
                {
                    return true;
                }

                // no successes after maximum retries
                if (LastSuccess == 0 && NumFailedConnectionAttempts >= Constants.PEERLIST_MAX_RETRIES)
                {
                    return true;
                }

                // too many falures over the last week
                if (DateTime.UtcNow.Ticks - LastSuccess > Constants.PEERLIST_MIN_FAIL_DAYS * 24L * 3600L * 10_000_000L && NumFailedConnectionAttempts >= Constants.PEERLIST_MAX_FAILURES)
                {
                    return true;
                }

                return false;
            }

            public double GetChance()
            {
                double chance = 1.0;

                long lastTry = Math.Min(DateTime.UtcNow.Ticks - LastAttempt, 0);

                if (lastTry < Constants.PEERLIST_RECENT_TRY * 10_000_000L)
                {
                    chance *= 0.01;
                }

                chance *= Math.Pow(0.66, Math.Min(NumFailedConnectionAttempts, 8));

                return chance;
            }
        }

        private int[,] Tried;
        private int[,] New;

        private List<Peer> Anchors;

        private ConcurrentDictionary<int, Peer> addrs = new();

        private byte[] salt;

        private int _counter = 1;

        private int _newCounter = 0;
        private int _triedCounter = 0;

        public int NumNew { get { return _newCounter; } }

        public int NumTried { get { return _triedCounter; } }

        private List<Peer> TriedCollisions = new();

        public Peerlist()
        {
            Tried = new int[Constants.PEERLIST_MAX_TRIED_BUCKETS, Constants.PEERLIST_BUCKET_SIZE];
            New = new int[Constants.PEERLIST_MAX_NEW_BUCKETS, Constants.PEERLIST_BUCKET_SIZE];

            DB.DisDB.GetDB().GetTried(this);
            DB.DisDB.GetDB().GetNew(this);

            Anchors = DB.DisDB.GetDB().GetAnchors();

            salt = DB.DisDB.GetDB().GetPeerlistSalt();
        }

        public byte[] GetGroup(IPEndPoint endpoint)
        {
            if (endpoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                var _endpoint = Core.Utils.SerializeEndpoint(endpoint);

                // hurricane electric ASN subnet (see bitcoind/src/netaddress.h)
                if (_endpoint[0] == 0x20 && _endpoint[1] == 0x01 && _endpoint[2] == 0x04 && _endpoint[3] == 0x70)
                {
                    // /36
                    var rv = _endpoint[11..16];
                    rv[0] %= 16;
                    return rv;
                }
                else
                {
                    // /32
                    return _endpoint[12..16];
                }
            }
            else if (endpoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                // /16
                return Core.Utils.SerializeEndpoint(endpoint)[12..14];
            }
            else
            {
                throw new Exception($"Peerlist.GetGroup: Unsupported network address for endpoint {endpoint}");
            }
        }

        public int GetTriedBucket(IPEndPoint endpoint)
        {
            byte[] data1 = new byte[salt.Length + 18];
            Array.Copy(salt, 0, data1, 0, salt.Length);
            Core.Utils.SerializeEndpoint(endpoint, data1, (uint)salt.Length);
            var i = Coin.Serialization.GetUInt64(SHA256.HashData(SHA256.HashData(data1)), 0) % Constants.TRIED_BUCKETS_PER_GROUP;

            var group = GetGroup(endpoint);
            byte[] data2 = new byte[salt.Length + group.Length + 8];
            Array.Copy(salt, 0, data2, 0, salt.Length);
            Array.Copy(group, 0, data2, salt.Length, group.Length);
            Coin.Serialization.CopyData(data2, (uint)(salt.Length + group.Length), i);

            return Coin.Serialization.GetInt32(SHA256.HashData(SHA256.HashData(data2)), 0) % Constants.PEERLIST_MAX_TRIED_BUCKETS;
        }

        public int GetNewBucket(IPEndPoint endpoint, IPEndPoint src)
        {
            var srcGroup = GetGroup(src);
            var group = GetGroup(endpoint);

            byte[] data1 = new byte[salt.Length + srcGroup.Length + group.Length];
            Array.Copy(salt, 0, data1, 0, salt.Length);
            Array.Copy(srcGroup, 0, data1, salt.Length, srcGroup.Length);
            Array.Copy(group, 0, data1, salt.Length + srcGroup.Length, group.Length);

            var i = Coin.Serialization.GetUInt64(SHA256.HashData(SHA256.HashData(data1)), 0) % Constants.NEW_BUCKETS_PER_SOURCE_GROUP;

            byte[] data2 = new byte[salt.Length + srcGroup.Length + 8];
            Array.Copy(salt, 0, data2, 0, salt.Length);
            Array.Copy(srcGroup, 0, data2, salt.Length, srcGroup.Length);
            Coin.Serialization.CopyData(data2, (uint)(salt.Length + srcGroup.Length), i);

            return Coin.Serialization.GetInt32(SHA256.HashData(SHA256.HashData(data2)), 0) % Constants.PEERLIST_MAX_NEW_BUCKETS;
        }

        public int GetBucketPosition(bool _new, int bucket, IPEndPoint endpoint)
        {
            byte[] data = new byte[salt.Length + 1 + 4 + 18];
            Array.Copy(salt, 0, data, 0, salt.Length);
            data[salt.Length] = _new ? (byte)'N' : (byte)'K';
            Coin.Serialization.CopyData(data, (uint)(salt.Length + 1), bucket);
            Core.Utils.SerializeEndpoint(endpoint, data, (uint)(salt.Length + 1 + 4));

            return Coin.Serialization.GetInt32(SHA256.HashData(SHA256.HashData(data)), 0) % Constants.PEERLIST_BUCKET_SIZE;
        }

        public void RemoveInNew(int nID)
        {
            for (int i = 0; i < New.GetLength(0); i++)
            {
                for (int j = 0; New.GetLength(1) < nID; j++)
                {
                    if (New[i, j] == nID)
                    {
                        New[i, j] = 0;
                        break;
                    }
                }
            }
        }

        public void AddTried(Peer peer, int nID)
        {
            int startBucket = GetNewBucket(peer.Endpoint, peer.Source);
            for (int i = 0; i < New.GetLength(0); i++)
            {
                int nbucket = (startBucket + i) % New.GetLength(0);
                int npos = GetBucketPosition(true, nbucket, peer.Endpoint);

                if (New[nbucket, npos] == nID)
                {
                    New[nbucket, npos] = 0;
                    peer.RefCount--;
                    if (peer.RefCount == 0) break;
                }
            }
            _newCounter--;

            int bucket = GetTriedBucket(peer.Endpoint);
            int pos = GetBucketPosition(false, bucket, peer.Endpoint);

            if (Tried[bucket, pos] != 0)
            {
                int nIDEvict = Tried[bucket, pos];
                var pOld = addrs[nIDEvict];

                pOld.InTried = false;
                Tried[bucket, pos] = 0;
                _triedCounter--;

                int ubucket = GetNewBucket(pOld.Endpoint, pOld.Source);
                int upos = GetBucketPosition(true, ubucket, peer.Endpoint);
                ClearNew(ubucket, upos);

                pOld.RefCount = 1;
                New[ubucket, upos] = nIDEvict;
                _newCounter++;
            }

            Tried[bucket, pos] = nID;
            _triedCounter++;
            peer.InTried = true;
        }

        public Peer Create(IPEndPoint endpoint, IPEndPoint source, out int nID)
        {
            nID = _counter++;
            addrs[nID] = new Peer(endpoint, source);
            return addrs[nID];
        }

        public bool AddNew(IPEndPoint endpoint, IPEndPoint source, long penalty)
        {
            if (endpoint.Equals(source))
            {
                penalty = 0;
            }

            int nID;
            var pinfo = FindPeer(endpoint, out nID);

            if (pinfo != null)
            {
                pinfo.LastSeen = DateTime.UtcNow.Ticks - penalty;

                // make it harder to randomly add duplicate
                int factor = 1;
                for (int i = 0; i < pinfo.RefCount; i++)
                {
                    factor *= 2;
                }
                if (factor > 1 && new Random().Next(factor) != 0)
                {
                    return false;
                }
            }
            else
            {
                pinfo = Create(endpoint, source, out nID);
                pinfo.LastSeen = DateTime.UtcNow.Ticks - penalty;
                _newCounter++;
            }

            int bucket = GetNewBucket(endpoint, source);
            int pos = GetBucketPosition(true, bucket, endpoint);

            bool _insert = New[bucket,pos] == 0;

            if (New[bucket,pos] != nID)
            {
                if (!_insert)
                {
                    var existingPeer = addrs[New[bucket,pos]];
                    if (existingPeer.IsTerrible() && existingPeer.InTried)
                    {
                        _insert = true;
                    }
                }

                if (_insert)
                {
                    ClearNew(bucket, pos);
                    pinfo.RefCount++;
                    New[bucket,pos] = nID;
                }
                else
                {
                    if (pinfo.RefCount == 0)
                    {
                        Delete(nID);
                    }
                }
            }

            return _insert;
        }

        public void Delete(int nID)
        {
            var pinfo = addrs[nID];
            if (pinfo == null) return;

            if (pinfo.InTried) return;
            if (pinfo.RefCount > 0) return;

            addrs.Remove(nID, out _);
            _newCounter--;
        }

        public void ClearNew(int bucket, int pos)
        {
            if (New[bucket,pos] != 0)
            {
                int nIDClear = New[bucket,pos];
                var pinfoClear = addrs[nIDClear];
                pinfoClear.RefCount = Math.Min(pinfoClear.RefCount - 1, 0);
                New[bucket, pos] = 0;
                if (pinfoClear.RefCount == 0)
                {
                    Delete(nIDClear);
                }
            }
        }

        public Peer FindPeer(IPEndPoint p, out int nID)
        {
            foreach (var _peer in addrs)
            {
                if (p.Equals(_peer.Value.Endpoint))
                {
                    nID = _peer.Key;
                    return _peer.Value;
                }
            }

            nID = -1;
            return null;
        }

        public bool Good(IPEndPoint endpoint, bool testBeforeEvict)
        {
            int nID;

            Peer p = FindPeer(endpoint, out nID);

            if (p == null) return false;

            p.LastSuccess = DateTime.UtcNow.Ticks;
            p.LastAttempt = DateTime.UtcNow.Ticks;
            p.NumFailedConnectionAttempts = 0;

            if (p.InTried) return false;

            if (p.RefCount > 0) return false;

            int bucket = GetTriedBucket(p.Endpoint);
            int pos = GetBucketPosition(false, bucket, p.Endpoint);

            if (testBeforeEvict && Tried[bucket, pos] != 0)
            {
                if (TriedCollisions.Count < Constants.PEERLIST_MAX_TRIED_COLLISION_SIZE)
                {
                    TriedCollisions.Add(addrs[nID]);
                }

                return false;
            }
            else
            {
                AddTried(p, nID);
                return true;
            }
        }

        public bool Add(IEnumerable<IPEndPoint> endpoints, IPEndPoint source, long penalty)
        {
            int added = 0;

            foreach (IPEndPoint endpoint in endpoints)
            {
                added += AddNew(endpoint, source, penalty) ? 1 : 0;
            }

            if (added > 0)
            {
                Daemon.Logger.Debug($"Peerlist.Add: added {added} peers.");
            }

            return added > 0;
        }

        public void Attempt(IPEndPoint endpoint, bool countFailure)
        {
            var peer = FindPeer(endpoint, out _);

            if (peer == null) return;

            peer.LastAttempt = DateTime.UtcNow.Ticks;

            if (countFailure)
            {
                peer.NumFailedConnectionAttempts++;
            }
        }

        public (Peer, long) Select(bool newOnly)
        {
            if (newOnly && NumNew == 0) return (null, 0);

            Random r = new Random();

            if (!newOnly && (NumTried > 0 && (NumNew == 0 || r.Next(0, 2) == 0)))
            {
                double chanceFactor = 1.0;

                while (true)
                {
                    int bucket = r.Next(0, Constants.PEERLIST_MAX_TRIED_BUCKETS);
                    int pos = r.Next(0, Constants.PEERLIST_BUCKET_SIZE);

                    int i;

                    for (i = 0; i < Constants.PEERLIST_BUCKET_SIZE; i++)
                    {
                        if (Tried[bucket, (pos + i) % Constants.PEERLIST_BUCKET_SIZE] != 0) break;
                    }

                    if (i == Constants.PEERLIST_BUCKET_SIZE) continue;

                    int nID = Tried[bucket, (pos + i) % Constants.PEERLIST_BUCKET_SIZE];
                    var found = addrs[nID];

                    if (found == null) return (null, 0);

                    if (r.NextDouble() < chanceFactor * found.GetChance())
                    {
                        return (found, found.LastAttempt);
                    }

                    chanceFactor *= 1.2;
                }
            }
            else
            {
                double chanceFactor = 1.0;

                while (true)
                {
                    int bucket = r.Next(0, Constants.PEERLIST_MAX_NEW_BUCKETS);
                    int pos = r.Next(0, Constants.PEERLIST_BUCKET_SIZE);

                    int i;
                    for (i = 0; i < Constants.PEERLIST_BUCKET_SIZE; i++)
                    {
                        if (New[bucket, (pos + i) % Constants.PEERLIST_BUCKET_SIZE] != 0) break;
                    }

                    if (i == Constants.PEERLIST_BUCKET_SIZE) continue;

                    int nID = New[bucket, (pos + i) % Constants.PEERLIST_BUCKET_SIZE];
                    var found = addrs[nID];

                    if (found == null) return (null, 0);

                    if (r.NextDouble() < chanceFactor * found.GetChance())
                    {
                        return (found, found.LastAttempt);
                    }

                    chanceFactor *= 1.2;
                }
            }
        }

        public void AddPeer(IPEndPoint endpoint = null, long lastSeen = 0)
        {
            // WIP
            if (endpoint == null) return;

            /* sets last seen to 24 hours ago, if not set */
            if (lastSeen <= 0) lastSeen = DateTime.UtcNow.Ticks - (86400L * 10000L * 1000L);

            //var peer = new Peer(endpoint, lastSeen);

            //_peers.Add(peer);
            //DB.DisDB.GetDB().AddPeer(peer);
        }
    }
}
