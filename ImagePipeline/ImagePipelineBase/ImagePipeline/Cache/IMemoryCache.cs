using FBCore.Common.References;
using System;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// Interface for the image pipeline memory cache.
    ///
    /// K is the key type.
    /// V is the value type.
    /// </summary>
    public interface IMemoryCache<K, V>
    {
        /// <summary>
        /// Caches the the given key-value pair.
        ///
        /// <para />The cache returns a new copy of the provided reference which
        /// should be used instead of the original one. The client should close
        /// the returned reference when it is not required anymore.
        ///
        /// <para />If the cache failed to cache the given value, then the null
        /// reference is returned.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A new reference to be used, or null if the caching failed.
        /// </returns>
        CloseableReference<V> Cache(K key, CloseableReference<V> value);

        /// <summary>
        /// Gets the item with the given key, or null if there is no such item.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        /// A reference to the cached value, or null if the item was not found.
        /// </returns>
        CloseableReference<V> Get(K key);

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
        int RemoveAll(Predicate<K> predicate);

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
        bool Contains(Predicate<K> predicate);
    }
}
