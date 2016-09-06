using FBCore.Common.Util;
using System;
using System.Collections.Generic;
using Windows.System;

namespace ImagePipeline.Memory
{
    /**
     * Provides pool parameters for {@link BitmapPool}
     */
    public static class DefaultBitmapPoolParams
    {
        /**
         * We are not reusing Bitmaps and want to free them as soon as possible.
         */
        private const int MAX_SIZE_SOFT_CAP = 0;

        /**
         * Our Bitmaps live in ashmem, meaning that they are pinned in androids' shared native memory.
         * Therefore, we are not constrained by the max heap size of the dalvik heap, but we want to make
         * sure we don't use too much memory on low end devices, so that we don't force other background
         * process to be evicted.
         */
        private static int GetMaxSizeHardCap()
        {
            ulong maxMemory = Math.Min(MemoryManager.AppMemoryUsageLimit, int.MaxValue);
            if (maxMemory > 16 * ByteConstants.MB)
            {
                return (int)(maxMemory / 4 * 3);
            }
            else
            {
                return (int)(maxMemory / 2);
            }
        }

        /**
         * This will cause all get/release calls to behave like alloc/free calls i.e. no pooling.
         */
        private static readonly Dictionary<int, int> DEFAULT_BUCKETS = new Dictionary<int, int>(0);

        public static PoolParams Get()
        {
            return new PoolParams(
                MAX_SIZE_SOFT_CAP,
                GetMaxSizeHardCap(),
                DEFAULT_BUCKETS
            );
        }
    }
}
