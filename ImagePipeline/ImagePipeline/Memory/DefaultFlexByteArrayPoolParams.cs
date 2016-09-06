using FBCore.Common.Util;
using System;
using System.Collections.Generic;

namespace ImagePipeline.Memory
{
    /**
     * Provides pool parameters ({@link PoolParams}) for {@link SharedByteArray}
     */
    public static class DefaultFlexByteArrayPoolParams
    {
        // The default max buffer size we'll use
        public const int DEFAULT_MAX_BYTE_ARRAY_SIZE = 4 * ByteConstants.MB;

        // The min buffer size we'll use
        private const int DEFAULT_MIN_BYTE_ARRAY_SIZE = 128 * ByteConstants.KB;

        // The maximum number of threads permitted to touch this pool
        public static readonly int DEFAULT_MAX_NUM_THREADS = Environment.ProcessorCount;

        public static Dictionary<int, int> GenerateBuckets(int min, int max, int numThreads)
        {
            Dictionary<int, int> buckets = new Dictionary<int, int>();
            for (int i = min; i <= max; i *= 2)
            {
                buckets.Add(i, numThreads);
            }

            return buckets;
        }

        public static PoolParams Get()
        {
            return new PoolParams(
                /* maxSizeSoftCap */ DEFAULT_MAX_BYTE_ARRAY_SIZE,
                /* maxSizeHardCap */ DEFAULT_MAX_NUM_THREADS * DEFAULT_MAX_BYTE_ARRAY_SIZE,
                /* bucketSizes */    GenerateBuckets(
                                        DEFAULT_MIN_BYTE_ARRAY_SIZE,
                                        DEFAULT_MAX_BYTE_ARRAY_SIZE,
                                        DEFAULT_MAX_NUM_THREADS),
                /* minBucketSize */     DEFAULT_MIN_BYTE_ARRAY_SIZE,
                /* maxBucketSize */     DEFAULT_MAX_BYTE_ARRAY_SIZE,
                /* maxNumThreads */     DEFAULT_MAX_NUM_THREADS);
        }
    }
}
