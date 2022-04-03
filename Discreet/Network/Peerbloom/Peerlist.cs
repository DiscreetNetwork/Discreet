﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom
{
    public class Peerlist
    {
        private uint[,] Tried;
        private uint[,] New;

        private List<Peer> Anchors;

        private ConcurrentDictionary<uint, Peer> addrs = new();

        private byte[] salt;

        private int _counter = 1;

        private int _newCounter = 0;
        private int _triedCounter = 0;

        public int NumNew { get { return _newCounter; } }

        public int NumTried { get { return _triedCounter; } }

        private List<Peer> TriedCollisions = new();

        private SemaphoreSlim doubleMutexGambit = new SemaphoreSlim(1, 1);

        public Peerlist()
        {
            var path = Path.Combine(Daemon.DaemonConfig.GetConfig().DaemonPath, "peerlist.bin");
            if (File.Exists(path))
            {
                try
                {
                    Deserialize(File.ReadAllBytes(path));
                    return;
                }
                catch
                {
                    Daemon.Logger.Error($"Peerlist: could not initialize from {path}; possibly corrupt");
                }
            }

            _counter = 1;
            _newCounter = 0;
            _triedCounter = 0;

            Tried = new uint[Constants.PEERLIST_MAX_TRIED_BUCKETS, Constants.PEERLIST_BUCKET_SIZE];
            New = new uint[Constants.PEERLIST_MAX_NEW_BUCKETS, Constants.PEERLIST_BUCKET_SIZE];

            addrs = new();
            Anchors = new();
            TriedCollisions = new();

            salt = new byte[32];
            new Random().NextBytes(salt);

            File.WriteAllBytes(path, Serialize());
        }

        public byte[] Serialize()
        {
            using MemoryStream _ms = new MemoryStream();

            Coin.Serialization.CopyData(_ms, salt);
            Coin.Serialization.CopyData(_ms, _counter);
            Coin.Serialization.CopyData(_ms, _triedCounter);
            Coin.Serialization.CopyData(_ms, _newCounter);

            for (int i = 0; i < Tried.GetLength(0); i++)
            {
                for (int j = 0; j < Tried.GetLength(1); j++)
                {
                    Coin.Serialization.CopyData(_ms, Tried[i, j]);
                }
            }

            for (int i = 0; i < New.GetLength(0); i++)
            {
                for (int j = 0; j < New.GetLength(1); j++)
                {
                    Coin.Serialization.CopyData(_ms, New[i, j]);
                }
            }

            Coin.Serialization.CopyData(_ms, addrs.Count);

            foreach (var kvpair in addrs)
            {
                Coin.Serialization.CopyData(_ms, kvpair.Key);
                Coin.Serialization.CopyData(_ms, kvpair.Value.Serialize());
            }

            Coin.Serialization.CopyData(_ms, Anchors.Count);

            for (int i = 0; i < Anchors.Count; i++)
            {
                Coin.Serialization.CopyData(_ms, Anchors[i].Serialize());
            }

            Coin.Serialization.CopyData(_ms, TriedCollisions.Count);

            for (int i = 0; i < TriedCollisions.Count; i++)
            {
                Coin.Serialization.CopyData(_ms, TriedCollisions[i].Serialize());
            }

            return _ms.ToArray();
        }

        public void Deserialize(byte[] data)
        {
            using var _ms = new MemoryStream(data);

            salt = Coin.Serialization.GetBytes(_ms);
            _counter = Coin.Serialization.GetInt32(_ms);
            _triedCounter = Coin.Serialization.GetInt32(_ms);
            _newCounter = Coin.Serialization.GetInt32(_ms);

            Tried = new uint[Constants.PEERLIST_MAX_TRIED_BUCKETS, Constants.PEERLIST_BUCKET_SIZE];
            New = new uint[Constants.PEERLIST_MAX_NEW_BUCKETS, Constants.PEERLIST_BUCKET_SIZE];

            for (int i = 0; i < Tried.GetLength(0); i++)
            {
                for (int j = 0; j < Tried.GetLength(1); j++)
                {
                    Tried[i, j] = Coin.Serialization.GetUInt32(_ms);
                }
            }

            for (int i = 0; i < New.GetLength(0); i++)
            {
                for (int j = 0; j < New.GetLength(1); j++)
                {
                    New[i, j] = Coin.Serialization.GetUInt32(_ms);
                }
            }

            addrs = new();
            var addrsCount = Coin.Serialization.GetInt32(_ms);

            for (int i = 0; i < addrsCount; i++)
            {
                var nID = Coin.Serialization.GetUInt32(_ms);
                addrs[nID] = new Peer(Coin.Serialization.GetBytes(_ms));
            }

            Anchors = new();
            var anchorCount = Coin.Serialization.GetInt32(_ms);

            for (int i = 0; i < anchorCount; i++)
            {
                Anchors.Add(new Peer(Coin.Serialization.GetBytes(_ms)));
            }

            TriedCollisions = new();
            var collisionCount = Coin.Serialization.GetInt32(_ms);

            for (int i = 0; i < collisionCount; i++)
            {
                TriedCollisions.Add(new Peer(Coin.Serialization.GetBytes(_ms)));
            }
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

        public uint GetTriedBucket(IPEndPoint endpoint)
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

            return Coin.Serialization.GetUInt32(SHA256.HashData(SHA256.HashData(data2)), 0) % Constants.PEERLIST_MAX_TRIED_BUCKETS;
        }

        public uint GetNewBucket(IPEndPoint endpoint, IPEndPoint src)
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

            return Coin.Serialization.GetUInt32(SHA256.HashData(SHA256.HashData(data2)), 0) % Constants.PEERLIST_MAX_NEW_BUCKETS;
        }

        public uint GetBucketPosition(bool _new, uint bucket, IPEndPoint endpoint)
        {
            byte[] data = new byte[salt.Length + 1 + 4 + 18];
            Array.Copy(salt, 0, data, 0, salt.Length);
            data[salt.Length] = _new ? (byte)'N' : (byte)'K';
            Coin.Serialization.CopyData(data, (uint)(salt.Length + 1), bucket);
            Core.Utils.SerializeEndpoint(endpoint, data, (uint)(salt.Length + 1 + 4));

            return Coin.Serialization.GetUInt32(SHA256.HashData(SHA256.HashData(data)), 0) % Constants.PEERLIST_BUCKET_SIZE;
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

        public void AddTried(Peer peer, uint nID)
        {
            if (addrs[nID] != null)
            {
                uint startBucket = GetNewBucket(peer.Endpoint, peer.Source);
                bool _modified = false;
                for (uint i = 0; i < New.GetLength(0); i++)
                {
                    uint nbucket = (startBucket + i) % (uint)New.GetLength(0);
                    uint npos = GetBucketPosition(true, nbucket, peer.Endpoint);

                    if (New[nbucket, npos] == nID)
                    {
                        New[nbucket, npos] = 0;
                        peer.RefCount--;
                        _modified = true;
                        if (peer.RefCount == 0) break;
                    }
                }
                if (_modified) _newCounter--;
            }

            uint bucket = GetTriedBucket(peer.Endpoint);
            uint pos = GetBucketPosition(false, bucket, peer.Endpoint);

            if (Tried[bucket, pos] != 0)
            {
                uint nIDEvict = Tried[bucket, pos];
                var pOld = addrs[nIDEvict];

                pOld.InTried = false;
                Tried[bucket, pos] = 0;
                _triedCounter--;

                uint ubucket = GetNewBucket(pOld.Endpoint, pOld.Source);
                uint upos = GetBucketPosition(true, ubucket, peer.Endpoint);
                ClearNew(ubucket, upos);

                pOld.RefCount = 1;
                New[ubucket, upos] = nIDEvict;
                _newCounter++;
            }

            Tried[bucket, pos] = nID;
            _triedCounter++;
            peer.InTried = true;
        }

        public Peer Create(IPEndPoint endpoint, IPEndPoint source, out uint nID)
        {
            nID = (uint)_counter++;
            addrs[nID] = new Peer(endpoint, source);
            return addrs[nID];
        }

        public Peer Create(IPEndPoint endpoint, IPEndPoint source, out uint nID, long firstSeen = 0, long lastSeen = 0)
        {
            nID = (uint)_counter++;
            addrs[nID] = new Peer(endpoint, source, lastSeen, firstSeen);
            return addrs[nID];
        }

        public bool AddNew(IPEndPoint endpoint, IPEndPoint source, long penalty)
        {
            if (endpoint.Equals(source))
            {
                penalty = 0;
            }

            uint nID;
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

            uint bucket = GetNewBucket(endpoint, source);
            uint pos = GetBucketPosition(true, bucket, endpoint);

            bool _insert = New[bucket,pos] == 0;

            if (New[bucket,pos] != nID)
            {
                if (!_insert)
                {
                    var existingPeer = addrs[New[bucket,pos]];
                    if (existingPeer.IsTerrible() || (existingPeer.RefCount > 1 && pinfo.RefCount == 0))
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

        public void Delete(uint nID)
        {
            var pinfo = addrs[nID];
            if (pinfo == null) return;

            if (pinfo.InTried) return;
            if (pinfo.RefCount > 0) return;

            addrs.Remove(nID, out _);
            _newCounter--;
        }

        public void ClearNew(uint bucket, uint pos)
        {
            if (New[bucket,pos] != 0)
            {
                uint nIDClear = New[bucket,pos];
                var pinfoClear = addrs[nIDClear];
                pinfoClear.RefCount = Math.Min(pinfoClear.RefCount - 1, 0);
                New[bucket, pos] = 0;
                if (pinfoClear.RefCount <= 0)
                {
                    Delete(nIDClear);
                }
            }
        }

        public void ClearTried(Peer p)
        {
            FindPeer(p.Endpoint, out var nID);
            if (!addrs.TryRemove(nID, out _)) return;

            var bucket = GetTriedBucket(p.Endpoint);
            var pos = GetBucketPosition(false, bucket, p.Endpoint);

            Tried[bucket, pos] = 0;

            p.InTried = false;
            _triedCounter--;
        }

        public Peer FindPeer(IPEndPoint p, out uint nID)
        {
            foreach (var _peer in addrs)
            {
                if (p.Equals(_peer.Value.Endpoint))
                {
                    nID = _peer.Key;
                    return _peer.Value;
                }
            }

            nID = 0;
            return null;
        }

        public bool Good(IPEndPoint endpoint, bool testBeforeEvict)
        {
            uint nID;

            Peer p = FindPeer(endpoint, out nID);

            if (p == null) return false;

            p.LastSuccess = DateTime.UtcNow.Ticks;
            p.LastAttempt = DateTime.UtcNow.Ticks;
            p.NumFailedConnectionAttempts = 0;

            if (p.InTried) return false;

            if (p.RefCount > 0) return false;

            uint bucket = GetTriedBucket(p.Endpoint);
            uint pos = GetBucketPosition(false, bucket, p.Endpoint);

            if (testBeforeEvict && Tried[bucket, pos] != 0)
            {
                if (TriedCollisions.Count < Constants.PEERLIST_MAX_TRIED_COLLISION_SIZE)
                {
                    addrs[nID].LastAttempt = 0;
                    addrs[nID].NumFailedConnectionAttempts = 0;
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

            if (NumTried == 0 && NumNew == 0) return (null, 0);

            Random r = new Random();

            if (!newOnly && (NumTried > 0 && (NumNew == 0 || r.Next(0, 2) == 0)))
            {
                double chanceFactor = 1.0;

                while (true)
                {
                    uint bucket = (uint)r.Next(0, Constants.PEERLIST_MAX_TRIED_BUCKETS);
                    uint pos = (uint)r.Next(0, Constants.PEERLIST_BUCKET_SIZE);

                    uint i;

                    for (i = 0; i < Constants.PEERLIST_BUCKET_SIZE; i++)
                    {
                        if (Tried[bucket, (pos + i) % Constants.PEERLIST_BUCKET_SIZE] != 0) break;
                    }

                    if (i == Constants.PEERLIST_BUCKET_SIZE) continue;

                    uint nID = Tried[bucket, (pos + i) % Constants.PEERLIST_BUCKET_SIZE];
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
                    uint bucket = (uint)r.Next(0, Constants.PEERLIST_MAX_NEW_BUCKETS);
                    uint pos = (uint)r.Next(0, Constants.PEERLIST_BUCKET_SIZE);

                    int i;
                    for (i = 0; i < Constants.PEERLIST_BUCKET_SIZE; i++)
                    {
                        if (New[bucket, (pos + i) % Constants.PEERLIST_BUCKET_SIZE] != 0) break;
                    }

                    if (i == Constants.PEERLIST_BUCKET_SIZE) continue;

                    uint nID = New[bucket, (pos + i) % Constants.PEERLIST_BUCKET_SIZE];
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

        public List<IPEndPoint> GetAddr(int maxAddresses, int maxPercent)
        {
            int numNodes = addrs.Count;

            if (maxPercent > 0)
            {
                numNodes = maxPercent * numNodes / 100;
            }

            if (maxAddresses > 0)
            {
                numNodes = Math.Min(numNodes, maxAddresses);
            }

            Random r = new Random();
            List<IPEndPoint> peers = new List<IPEndPoint>();
            List<uint> nIDs = addrs.Keys.ToList();
            for (int i = 0; i < numNodes; i++)
            {
                addrs.TryGetValue(nIDs[r.Next(0, nIDs.Count)], out var peer);

                if (peer != null)
                {
                    if (peer.IsTerrible())
                    {
                        if (nIDs.Count < maxAddresses) i++;
                        continue;
                    }

                    if (!peers.Contains(peer.Endpoint))
                    {
                        peers.Add(peer.Endpoint);
                    }
                }
            }

            return peers;
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

        /// <summary>
        /// Used to manage collisions, periodically backup the tables, and resolve collisions.
        /// </summary>
        /// <returns></returns>
        public void Start(Feeler feeler, CancellationToken token)
        {
            _ = Task.Run(() => Saver(token)).ConfigureAwait(false);
            _ = Task.Run(() => Collisioner(feeler, token)).ConfigureAwait(false);
        }

        private async Task Saver(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var timer = DateTime.UtcNow.AddSeconds(Constants.PEERLIST_SAVE_INTERVAL).Ticks;

                while (timer > DateTime.UtcNow.Ticks && !token.IsCancellationRequested)
                {
                    // we can await much longer for this
                    await Task.Delay(5000);
                }

                if (token.IsCancellationRequested) return;

                await File.WriteAllBytesAsync(Path.Combine(Daemon.DaemonConfig.GetConfig().DaemonPath, "peerlist.bin"), Serialize(), token);

                Daemon.Logger.Debug($"Peerlist.Saver: saved peerlist to disk");
            }
        }

        private async Task Collisioner(Feeler feeler, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var timer = DateTime.UtcNow.AddSeconds(Constants.PEERLIST_RESOLVE_COLLISION_INTERVAL).Ticks;

                while (timer > DateTime.UtcNow.Ticks && !token.IsCancellationRequested)
                {
                    await Task.Delay(5000);
                }

                if (token.IsCancellationRequested) return;

                // first check if any collisions have been resolved yet
                List<Peer> toRemove = new();

                foreach (var p in TriedCollisions)
                {
                    uint bucket = GetTriedBucket(p.Endpoint);
                    uint pos = GetBucketPosition(false, bucket, p.Endpoint);

                    uint nID = Tried[bucket, pos];
                    if (nID == 0) continue;

                    var oldPeer = addrs[nID];
                    if (oldPeer.LastAttempt > 0 && oldPeer.NumFailedConnectionAttempts > 0)
                    {
                        Good(p.Endpoint, false);
                    }
                    else
                    {
                        toRemove.Add(p);
                    }
                }

                foreach (var peer in toRemove)
                {
                    TriedCollisions.Remove(peer);
                }

                // now pass unchecked collisions into Feeler if not already there
                foreach (var peer in TriedCollisions)
                {
                    feeler.Enqueue(Feeler.FeelerPriorityLevel.COLLISION, peer);
                }
            }
        }
    }
}