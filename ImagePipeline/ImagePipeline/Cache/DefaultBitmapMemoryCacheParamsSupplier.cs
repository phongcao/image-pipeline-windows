using FBCore.Common.Internal;
using FBCore.Common.Util;
using System;
using Windows.System;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// Supplies <see cref="MemoryCacheParams"/> for the bitmap memory cache.
    /// </summary>
    public class DefaultBitmapMemoryCacheParamsSupplier : ISupplier<MemoryCacheParams>
    {
        private const int MAX_CACHE_ENTRIES = 256;
        private const int MAX_EVICTION_QUEUE_SIZE = int.MaxValue;
        private const int MAX_EVICTION_QUEUE_ENTRIES = int.MaxValue;
        private const int MAX_CACHE_ENTRY_SIZE = int.MaxValue;

        /// <summary>
        /// Gets the memory cache params
        /// </summary>
        public MemoryCacheParams Get()
        {
            return new MemoryCacheParams(
                GetMaxCacheSize(),
                MAX_CACHE_ENTRIES,
                MAX_EVICTION_QUEUE_SIZE,
                MAX_EVICTION_QUEUE_ENTRIES,
                MAX_CACHE_ENTRY_SIZE);
        }

        private int GetMaxCacheSize()
        {
            ulong maxMemory = Math.Min(MemoryManager.AppMemoryUsageLimit, int.MaxValue);

            if (maxMemory < 32 * ByteConstants.MB)
            {
                return 4 * ByteConstants.MB;
            }
            else if (maxMemory < 64 * ByteConstants.MB)
            {
                return 6 * ByteConstants.MB;
            }
            else
            {
                return (int)(maxMemory / 4);
            }
        }
    }
}
