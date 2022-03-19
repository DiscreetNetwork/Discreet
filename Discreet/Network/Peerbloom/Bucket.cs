using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discreet.Cipher;
using System.Diagnostics.CodeAnalysis;
using System.Collections;

namespace Discreet.Network.Peerbloom
{
    public class Bucket: IDictionary<SHA256, Peerlist.Peer>
    {
        public int Capacity { get; private set; }
        public int Count { get { return _data.Count; } }

        public ICollection<SHA256> Keys => _data.Keys;

        public ICollection<Peerlist.Peer> Values => _data.Values;

        public bool IsReadOnly => false;

        public Peerlist.Peer this[SHA256 key] { get => _data[key]; set => Add(key, value); }

        private ConcurrentDictionary<SHA256, Peerlist.Peer> _data;

        public Bucket(int capacity)
        {
            Capacity = capacity;
            _data = new ConcurrentDictionary<SHA256, Peerlist.Peer>();
        }

        public void Add(SHA256 key, Peerlist.Peer value)
        {
            if (!_data.ContainsKey(key) && _data.Count == Capacity) return;

            _data.TryAdd(key, value);
        }

        public bool ContainsKey(SHA256 key)
        {
            return _data.ContainsKey(key);
        }

        public bool Remove(SHA256 key)
        {
            return _data.Remove(key, out _);
        }

        public bool TryGetValue(SHA256 key, [MaybeNullWhen(false)] out Peerlist.Peer value)
        {
            return _data.TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<SHA256, Peerlist.Peer> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(KeyValuePair<SHA256, Peerlist.Peer> item)
        {
            return _data.Contains(item);
        }

        public void CopyTo(KeyValuePair<SHA256, Peerlist.Peer>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException("array");

            if (arrayIndex < 0) throw new ArgumentOutOfRangeException("index");

            if (array.Length - arrayIndex < _data.Count) throw new ArgumentException("destination array does not have enough available space", "array");

            foreach (var pair in _data)
            {
                array[arrayIndex++] = pair;
            }
        }

        public bool Remove(KeyValuePair<SHA256, Peerlist.Peer> item)
        {
            return _data.Remove(item.Key, out _);
        }

        public IEnumerator<KeyValuePair<SHA256, Peerlist.Peer>> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }
    }
}
