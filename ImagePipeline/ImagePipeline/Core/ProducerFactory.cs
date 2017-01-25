using Cache.Common;
using ImagePipeline.Cache;
using ImagePipeline.Image;
using ImagePipeline.Memory;

namespace ImagePipeline.Core
{
    /// <summary>
    /// Producer factory
    /// </summary>
    public class ProducerFactory
    {
        // Local dependencies
        // To be implemented

        // Decode dependencies
        // To be implemented

        // Dependencies used by multiple steps
        private readonly IExecutorSupplier _executorSupplier;
        private readonly IPooledByteBufferFactory _pooledByteBufferFactory;

        // Cache dependencies
        private readonly BufferedDiskCache _defaultBufferedDiskCache;
        private readonly BufferedDiskCache _smallImageBufferedDiskCache;
        private readonly IMemoryCache<ICacheKey, IPooledByteBuffer> _encodedMemoryCache;
        private readonly IMemoryCache<ICacheKey, CloseableImage> _bitmapMemoryCache;
        private readonly ICacheKeyFactory _cacheKeyFactory;
        private readonly int _forceSmallCacheThresholdBytes;

        // Postproc dependencies
        // To be implemented
    }
}
