using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LimeBean {

    class Cache<K, V> where K : IEquatable<K> {
        int _capacity;
        LinkedList<K> _sequence;
        IDictionary<K, LinkedListNode<K>> _index;
        IDictionary<K, V> _values;

        public Cache() {
            _capacity = 50;
            _sequence = new LinkedList<K>();
            _index = new Dictionary<K, LinkedListNode<K>>();
            _values = new Dictionary<K, V>();
        }

        public int Capacity {
            get { return _capacity; }
            set {
                _capacity = value;
                TrimExcess();
            }
        }

        public int Count {
            get { return _values.Count; }
        }

        public bool Contains(K key) {
            return _values.ContainsKey(key);
        }

        public V Get(K key) {
            if(!Contains(key))
                throw new KeyNotFoundException();

            PromoteKey(key);
            return _values[key];
        }

        public void Put(K key, V value) {
            if(!Contains(key)) {
                if(Capacity > 0) {
                    var node = new LinkedListNode<K>(key);
                    _sequence.AddFirst(node);
                    _index[key] = node;
                    _values[key] = value;
                }
                TrimExcess();
            } else {
                PromoteKey(key);
            }
        }

        public void Remove(K key) { 
            if(!Contains(key))
                return;

            _sequence.Remove(_index[key]);
            _index.Remove(key);
            _values.Remove(key);
        }

        public void Clear() {
            _sequence.Clear();
            _index.Clear();
            _values.Clear();
        }

        internal IEnumerable<K> EnumerateKeys() {
            return _sequence;
        }

        void PromoteKey(K key) {
            var node = _index[key];
            if(node != _sequence.First) {
                _sequence.Remove(node);
                _sequence.AddFirst(node);
            }
        }

        void TrimExcess() {
            while(_sequence.Count > Capacity)
                Remove(_sequence.Last.Value);
        }

    }

}
