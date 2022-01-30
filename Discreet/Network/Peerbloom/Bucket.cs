using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Network.Peerbloom
{
    public class Bucket
    {
        /// <summary>
        /// Holds information about all the known nodes within the bucket
        /// </summary>
        private List<RemoteNode> _nodes;

        /// <summary>
        /// The Low value of the ID space / region, that this bucket holds
        /// </summary>
        public BigInteger Low { get; private set; }

        /// <summary>
        /// The High value of the ID space / region, that this bucket holds
        /// </summary>
        public BigInteger High { get; private set; }

        /// <summary>
        /// A datetime that shows when the bucket were last refreshed / updated
        /// </summary>
        public DateTime LastUpdated { get; private set; }

        public bool IsFull { get => _nodes.Count == Constants.BUCKET_LENGTH; }

        /// <summary>
        /// The default constructor which is used to instantiate the very first bucket in the node
        /// </summary>
        public Bucket()
        {
            _nodes = new List<RemoteNode>();
            Low = 0;
            High = BigInteger.Pow(new BigInteger(2), Constants.ID_BIT_LENGTH);
            LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// The constructor used when splitting the bucket into two seperate buckets
        /// </summary>
        /// <param name="low">Low value of the ID space that the new bucket supports</param>
        /// <param name="high">High value of the ID space that the new bucket supports</param>
        public Bucket(BigInteger low, BigInteger high)
        {
            _nodes = new List<RemoteNode>();
            Low = low;
            High = high;
            LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Used for setting the property 'LastUpdated' to DateTime.UtcNow
        /// </summary>
        public void SetLastUpdated() => LastUpdated = DateTime.UtcNow;

        /// <summary>
        /// Returns a new instance of the list of nodes. This prevents accidently altering the original list
        /// </summary>
        /// <returns></returns>
        public List<RemoteNode> GetNodes() => _nodes.ToList();

        public void AddNode(RemoteNode toAdd)
        {
            if (IsFull) return;

            _nodes.Add(toAdd);
        }

        public void Replace(RemoteNode toReplace)
        {
            _nodes.Find(x => x.Id.Value == toReplace.Id.Value).SetLastSeen();
        }

        public void Evict(NodeId node)
        {
            _nodes.RemoveAt(_nodes.FindIndex(x => x.Id.Value == node.Value));
        }

        /// <summary>
        /// Splits the bucket into two, and divides the current Id space / range amongst the two new buckets
        /// </summary>
        /// <returns>Two buckets in which the nodes have been placed accordingly based on their Id values</returns>
        public (Bucket, Bucket) Split()
        {
            BigInteger mid = (Low + High) / 2;
            Bucket b1 = new Bucket(Low, mid);
            Bucket b2 = new Bucket(mid, High);

            foreach (var node in _nodes)
            {
                Bucket b = node.Id.Value < mid ? b1 : b2;
                b.AddNode(node);
            }

            return (b1, b2);
        }

        /// <summary>
        /// Helper method to determine if a given NodeId is within the low and high range of the buckets ID space / range
        /// </summary>
        /// <param name="id">The NodeId of the node to check against</param>
        /// <returns></returns>
        public bool IsNodeInRange(NodeId id) => id.Value >= Low && id.Value < High;

        /// <summary>
        /// Helper method to check if the bucket contains the given node
        /// </summary>
        /// <param name="node">The node to check against</param>
        /// <returns></returns>
        public bool ContainsNode(RemoteNode node) => _nodes.Any(x => x.Id.Value == node.Id.Value);


        public int Depth()
        {
            if (_nodes.Count < 2) return 0; // We cant find shared bits, if there's less than two nodes in the bucket

            BitArray bitArray = new BitArray(_nodes.First().Id.GetBytes());

            for (int i = 1; i < _nodes.Count; i++)
            {
                bitArray = Utility.GetSharedBits(bitArray, _nodes[i].Id.GetBytes());
            }

            return bitArray.Count;
        }
    }
}
