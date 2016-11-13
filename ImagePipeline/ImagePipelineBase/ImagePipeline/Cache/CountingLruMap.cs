using System;
using System.Collections.Generic;
using Truncon.Collections;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// Map that keeps track of the elements order (according to the LRU policy) and their size.
    /// </summary>
    public class CountingLruMap<K, V> where V : class
    {
        private readonly object _mapGate = new object();

        private readonly IValueDescriptor<V> _valueDescriptor;
        private readonly OrderedDictionary<K, V> _map = new OrderedDictionary<K, V>();
        private int _sizeInBytes = 0;

        /// <summary>
        /// Instantiates the <see cref="CountingLruMap{K, V}"/>.
        /// </summary>
        /// <param name="valueDescriptor"></param>
        public CountingLruMap(IValueDescriptor<V> valueDescriptor)
        {
            _valueDescriptor = valueDescriptor;
        }

        internal IList<K> Keys
        {
            get
            {
                lock (_mapGate)
                {
                    return new List<K>(_map.Keys);
                }
            }
        }

        internal IList<V> Values
        {
            get
            {
                lock (_mapGate)
                {
                    return new List<V>(_map.Values);
                }
            }
        }

        /// <summary>
        /// Gets the count of the elements in the map.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_mapGate)
                {
                    return _map.Count;
                }
            }
        }

        /// <summary>
        /// Gets the total size in bytes of the elements in the map.
        /// </summary>
        public int SizeInBytes
        {
            get
            {
                lock (_mapGate)
                {
                    return _sizeInBytes;
                }
            }
        }

        /// <summary>
        /// Gets the key of the first element in the map.
        /// </summary>
        public K FirstKey
        {
            get
            {
                lock (_mapGate)
                {
                    IEnumerator<K> enumerator = _map.Keys.GetEnumerator();
                    enumerator.MoveNext();
                    return (_map.Count == 0) ? default(K) : enumerator.Current;
                }
            }
        }

        /// <summary>
        /// Gets the all matching elements.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IList<KeyValuePair<K, V>> GetMatchingEntries(Predicate<K> predicate)
        {
            lock (_mapGate)
            {
                IList<KeyValuePair<K, V>> matchingEntries = new List<KeyValuePair<K, V>>(_map.Count);
                foreach (var entry in _map)
                {
                    if (predicate == null || predicate(entry.Key))
                    {
                        matchingEntries.Add(entry);
                    }
                }

                return matchingEntries;
            }
        }

        /// <summary>
        /// Returns whether the map contains an element with the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Contains(K key)
        {
            lock (_mapGate)
            {
                return _map.ContainsKey(key);
            }
        }

        /// <summary>
        /// Gets the element from the map.
        /// </summary>
        public V Get(K key)
        {
            lock (_mapGate)
            {
                V value = default(V);
                if (_map.TryGetValue(key, out value))
                {
                    return value;
                }

                return null;
            }
        }

        /// <summary>
        /// Adds the element to the map, and removes the old element with the same key if any.
        /// </summary>
        public V Put(K key, V value)
        {
            lock (_mapGate)
            {
                // We do remove and insert instead of just replace, in order to cause a structural change
                // to the map, as we always want the latest inserted element to be last in the queue.
                V oldValue = default(V);
                if (_map.TryGetValue(key, out oldValue))
                {
                    _map.Remove(key);
                }

                _sizeInBytes -= GetValueSizeInBytes(oldValue);
                _map.Add(key, value);
                _sizeInBytes += GetValueSizeInBytes(value);
                return oldValue;
            }
        }

        /// <summary>
        /// Removes the element from the map.
        /// </summary>
        public V Remove(K key)
        {
            lock (_mapGate)
            {
                V oldValue = default(V);
                if (_map.TryGetValue(key, out oldValue))
                {
                    _map.Remove(key);
                }

                _sizeInBytes -= GetValueSizeInBytes(oldValue);
                return oldValue;
            }
        }

        /// <summary>
        /// Removes all the matching elements from the map.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IList<V> RemoveAll(Predicate<K> predicate)
        {
            lock (_mapGate)
            {
                List<K> oldKeys = new List<K>();
                IList<V> oldValues = new List<V>();
                IEnumerator<KeyValuePair<K, V>> iterator = _map.GetEnumerator();
                while (iterator.MoveNext())
                {
                    KeyValuePair<K, V> entry = iterator.Current;
                    if (predicate == null || predicate(entry.Key))
                    {
                        oldKeys.Add(entry.Key);
                        oldValues.Add(entry.Value);
                        _sizeInBytes -= GetValueSizeInBytes(entry.Value);
                    }
                }

                oldKeys.ForEach(k => _map.Remove(k));
                return oldValues;
            }
        }

        /// <summary>
        /// Clears the map.
        /// </summary>
        public IList<V> Clear()
        {
            lock (_mapGate)
            {
                IList<V> oldValues = new List<V>(_map.Values);
                _map.Clear();
                _sizeInBytes = 0;
                return oldValues;
            }
        }

        private int GetValueSizeInBytes(V value)
        {
            return (value == null) ? 0 : _valueDescriptor.GetSizeInBytes(value);
        }
    }
}
