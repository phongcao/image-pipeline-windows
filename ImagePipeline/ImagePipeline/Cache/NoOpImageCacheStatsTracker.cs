namespace ImagePipeline.Cache
{
    /// <summary>
    /// Class that does no stats tracking at all.
    /// </summary>
    public class NoOpImageCacheStatsTracker : IImageCacheStatsTracker
    {
        private static readonly object _instanceGate = new object();
        private static NoOpImageCacheStatsTracker _instance = null;

        private NoOpImageCacheStatsTracker()
        {
        }

        /// <summary>
        /// Singleton.
        /// </summary>
        public static NoOpImageCacheStatsTracker Instance
        {
            get
            {
                lock (_instanceGate)
                {
                    if (_instance == null)
                    {
                        _instance = new NoOpImageCacheStatsTracker();
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnBitmapCachePut()
        {
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnBitmapCacheHit()
        {
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnBitmapCacheMiss()
        {
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnMemoryCachePut()
        {
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnMemoryCacheHit()
        {
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnMemoryCacheMiss()
        {
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnStagingAreaHit()
        {
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnStagingAreaMiss()
        {
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnDiskCacheHit()
        {
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnDiskCacheMiss()
        {
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnDiskCacheGetFail()
        {
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void RegisterBitmapMemoryCache<K, V>(CountingMemoryCache<K, V> bitmapMemoryCache)
        {
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void RegisterEncodedMemoryCache<K, V>(CountingMemoryCache<K, V> encodedMemoryCache)
        {
        }
    }
}
