using FBCore.Common.Internal;
using System.Collections.Generic;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Config parameters for pools (<see cref="BasePool{T}"/>).
    /// Supplied via a provider.
    /// <para />
    /// <see cref="MaxSizeSoftCap"/>
    /// This represents a soft cap on the size of the pool.
    /// When the pool size hits this limit, the pool tries to
    /// trim its free space until either the pool size is below
    /// the soft cap, or the free space is zero.
    /// Note that allocations will not fail because we have
    /// exceeded the soft cap.
    /// <para />
    /// <see cref="MaxSizeHardCap"/>
    /// The hard cap represents a hard cap on the size of the pool.
    /// When the pool size exceeds this cap, allocations will start
    /// failing with a <see cref="PoolSizeViolationException"/>.
    /// <para />
    /// <see cref="BucketSizes"/>
    /// The pool can be configured with a set of 'sizes' - a bucket
    /// is created for each such size.
    /// Additionally, each bucket can have a a max-length specified,
    /// which is the sum of the used and free items in that bucket.
    /// As with the MaxSize parameter above, the maxLength here is
    /// a soft cap, in that it will not cause an exception on get;
    /// it simply controls the release path. When the bucket sizes
    /// are specified upfront, the pool may still get requests for
    /// non standard sizes. Such cases are treated as plain 
    /// Alloc/Free calls i.e. the values are not maintained in the
    /// pool.
    /// If this parameter is null, then the pool will create buckets
    /// on demand.
    /// <para />
    /// <see cref="MinBucketSize"/>
    /// This represents the minimum size of the buckets in the pool.
    /// This assures that all buckets can hold any element smaller
    /// or equal to this size.
    /// <para />
    /// <see cref="MaxBucketSize"/>
    /// This represents the maximum size of the buckets in the pool.
    /// This restricts all buckets to only accept elements smaller
    /// or equal to this size. If this size is exceeded, an exception
    /// will be thrown.
    /// </summary>
    public class PoolParams
    {
        /// <summary>
        /// If MaxNumThreads is set to this level, the pool doesn't
        /// actually care what it is.
        /// </summary>
        public const int IGNORE_THREADS = -1;

        /// <summary>
        /// The hard cap represents a hard cap on the size of
        /// the pool.
        /// </summary>
        public int MaxSizeHardCap { get; }

        /// <summary>
        /// This represents a soft cap on the size of the pool.
        /// </summary>
        public int MaxSizeSoftCap { get; }

        /// <summary>
        /// The pool can be configured with a set of 'sizes' -
        /// a bucket is created for each such size.
        /// </summary>
        public Dictionary<int, int> BucketSizes { get; }

        /// <summary>
        /// This represents the minimum size of the buckets in
        /// the pool.
        /// </summary>
        public int MinBucketSize { get; }

        /// <summary>
        /// This represents the maximum size of the buckets in
        /// the pool.
        /// </summary>
        public int MaxBucketSize { get; }

        /// <summary>
        /// The maximum number of threads that may be accessing
        /// this pool.
        ///
        /// <para />Pool implementations may or may not need
        /// this to be set.
        /// </summary>
        public int MaxNumThreads { get; }

        /// <summary>
        /// Set up pool params.
        /// </summary>
        /// <param name="maxSize">
        /// Soft-cap and hard-cap on size of the pool.
        /// </param>
        /// <param name="bucketSizes">
        /// (Optional) bucket sizes and lengths for the pool.
        /// </param>
        public PoolParams(int maxSize, Dictionary<int, int> bucketSizes) :
            this(maxSize, maxSize, bucketSizes, 0, int.MaxValue, IGNORE_THREADS)
        {
        }

        /// <summary>
        /// Set up pool params.
        /// </summary>
        /// <param name="maxSizeSoftCap">
        /// Soft cap on max size of the pool.
        /// </param>
        /// <param name="maxSizeHardCap">
        /// Hard cap on max size of the pool.
        /// </param>
        /// <param name="bucketSizes">
        /// (Optional) bucket sizes and lengths for the pool.
        /// </param>
        public PoolParams(int maxSizeSoftCap, int maxSizeHardCap, Dictionary<int, int> bucketSizes) :
            this(maxSizeSoftCap, maxSizeHardCap, bucketSizes, 0, int.MaxValue, IGNORE_THREADS)
        {
        }

        /// <summary>
        /// Set up pool params.
        /// </summary>
        /// <param name="maxSizeSoftCap">
        /// Soft cap on max size of the pool.
        /// </param>
        /// <param name="maxSizeHardCap">
        /// Hard cap on max size of the pool.
        /// </param>
        /// <param name="bucketSizes">
        /// (Optional) bucket sizes and lengths for the pool.
        /// </param>
        /// <param name="minBucketSize">
        /// Min bucket size for the pool.
        /// </param>
        /// <param name="maxBucketSize">
        /// Max bucket size for the pool.
        /// </param>
        /// <param name="maxNumThreads">
        /// The maximum number of threads in the pool, or -1
        /// if the pool doesn't care.
        /// </param>

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
