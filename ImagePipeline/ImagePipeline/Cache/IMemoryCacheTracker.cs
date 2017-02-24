namespace ImagePipeline.Cache
{
    /// <summary>
    /// Memory cache tracker.
    /// </summary>
    public interface IMemoryCacheTracker
    {
        /// <summary>
        /// On cache hit.
        /// </summary>
        void OnCacheHit();

        /// <summary>
        /// On cache miss.
        /// </summary>
        void OnCacheMiss();

        /// <summary>
        /// On cache put.
        /// </summary>
        void OnCachePut();
    }
}
