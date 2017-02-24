using FBCore.Common.Internal;
using FBCore.Common.Memory;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// A pool of byte arrays.
    /// The pool manages a number of byte arrays of a predefined set
    /// of sizes. This set of sizes is typically, but not required
    /// to be, based on powers of 2.
    /// The pool supports a Get/Release paradigm.
    /// On a Get request, the pool attempts to find an existing byte
    /// array whose size is at least as big as the requested size.
    /// On a Release request, the pool adds the byte array to the
    /// appropriate bucket.
    /// This byte array can then be used for a subsequent Get request.
    /// </summary>
    public class GenericByteArrayPool : BasePool<byte[]>, IByteArrayPool
    {
        private int[] _bucketSizes;

        /// <summary>
        /// Creates a new instance of the GenericByteArrayPool class.
        /// </summary>
        /// <param name="memoryTrimmableRegistry">
        /// The memory manager to register with.
        /// </param>
        /// <param name="poolParams">
        /// Provider for pool parameters.
        /// </param>
        /// <param name="poolStatsTracker">
        /// Listener that logs pool statistics.
        /// </param>
        public GenericByteArrayPool(
            IMemoryTrimmableRegistry memoryTrimmableRegistry,
            PoolParams poolParams,
            PoolStatsTracker poolStatsTracker) : 
            base(memoryTrimmableRegistry, poolParams, poolStatsTracker)
        {
            _bucketSizes = new int[poolParams.BucketSizes.Keys.Count];
            poolParams.BucketSizes.Keys.CopyTo(_bucketSizes, 0);
            Initialize();
        }

        /// <summary>
        /// Gets the smallest buffer size supported by the pool.
        /// </summary>
        /// <returns>
        /// The smallest buffer size supported by the pool.
        /// </returns>
        public int GetMinBufferSize()
        {
            return _bucketSizes[0];
        }

        /// <summary>
        /// Allocate a buffer greater than or equal to the specified size.
        /// </summary>
        /// <param name="bucketedSize">
        /// Size of the buffer requested.
        /// </param>
        /// <returns>
        /// A byte array of the specified or larger size. Null if the size
        /// is invalid.
        /// </returns>
        protected internal override byte[] Alloc(int bucketedSize)
        {
            return new byte[bucketedSize];
        }

        /// <summary>
        /// Frees the 'value'.
        /// </summary>
        /// <param name="value">The value to free.</param>
        protected internal override void Free(byte[] value)
        {
            Preconditions.CheckNotNull(value);
            // Do nothing. Let the GC take care of this
        }

        /// <summary>
        /// Get the 'bucketed' size for the given request size.
        /// The 'bucketed' size is  a size that is the same or larger
        /// than the request size. We walk through our list of
        /// pre-defined bucket sizes, and use that to determine the
        /// smallest bucket  size that is larger than the requested size.
        /// If no such 'bucketedSize' is found, then we simply return
        /// "requestSize".
        /// </summary>
        /// <param name="requestSize">
        /// The logical request size.
        /// </param>
        /// <returns>The bucketed size.</returns>
        /// <exception cref="InvalidSizeException">
        /// If the requested size was invalid.
        /// </exception>
        protected internal override int GetBucketedSize(int requestSize)
        {
            int intRequestSize = requestSize;
            if (intRequestSize <= 0)
            {
                throw new InvalidSizeException(requestSize);
            }

            // Find the smallest bucketed size that is larger than
            // the requested size
            foreach (int bucketedSize in _bucketSizes)
            {
                if (bucketedSize >= intRequestSize)
                {
                    return bucketedSize;
                }
            }

            // Requested size doesn't match our existing buckets -
            // just return the requested size. This will eventually
            // translate into a plain Alloc/Free paradigm.
            return requestSize;
        }

        /// <summary>
        /// Gets the bucketed size of the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Just the length of the value.</returns>
        protected internal override int GetBucketedSizeForValue(byte[] value)
        {
            Preconditions.CheckNotNull(value);
            return value.Length;
        }
    }
}
