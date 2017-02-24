using FBCore.Common.Internal;
using FBCore.Common.Memory;
using System.Collections.Generic;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Manages a pool of native memory chunks (<see cref="NativeMemoryChunk"/>).
    /// </summary>
    public class NativeMemoryChunkPool : BasePool<NativeMemoryChunk>
    {
        private readonly int[] _bucketSizes;

        /// <summary>
        /// Creates a new instance of <see cref="NativeMemoryChunkPool"/>.
        /// </summary>
        /// <param name="memoryTrimmableRegistry">
        /// The memory manager to register with.
        /// </param>
        /// <param name="poolParams">
        /// Provider for pool parameters.
        /// </param>
        /// <param name="nativeMemoryChunkPoolStatsTracker">
        /// The pool stats tracker.
        /// </param>
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
        /// Gets the smallest size supported by the pool.
        /// </summary>
        /// <returns>
        /// The smallest size supported by the pool.
        /// </returns>
        public int GetMinBufferSize()
        {
            return _bucketSizes[0];
        }

        /// <summary>
        /// Allocate a native memory chunk larger than or equal to
        /// the specified size.
        /// </summary>
        /// <param name="bucketedSize">
        /// Size of the buffer requested.
        /// </param>
        /// <returns>
        /// A native memory chunk of the specified or larger size.
        /// Null if the size is invalid.
        /// </returns>
        protected internal override NativeMemoryChunk Alloc(int bucketedSize)
        {
            return new NativeMemoryChunk(bucketedSize);
        }

        /// <summary>
        /// Frees the 'value'.
        /// </summary>
        /// <param name="value">The value to free.</param>
        protected internal override void Free(NativeMemoryChunk value)
        {
            Preconditions.CheckNotNull(value);
            value.Dispose();
        }

        /// <summary>
        /// Get the 'bucketed' size for the given request size.
        /// The 'bucketed' size is a size that is the same or
        /// larger than the request size. We walk through our 
        /// list of pre-defined bucket sizes, and use that to
        /// determine the smallest bucket size that is larger
        /// than the requested size.
        /// If no such 'bucketedSize' is found, then we simply
        /// return "requestSize".
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
            // just return the requested size.
            // This will eventually translate into a plain 
            // Alloc / Free paradigm.
            return requestSize;
        }

        /// <summary>
        /// Gets the bucketed size of the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Just the length of the value.</returns>
        protected internal override int GetBucketedSizeForValue(NativeMemoryChunk value)
        {
            Preconditions.CheckNotNull(value);
            return value.Size;
        }

        /// <summary>
        /// Checks if the value is reusable (for subsequent
        /// <see cref="BasePool{T}.Get(int)"/> operations.
        /// The value is reusable, if
        ///  - It hasn't already been freed.
        /// </summary>
        /// <param name="value">
        /// The value to test for reusability.
        /// </param>
        /// <returns>
        /// true, if the value is reusable.
        /// </returns>
        protected internal override bool IsReusable(NativeMemoryChunk value)
        {
            Preconditions.CheckNotNull(value);
            return !value.Closed;
        }
    }
}
