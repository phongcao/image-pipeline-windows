using FBCore.Common.Util;
using System;
using System.Collections.Generic;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Provides pool parameters (<see cref="PoolParams"/>) for <see cref="SharedByteArray"/>
    /// </summary>
    public static class DefaultFlexByteArrayPoolParams
    {
        /// <summary>
        /// The default max buffer size we'll use
        /// </summary>
        public const int DEFAULT_MAX_BYTE_ARRAY_SIZE = 4 * ByteConstants.MB;

        /// <summary>
        /// The min buffer size we'll use
        /// </summary>
        private const int DEFAULT_MIN_BYTE_ARRAY_SIZE = 128 * ByteConstants.KB;

        /// <summary>
        /// The maximum number of threads permitted to touch this pool
        /// </summary>
        public static readonly int DEFAULT_MAX_NUM_THREADS = Environment.ProcessorCount;

        /// <summary>
        /// Generates bucket with parameters
        /// </summary>
        /// <param name="min">The min buffer size</param>
        /// <param name="max">The max buffer size</param>
        /// <param name="numThreads">The number of threads permitted to touch this pool</param>
        /// <returns></returns>
        public static Dictionary<int, int> GenerateBuckets(int min, int max, int numThreads)
        {
            Dictionary<int, int> buckets = new Dictionary<int, int>();
            for (int i = min; i <= max; i *= 2)
            {
                buckets.Add(i, numThreads);
            }

            return buckets;
        }

        /// <summary>
        /// Instantiates the <see cref="PoolParams"/>.
        /// </summary>
        /// <returns></returns>
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
