using FBCore.Common.References;
using System;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// Instrumented memory cache.
    /// </summary>
    public class InstrumentedMemoryCache<K, V> : IMemoryCache<K, V>
    {
        private readonly IMemoryCache<K, V> _delegateMemoryCache;
        private readonly IMemoryCacheTracker _tracker;

        /// <summary>
        /// Instantiates the <see cref="InstrumentedMemoryCache{K, V}"/>.
        /// </summary>
        public InstrumentedMemoryCache(
            IMemoryCache<K, V> delegateMemoryCache, 
            IMemoryCacheTracker tracker) 
        {
            _delegateMemoryCache = delegateMemoryCache;
            _tracker = tracker;
        }

        /// <summary>
        /// Gets the item with the given key, or null if there is no such item.
        /// </summary>
        /// <returns>
        /// A reference to the cached value, or null if the item was not found.
        /// </returns>
        public CloseableReference<V> Get(K key)
        {
            CloseableReference<V> result = _delegateMemoryCache.Get(key);
            if (result == null)
            {
                _tracker.OnCacheMiss();
            }
            else
            {
                _tracker.OnCacheHit();
            }

            return result;
        }

        /// <summary>
        /// Caches the the given key-value pair.
        ///
        /// <para />The cache returns a new copy of the provided reference
        /// which should be used instead of the original one.
        /// The client should close the returned reference when it is not
        /// required anymore.
        ///
        /// <para />If the cache failed to cache the given value, then the
        /// null reference is returned.
        /// </summary>
        /// <returns>
        /// A new reference to be used, or null if the caching failed.
        /// </returns>
        public CloseableReference<V> Cache(K key, CloseableReference<V> value)
        {
            _tracker.OnCachePut();
            return _delegateMemoryCache.Cache(key, value);
        }

        /// <summary>
        /// Removes all the items from the cache whose keys match the
        /// specified predicate.
        /// </summary>
        /// <param name="predicate">
        /// Returns true if an item with the given key should be removed.
        /// </param>
        /// <returns>
        /// Number of the items removed from the cache.
        /// </returns>
        public int RemoveAll(Predicate<K> predicate)
        {
            return _delegateMemoryCache.RemoveAll(predicate);
        }

        /// <summary>
        /// Find if any of the items from the cache whose keys match the
        /// specified predicate.
        /// </summary>
        /// <param name="predicate">
        /// Returns true if an item with the given key matches.
        /// </param>
        /// <returns>
        /// true if the predicate was found in the cache, false otherwise.
        /// </returns>
        public bool Contains(Predicate<K> predicate)
        {
            return _delegateMemoryCache.Contains(predicate);
        }
    }
}
