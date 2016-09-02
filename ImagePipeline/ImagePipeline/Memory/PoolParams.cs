using FBCore.Common.Internal;
using System.Collections.Generic;

namespace ImagePipeline.Memory
{
    /**
     * Config parameters for pools ({@link BasePool}. Supplied via a provider.
     * <p>
     * {@link #maxSizeSoftCap}
     * This represents a soft cap on the size of the pool. When the pool size hits this limit, the pool
     * tries to trim its free space until either the pool size is below the soft cap, or the
     * free space is zero.
     * Note that allocations will not fail because we have exceeded the soft cap
     * <p>
     * {@link #maxSizeHardCap}
     * The hard cap represents a hard cap on the size of the pool. When the pool size exceeds this cap,
     * allocations will start failing with a {@link BasePool.PoolSizeViolationException}
     * <p>
     * {@link #bucketSizes}
     * The pool can be configured with a set of 'sizes' - a bucket is created for each such size.
     * Additionally, each bucket can have a a max-length specified, which is the sum of the used and
     * free items in that bucket. As with the MaxSize parameter above, the maxLength here is a soft
     * cap, in that it will not cause an exception on get; it simply controls the release path
     * When the bucket sizes are specified upfront, the pool may still get requests for non standard
     * sizes. Such cases are treated as plain alloc/free calls i.e. the values are not maintained in
     * the pool.
     * If this parameter is null, then the pool will create buckets on demand
     * <p>
     * {@link #minBucketSize}
     * This represents the minimum size of the buckets in the pool. This assures that all buckets can
     * hold any element smaller or equal to this size.
     * <p>
     * {@link #maxBucketSize}
     * This represents the maximum size of the buckets in the pool. This restricts all buckets to only
     * accept elements smaller or equal to this size. If this size is exceeded, an exception will be
     * thrown.
     */
    public class PoolParams
    {
        /** If maxNumThreads is set to this level, the pool doesn't actually care what it is */
        public const int IGNORE_THREADS = -1;

        public int MaxSizeHardCap { get; }

        public int MaxSizeSoftCap { get; }

        public Dictionary<int, int> BucketSizes { get; }

        public int MinBucketSize { get; }

        public int MaxBucketSize { get; }

        /** The maximum number of threads that may be accessing this pool.
         *
         * <p>Pool implementations may or may not need this to be set.
         */
        public int MaxNumThreads { get; }

        /**
         * Set up pool params
         * @param maxSize soft-cap and hard-cap on size of the pool
         * @param bucketSizes (optional) bucket sizes and lengths for the pool
         */
        public PoolParams(int maxSize, Dictionary<int, int> bucketSizes) :
            this(maxSize, maxSize, bucketSizes, 0, int.MaxValue, IGNORE_THREADS)
        {
        }

        /**
         * Set up pool params
         * @param maxSizeSoftCap soft cap on max size of the pool
         * @param maxSizeHardCap hard cap on max size of the pool
         * @param bucketSizes (optional) bucket sizes and lengths for the pool
         */
        public PoolParams(int maxSizeSoftCap, int maxSizeHardCap, Dictionary<int, int> bucketSizes) :
            this(maxSizeSoftCap, maxSizeHardCap, bucketSizes, 0, int.MaxValue, IGNORE_THREADS)
        {
        }

        /**
         * Set up pool params
         * @param maxSizeSoftCap soft cap on max size of the pool
         * @param maxSizeHardCap hard cap on max size of the pool
         * @param bucketSizes (optional) bucket sizes and lengths for the pool
         * @param minBucketSize min bucket size for the pool
         * @param maxBucketSize max bucket size for the pool
         * @param maxNumThreads the maximum number of threads in th epool, or -1 if the pool doesn't care
         */
        public PoolParams(
            int maxSizeSoftCap,
            int maxSizeHardCap,
            Dictionary<int, int> bucketSizes,
            int minBucketSize,
            int maxBucketSize,
            int maxNumThreads)
        {
            Preconditions.CheckState(maxSizeSoftCap >= 0 && maxSizeHardCap >= maxSizeSoftCap);
            MaxSizeSoftCap = maxSizeSoftCap;
            MaxSizeHardCap = maxSizeHardCap;
            BucketSizes = bucketSizes;
            MinBucketSize = minBucketSize;
            MaxBucketSize = maxBucketSize;
            MaxNumThreads = maxNumThreads;
        }
    }
}
