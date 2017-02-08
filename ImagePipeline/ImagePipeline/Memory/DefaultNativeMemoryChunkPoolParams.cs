using FBCore.Common.Util;
using System;
using System.Collections.Generic;
using Windows.System;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Provides pool parameters (<see cref="PoolParams"/>) for <see cref="NativeMemoryChunkPool"/>
    ///
    /// </summary>
    public static class DefaultNativeMemoryChunkPoolParams
    {
        /// <summary>
        /// Length of 'small' sized buckets. Bucket lengths for these buckets are larger because
        /// they're smaller in size
        /// </summary>
        private const int SMALL_BUCKET_LENGTH = 5;

        /// <summary>
        /// Bucket lengths for 'large' (> 256KB) buckets
        /// </summary>
        private const int LARGE_BUCKET_LENGTH = 2;

        /// <summary>
        /// Gets the default pool params
        /// </summary>
        /// <returns>The default pool params</returns>
        public static PoolParams Get()
        {
            Dictionary<int, int> DEFAULT_BUCKETS = new Dictionary<int, int>();
            DEFAULT_BUCKETS.Add(1 * ByteConstants.KB, SMALL_BUCKET_LENGTH);
            DEFAULT_BUCKETS.Add(2 * ByteConstants.KB, SMALL_BUCKET_LENGTH);
            DEFAULT_BUCKETS.Add(4 * ByteConstants.KB, SMALL_BUCKET_LENGTH);
            DEFAULT_BUCKETS.Add(8 * ByteConstants.KB, SMALL_BUCKET_LENGTH);
            DEFAULT_BUCKETS.Add(16 * ByteConstants.KB, SMALL_BUCKET_LENGTH);
            DEFAULT_BUCKETS.Add(32 * ByteConstants.KB, SMALL_BUCKET_LENGTH);
            DEFAULT_BUCKETS.Add(64 * ByteConstants.KB, SMALL_BUCKET_LENGTH);
            DEFAULT_BUCKETS.Add(128 * ByteConstants.KB, SMALL_BUCKET_LENGTH);
            DEFAULT_BUCKETS.Add(256 * ByteConstants.KB, LARGE_BUCKET_LENGTH);
            DEFAULT_BUCKETS.Add(512 * ByteConstants.KB, LARGE_BUCKET_LENGTH);
            DEFAULT_BUCKETS.Add(1024 * ByteConstants.KB, LARGE_BUCKET_LENGTH);

            return new PoolParams(
                GetMaxSizeSoftCap(),
                GetMaxSizeHardCap(),
                DEFAULT_BUCKETS);
        }

        /// <summary>
        /// Gets the soft cap on max size of the pool.
        /// </summary>
        private static int GetMaxSizeSoftCap()
        {
            ulong maxMemory = Math.Min(MemoryManager.AppMemoryUsageLimit, int.MaxValue);
            if (maxMemory < 16 * ByteConstants.MB)
            {
                return 3 * ByteConstants.MB;
            }
            else if (maxMemory < 32 * ByteConstants.MB)
            {
                return 6 * ByteConstants.MB;
            }
            else
            {
                // Phong Cao: Increases pool size for Windows devices
                return 50 * ByteConstants.MB; // 12 * ByteConstants.MB
            }
        }

        /// <summary>
        /// Gets the hard cap on max size of the pool.
        /// </summary>
        private static int GetMaxSizeHardCap()
        {
            ulong maxMemory = Math.Min(MemoryManager.AppMemoryUsageLimit, int.MaxValue);
            if (maxMemory < 16 * ByteConstants.MB)
            {
                return (int)(maxMemory / 2);
            }
            else
            {
                return (int)(maxMemory / 4 * 3);
            }
        }
    }
}
