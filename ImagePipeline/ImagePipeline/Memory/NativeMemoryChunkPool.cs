using FBCore.Common.Internal;
using FBCore.Common.Memory;
using System.Collections.Generic;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Manages a pool of native memory chunks (<see cref="NativeMemoryChunk"/>)
    /// </summary>
    public class NativeMemoryChunkPool : BasePool<NativeMemoryChunk>
    {
        private readonly int[] _bucketSizes;

        /// <summary>
        /// Creates a new instance of the NativeMemoryChunkPool class
        /// <param name="memoryTrimmableRegistry">the memory manager to register with</param>
        /// <param name="poolParams">provider for pool parameters</param>
        /// <param name="nativeMemoryChunkPoolStatsTracker"></param>
        /// </summary>
        public NativeMemoryChunkPool(
            IMemoryTrimmableRegistry memoryTrimmableRegistry,
            PoolParams poolParams,
            PoolStatsTracker nativeMemoryChunkPoolStatsTracker) : base(
                memoryTrimmableRegistry,
                poolParams,
                nativeMemoryChunkPoolStatsTracker)
        {
            Dictionary<int, int> bucketSizes = poolParams.BucketSizes;
            _bucketSizes = new int[bucketSizes.Keys.Count];
            poolParams.BucketSizes.Keys.CopyTo(_bucketSizes, 0);
            Initialize();
        }

        /// <summary>
        /// Gets the smallest size supported by the pool
        /// @return the smallest size supported by the pool
        /// </summary>
        public int GetMinBufferSize()
        {
            return _bucketSizes[0];
        }

        /// <summary>
        /// Allocate a native memory chunk larger than or equal to the specified size
        /// <param name="bucketedSize">size of the buffer requested</param>
        /// @return a native memory chunk of the specified or larger size. Null if the size is invalid
        /// </summary>
        protected internal override NativeMemoryChunk Alloc(int bucketedSize)
        {
            return new NativeMemoryChunk(bucketedSize);
        }

        /// <summary>
        /// Frees the 'value'
        /// <param name="value">the value to free</param>
        /// </summary>
        protected internal override void Free(NativeMemoryChunk value)
        {
            Preconditions.CheckNotNull(value);
            value.Dispose();
        }

        /// <summary>
        /// Gets the size in bytes for the given 'bucketed' size
        /// <param name="bucketedSize">the bucketed size</param>
        /// @return size in bytes
        /// </summary>
        protected internal override int GetSizeInBytes(int bucketedSize)
        {
            return bucketedSize;
        }

        /// <summary>
        /// Get the 'bucketed' size for the given request size. The 'bucketed' size is a size that is
        /// the same or larger than the request size. We walk through our list of pre-defined bucket
        /// sizes, and use that to determine the smallest bucket size that is larger than the requested
        /// size.
        /// If no such 'bucketedSize' is found, then we simply return "requestSize"
        /// <param name="requestSize">the logical request size</param>
        /// @return the bucketed size
        /// @throws InvalidSizeException, if the requested size was invalid
        /// </summary>
        protected internal override int GetBucketedSize(int requestSize)
        {
            int intRequestSize = requestSize;
            if (intRequestSize <= 0)
            {
                throw new InvalidSizeException(requestSize);
            }

            // Find the smallest bucketed size that is larger than the requested size
            foreach (int bucketedSize in _bucketSizes)
            {
                if (bucketedSize >= intRequestSize)
                {
                    return bucketedSize;
                }
            }

            // Requested size doesn't match our existing buckets - just return the requested size
            // This will eventually translate into a plain alloc/free paradigm
            return requestSize;
        }

        /// <summary>
        /// Gets the bucketed size of the value
        /// <param name="value">the value</param>
        /// @return just the length of the value
        /// </summary>
        protected internal override int GetBucketedSizeForValue(NativeMemoryChunk value)
        {
            Preconditions.CheckNotNull(value);
            return value.Size;
        }

        /// <summary>
        /// Checks if the value is reusable (for subsequent <see cref="BasePool&lt;T&gt;.Get(int)"/> operations.
        /// The value is reusable, if
        ///  - it hasn't already been freed
        /// <param name="value">the value to test for reusability</param>
        /// @return true, if the value is reusable
        /// </summary>
        protected internal override bool IsReusable(NativeMemoryChunk value)
        {
            Preconditions.CheckNotNull(value);
            return !value.IsClosed;
        }
    }
}
