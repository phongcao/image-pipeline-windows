using FBCore.Common.Internal;
using FBCore.Common.Util;
using System;
using Windows.System;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// Supplies <see cref="MemoryCacheParams"/> for the encoded image
    /// memory cache.
    /// </summary>
    public class DefaultEncodedMemoryCacheParamsSupplier : ISupplier<MemoryCacheParams>
    {
        // We want memory cache to be bound only by its memory consumption
        private const int MAX_CACHE_ENTRIES = int.MaxValue;
        private const int MAX_EVICTION_QUEUE_ENTRIES = MAX_CACHE_ENTRIES;

        /// <summary>
        /// Gets the memory cache params.
        /// </summary>
        public MemoryCacheParams Get()
        {
            int maxCacheSize = GetMaxCacheSize();
            int maxCacheEntrySize = maxCacheSize / 8;
            return new MemoryCacheParams(
                maxCacheSize,
                MAX_CACHE_ENTRIES,
                maxCacheSize,
                MAX_EVICTION_QUEUE_ENTRIES,
                maxCacheEntrySize);
        }

        private int GetMaxCacheSize()
        {
            ulong maxMemory = Math.Min(MemoryManager.AppMemoryUsageLimit, int.MaxValue);
            if (maxMemory < 16 * ByteConstants.MB)
            {
                return 1 * ByteConstants.MB;
            }
            else if (maxMemory < 32 * ByteConstants.MB)
            {
                return 2 * ByteConstants.MB;
            }
            else
            {
                // Phong Cao: Increases pool size for Windows devices
                return 20 * ByteConstants.MB; // return 4 * ByteConstants.MB;
            }
        }
    }
}
