using FBCore.Common.Internal;
using FBCore.Common.Memory;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// A pool of byte arrays.
    /// The pool manages a number of byte arrays of a predefined set of sizes. 
    /// This set of sizes is typically, but not required to be, based on powers of 2.
    /// The pool supports a get/release paradigm.
    /// On a get request, the pool attempts to find an existing byte array whose size
    /// is at least as big as the requested size.
    /// On a release request, the pool adds the byte array to the appropriate bucket.
    /// This byte array can then be used for a subsequent get request.
    /// </summary>
    public class GenericByteArrayPool : BasePool<byte[]>, IByteArrayPool
    {
        private int[] _bucketSizes;

        /// <summary>
        /// Creates a new instance of the GenericByteArrayPool class.
        /// <param name="memoryTrimmableRegistry">The memory manager to register with.</param>
        /// <param name="poolParams">Provider for pool parameters.</param>
        /// <param name="poolStatsTracker">Listener that logs pool statistics.</param>
        /// </summary>
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
        /// @return the smallest buffer size supported by the pool.
        /// </summary>
        public int GetMinBufferSize()
        {
            return _bucketSizes[0];
        }

        /// <summary>
        /// Allocate a buffer greater than or equal to the specified size
        /// <param name="bucketedSize">Size of the buffer requested.</param>
        /// @return a byte array of the specified or larger size. Null if the size 
        /// is invalid.
        /// </summary>
        protected internal override byte[] Alloc(int bucketedSize)
        {
            return new byte[bucketedSize];
        }

        /// <summary>
        /// Frees the 'value'
        /// <param name="value">The value to free</param>
        /// </summary>
        protected internal override void Free(byte[] value)
        {
            Preconditions.CheckNotNull(value);
            // Do nothing. Let the GC take care of this
        }

        /// <summary>
        /// Get the 'bucketed' size for the given request size. The 'bucketed' size is 
        /// a size that is the same or larger than the request size. We walk through our 
        /// list of pre-defined bucket sizes, and use that to determine the smallest bucket 
        /// size that is larger than the requested size.
        /// If no such 'bucketedSize' is found, then we simply return "requestSize".
        /// <param name="requestSize">The logical request size.</param>
        /// @return the bucketed size.
        /// @throws InvalidSizeException, if the requested size was invalid.
        /// </summary>
        protected internal override int GetBucketedSize(int requestSize)
        {
            int intRequestSize = requestSize;
            if (intRequestSize <= 0)
            {
                throw new InvalidSizeException(requestSize);
            }

            // find the smallest bucketed size that is larger than the requested size
            foreach (int bucketedSize in _bucketSizes)
            {
                if (bucketedSize >= intRequestSize)
                {
                    return bucketedSize;
                }
            }

            // Requested size doesn't match our existing buckets - just return the 
            // requested size. This will eventually translate into a plain alloc/free 
            // paradigm
            return requestSize;
        }

        /// <summary>
        /// Gets the bucketed size of the value.
        /// <param name="value">The value.</param>
        /// @return just the length of the value.
        /// </summary>
        protected internal override int GetBucketedSizeForValue(byte[] value)
        {
            Preconditions.CheckNotNull(value);
            return value.Length;
        }
    }
}
