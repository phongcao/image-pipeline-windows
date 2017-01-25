using Cache.Common;
using FBCore.Common.Internal;
using FBCore.Common.Memory;
using ImagePipeline.Bitmaps;
using ImagePipeline.Memory;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// Factory for the encoded <see cref="CountingMemoryCache{K, V}"/>
    /// </summary>
    public class EncodedCountingMemoryCacheFactory
    {
        /// <summary>
        /// Returns the encoded <see cref="CountingMemoryCache{K, V}"/>
        /// </summary>
        public static CountingMemoryCache<ICacheKey, IPooledByteBuffer> Get(
            ISupplier<MemoryCacheParams> encodedMemoryCacheParamsSupplier,
            IMemoryTrimmableRegistry memoryTrimmableRegistry,
            PlatformBitmapFactory platformBitmapFactory)
        {
            IValueDescriptor<IPooledByteBuffer> valueDescriptor =
                new ValueDescriptorImpl<IPooledByteBuffer>(
                    (value) =>
                    {
                        return value.Size;
                    });

            ICacheTrimStrategy trimStrategy = new NativeMemoryCacheTrimStrategy();

            CountingMemoryCache<ICacheKey, IPooledByteBuffer> countingCache =
                new CountingMemoryCache<ICacheKey, IPooledByteBuffer>(
                    valueDescriptor,
                    trimStrategy,
                    encodedMemoryCacheParamsSupplier,
                    platformBitmapFactory,
                    false);

            memoryTrimmableRegistry.RegisterMemoryTrimmable(countingCache);

            return countingCache;
        }
    }
}
