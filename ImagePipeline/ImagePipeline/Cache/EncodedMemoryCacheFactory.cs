using Cache.Common;
using ImagePipeline.Memory;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// Factory for the instrumented <see cref="EncodedCountingMemoryCacheFactory"/>"/>
    /// </summary>
    public class EncodedMemoryCacheFactory
    {
        /// <summary>
        /// Returns the instrumented <see cref="EncodedCountingMemoryCacheFactory"/>
        /// </summary>
        public static IMemoryCache<ICacheKey, IPooledByteBuffer> Get(
            CountingMemoryCache<ICacheKey, IPooledByteBuffer> encodedCountingMemoryCache,
            IImageCacheStatsTracker imageCacheStatsTracker)
        {
            imageCacheStatsTracker.RegisterEncodedMemoryCache(encodedCountingMemoryCache);

            IMemoryCacheTracker memoryCacheTracker = new MemoryCacheTrackerImpl(
                () =>
                {
                    imageCacheStatsTracker.OnMemoryCacheHit();
                },
                () =>
                {
                    imageCacheStatsTracker.OnMemoryCacheMiss();
                },
                () =>
                {
                    imageCacheStatsTracker.OnMemoryCachePut();
                });

            return new InstrumentedMemoryCache<ICacheKey, IPooledByteBuffer>(
                encodedCountingMemoryCache, 
                memoryCacheTracker);
        }
    }
}
