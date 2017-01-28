using Cache.Common;
using FBCore.Common.Internal;
using FBCore.Common.Memory;
using ImagePipeline.Bitmaps;
using ImagePipeline.Image;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// Factory for bitmap <see cref="CountingMemoryCache{K, V}"/>.
    /// </summary>
    public class BitmapCountingMemoryCacheFactory
    {
        /// <summary>
        /// Instantiates the bitmap counting memory cache.
        /// </summary>
        public static CountingMemoryCache<ICacheKey, CloseableImage> Get(
           ISupplier<MemoryCacheParams> bitmapMemoryCacheParamsSupplier,
           IMemoryTrimmableRegistry memoryTrimmableRegistry,
           PlatformBitmapFactory platformBitmapFactory,
           bool isExternalCreatedBitmapLogEnabled)
        {
            IValueDescriptor<CloseableImage> valueDescriptor =
                new ValueDescriptorImpl<CloseableImage>(
                    (value) =>
                    {
                        return value.SizeInBytes;
                    });

            ICacheTrimStrategy trimStrategy = new BitmapMemoryCacheTrimStrategy();

            CountingMemoryCache<ICacheKey, CloseableImage> countingCache =
                new CountingMemoryCache<ICacheKey, CloseableImage>(
                    valueDescriptor,
                    trimStrategy,
                    bitmapMemoryCacheParamsSupplier,
                    platformBitmapFactory,
                    isExternalCreatedBitmapLogEnabled);

            memoryTrimmableRegistry.RegisterMemoryTrimmable(countingCache);

            return countingCache;
        }
    }
}
