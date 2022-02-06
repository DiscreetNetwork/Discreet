using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Discreet.Network.Peerbloom.Extensions;

namespace Discreet.Network.Peerbloom
{
    public class BucketManager
    {
        private List<Bucket> _buckets;
        private LocalNode _localNode;

        public BucketManager(LocalNode localNode)
        {
            _buckets = new List<Bucket>();
            _localNode = localNode;
            _buckets.Add(new Bucket());
        }

        public void AddRemoteNode(RemoteNode remoteNode)
        {
            remoteNode.SetLastSeen();

            Bucket b = _buckets[_buckets.FindIndex(x => x.IsNodeInRange(remoteNode.Id))];

            if(b.ContainsNode(remoteNode))
            {
                b.Replace(remoteNode);
                return;
            }

            if(!b.IsFull)
            {
                b.AddNode(remoteNode);
                Console.WriteLine($"Added peer: {remoteNode.Endpoint.Address}:{remoteNode.Endpoint.Port}");
                return;
            }

            if(b.IsNodeInRange(_localNode.Id) || (b.Depth() % Constants.ALPHA) != 0)
            {
                (Bucket b1, Bucket b2) = b.Split();
                Visor.Logger.Log($"Performed bucket split: {_buckets.Count} total buckets");
                b1.SetLastUpdated();
                b2.SetLastUpdated();

                int index = _buckets.FindIndex(x => x.IsNodeInRange(remoteNode.Id));
                _buckets[index] = b1;
                _buckets.Insert(index + 1, b2);
                AddRemoteNode(remoteNode);
                return;
            }

            /// TODO:
            ///  - If none of the above checks
            ///  - Ping the last seen node in the bucket 'b'
            ///  - If the node does not respond, replace it with the new node
            ///  - Otherwise, reject the new node
            
            /// TODO 2:
            ///  - Add a pending nodes system
            ///  - If the last seen nodes does respond, add the new node to the pending queue 
        }

        public async Task RefreshBucket(Bucket b)
        {
            Visor.Logger.Log("Refreshing a bucket\n");
            foreach (var node in b.GetNodes())
            {
                var fetchedNodes = await node.FindNode(_localNode.Id, _localNode.Endpoint, new NodeId(Utility.GetRandomPositiveBigInteger(b.Low, b.High).ToKey()));
                if(fetchedNodes == null)
                {
                    // Handle eviction
                    continue;
                }

                fetchedNodes.ForEach(x => AddRemoteNode(x));
            }

            b.SetLastUpdated();
        }

        public List<Bucket> GetBuckets() => _buckets.ToList();

        /// <summary>
        /// Finds and returns the bucket, in which the given Id belongs to
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Bucket GetBucket(NodeId id)
        {
            int index = _buckets.FindIndex(x => x.IsNodeInRange(id));
            return _buckets[index];
        }

        /// <summary>
        /// Returns up to 'BUCKET_LENGTH' nodes, sorted by distance to the 'target' nodeId
        /// </summary>
        /// <param name="target"></param>
        /// <param name="toExclude"></param>
        /// <returns></returns>
        public List<RemoteNode> GetClosestNodes(NodeId target, NodeId toExclude)
        {
            var closestNodes = _buckets.
                SelectMany(x => x.GetNodes()).
                Where(x => x.Id.Value != toExclude.Value).
                Select(x => new { node = x, distance = x.Id.Value ^ target.Value }).
                OrderBy(d => d.distance).
                Take(Constants.BUCKET_LENGTH);

            return closestNodes.Select(x => x.node).ToList();
        }
    }
}
