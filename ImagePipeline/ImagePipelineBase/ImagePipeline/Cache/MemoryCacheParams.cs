namespace ImagePipeline.Cache
{
    /// <summary>
    /// Configuration for a memory cache.
    /// </summary>
    public class MemoryCacheParams
    {
        /// <summary>
        /// Gets the maximum size of the cache, in bytes.
        /// </summary>
        public int MaxCacheSize { get; }

        /// <summary>
        /// Gets the maximum number of items that can live in the cache.
        /// </summary>
        public int MaxCacheEntries { get; }

        /// <summary>
        /// Gets the eviction queue is an area of memory that stores items ready
        /// for eviction but have not yet been deleted. This is the maximum
        /// size of that queue in bytes.
        /// </summary>
        public int MaxEvictionQueueSize { get; }

        /// <summary>
        /// Gets the maximum number of entries in the eviction queue.
        /// </summary>
        public int MaxEvictionQueueEntries { get; }

        /// <summary>
        /// Gets the maximum size of a single cache entry.
        /// </summary>
        public int MaxCacheEntrySize { get; }

        /// <summary>
        /// Pass arguments to control the cache's behavior in the constructor.
        /// </summary>
        /// <param name="maxCacheSize">
        /// The maximum size of the cache, in bytes.
        /// </param>
        /// <param name="maxCacheEntries">
        /// The maximum number of items that can live in the cache.
        /// </param>
        /// <param name="maxEvictionQueueSize">
        /// The eviction queue is an area of memory that stores items ready for
        /// eviction but have not yet been deleted. This is the maximum size of
        /// that queue in bytes.
        /// </param>
        /// <param name="maxEvictionQueueEntries">
        /// The maximum number of entries in the eviction queue.
        /// </param>
        /// <param name="maxCacheEntrySize">
        /// The maximum size of a single cache entry.
        /// </param>
        public MemoryCacheParams(
            int maxCacheSize,
            int maxCacheEntries,
            int maxEvictionQueueSize,
            int maxEvictionQueueEntries,
            int maxCacheEntrySize)
        {
            MaxCacheSize = maxCacheSize;
            MaxCacheEntries = maxCacheEntries;
            MaxEvictionQueueSize = maxEvictionQueueSize;
            MaxEvictionQueueEntries = maxEvictionQueueEntries;
            MaxCacheEntrySize = maxCacheEntrySize;
        }
    }
}
