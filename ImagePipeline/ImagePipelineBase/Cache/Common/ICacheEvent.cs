using System.IO;

namespace Cache.Common
{
    /// <summary>
    /// Interface to provide details of an individual cache event.
    ///
    /// <para />All values may be null depending on the kind of event.
    /// See the docs for each method to see when to expect values to
    /// be available.
    /// </summary>
    public interface ICacheEvent
    {
        /// <summary>
        /// Gets the cache key related to this event.
        ///
        /// <para />This should be present for all events other
        /// than eviction.
        /// </summary>
        ICacheKey CacheKey { get; }

        /// <summary>
        /// Gets the resource ID for the cached item.
        ///
        /// <para />This is present in cache hit, write success, read
        /// and write exceptions and evictions.
        ///
        /// <para />It may also be present in cache miss events if an
        /// ID was found in the cache's index but the resource wasn't
        /// then found in storage.
        /// </summary>
        string ResourceId { get; }

        /// <summary>
        /// Gets the size of the new resource in storage, in bytes.
        ///
        /// <para />This is present in write success and eviction events.
        /// </summary>
        long ItemSize { get; }

        /// <summary>
        /// Gets the total size of the resources currently in storage,
        /// in bytes.
        ///
        /// <para />This is present in write success and eviction events.
        /// </summary>
        long CacheSize { get; }

        /// <summary>
        /// Gets the current size limit for the cache, in bytes.
        ///
        /// <para />This is present in eviction events where the eviction
        /// is due to the need to trim for size.
        /// </summary>
        long CacheLimit { get; }

        /// <summary>
        /// Gets the exception which occurred to trigger a read or write
        /// exception event.
        /// </summary>
        IOException Exception { get; }

        /// <summary>
        /// Gets the reason for an item's eviction in eviction events.
        /// </summary>
        EvictionReason EvictionReason { get; }
    }
}
