using Cache.Common;
using ImagePipeline.Image;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// Bitmap memory cache factory.
    /// </summary>
    public class BitmapMemoryCacheFactory
    {
        /// <summary>
        /// Gets the instrumented memory cache.
        /// </summary>
        public static IMemoryCache<ICacheKey, CloseableImage> Get(
            CountingMemoryCache<ICacheKey, CloseableImage> bitmapCountingMemoryCache,
            IImageCacheStatsTracker imageCacheStatsTracker)
        {
            imageCacheStatsTracker.RegisterBitmapMemoryCache(bitmapCountingMemoryCache);

            IMemoryCacheTracker memoryCacheTracker = new MemoryCacheTrackerImpl(
                () =>
                {
                    imageCacheStatsTracker.OnBitmapCacheHit();
                },
                () =>
                {
                    imageCacheStatsTracker.OnBitmapCacheMiss();
                },
                () =>
                {
                    imageCacheStatsTracker.OnBitmapCachePut();
                });

            return new InstrumentedMemoryCache<ICacheKey, CloseableImage>(
                bitmapCountingMemoryCache, 
                memoryCacheTracker);
        }
    }
}
