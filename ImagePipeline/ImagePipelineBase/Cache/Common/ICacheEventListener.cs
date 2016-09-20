namespace Cache.Common
{
    /// <summary>
    /// Eviction reason enum
    /// </summary>
    public enum EvictionReason
    {
        /// <summary>
        /// Cache full
        /// </summary>
        CACHE_FULL,

        /// <summary>
        /// Content stale
        /// </summary>
        CONTENT_STALE,

        /// <summary>
        /// User forced
        /// </summary>
        USER_FORCED,

        /// <summary>
        /// Cache manager trimmed
        /// </summary>
        CACHE_MANAGER_TRIMMED
    }

    /// <summary>
    /// An interface for logging various cache events.
    ///
    /// <para /> In all callback methods, the <see cref="ICacheEvent"/> 
    /// object should not be held beyond the method itself as they may be automatically recycled.
    /// </summary>
    public interface ICacheEventListener
    {
        /// <summary>
        /// Triggered by a cache hit.
        /// </summary>
        void OnHit(ICacheEvent cacheEvent);

        /// <summary>
        /// Triggered by a cache miss for the given key.
        /// </summary>
        void OnMiss(ICacheEvent cacheEvent);

        /// <summary>
        /// Triggered at the start of the process to save a resource in cache.
        /// </summary>
        void OnWriteAttempt(ICacheEvent cacheEvent);

        /// <summary>
        /// Triggered after a resource has been successfully written to cache.
        /// </summary>
        void OnWriteSuccess(ICacheEvent cacheEvent);

        /// <summary>
        /// Triggered if a cache hit was attempted but an exception was thrown trying to read the resource
        /// from storage.
        /// </summary>
        void OnReadException(ICacheEvent cacheEvent);

        /// <summary>
        /// Triggered if a cache write was attempted but an exception was thrown trying to write the
        /// exception to storage.
        /// </summary>
        void OnWriteException(ICacheEvent cacheEvent);

        /// <summary>
        /// Triggered by an eviction from cache.
        /// </summary>
        void OnEviction(ICacheEvent cacheEvent);

        /// <summary>
        /// Triggered by a full cache clearance.
        /// </summary>
        void OnCleared();
    }
}
