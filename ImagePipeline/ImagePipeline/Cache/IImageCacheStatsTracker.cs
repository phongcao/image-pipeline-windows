namespace ImagePipeline.Cache
{
    /// <summary>
    /// Interface for stats tracking for the image cache.
    ///
    /// <para />An implementation of this interface, passed to
    /// ImagePipelineConfig will be notified for each of the
    /// following cache events. Use this to keep cache stats for your app.
    /// </summary>
    public interface IImageCacheStatsTracker
    {
        ///  Called whenever decoded images are put into the bitmap cache. 
        void OnBitmapCachePut();

        ///  Called on a bitmap cache hit. 
        void OnBitmapCacheHit();

        ///  Called on a bitmap cache miss. 
        void OnBitmapCacheMiss();

        ///  Called whenever encoded images are put into the encoded memory cache. 
        void OnMemoryCachePut();

        ///  Called on an encoded memory cache hit. 
        void OnMemoryCacheHit();

        ///  Called on an encoded memory cache hit. 
        void OnMemoryCacheMiss();

        /// <summary>
        /// Called on an staging area hit.
        ///
        /// <para />The staging area stores encoded images. It gets the images before they are written
        /// to disk cache.
        /// </summary>
        void OnStagingAreaHit();

        ///  Called on a staging area miss hit. 
        void OnStagingAreaMiss();

        ///  Called on a disk cache hit. 
        void OnDiskCacheHit();

        ///  Called on a disk cache miss. 
        void OnDiskCacheMiss();

        ///  Called if an exception is thrown on a disk cache read. 
        void OnDiskCacheGetFail();

        /// <summary>
        /// Registers a bitmap cache with this tracker.
        ///
        /// <para />Use this method if you need access to the cache itself to compile your stats.
        /// </summary>
        void RegisterBitmapMemoryCache<K, V>(CountingMemoryCache<K, V> bitmapMemoryCache);

        /// <summary>
        /// Registers an encoded memory cache with this tracker.
        ///
        /// <para />Use this method if you need access to the cache itself to compile your stats.
        /// </summary>
        void RegisterEncodedMemoryCache<K, V>(CountingMemoryCache<K, V> encodedMemoryCache);
    }
}
