using FBCore.Common.Internal;
using FBCore.Common.Memory;
using FBCore.Common.References;
using System.Collections.Generic;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// A special byte-array pool designed to minimize allocations.
    ///
    /// <para />The length of each bucket's free list is capped at the number of threads using the pool.
    /// <para />The free list of each bucket uses <see cref="OOMSoftReference{T}"/>s.
    /// </summary>
    public class FlexByteArrayPool
    {
        private readonly IResourceReleaser<byte[]> _resourceReleaser;
        internal SoftRefByteArrayPool _delegatePool;

        /// <summary>
        /// Instantiates the <see cref="FlexByteArrayPool"/>.
        /// </summary>
        /// <param name="memoryTrimmableRegistry">A class to be notified of system memory events</param>
        /// <param name="args">The pool params</param>
        public FlexByteArrayPool(
            IMemoryTrimmableRegistry memoryTrimmableRegistry,
            PoolParams args)
        {
            Preconditions.CheckArgument(args.MaxNumThreads > 0);
            _delegatePool = new SoftRefByteArrayPool(memoryTrimmableRegistry, args, NoOpPoolStatsTracker.Instance);
            _resourceReleaser = new ResourceReleaserImpl<byte[]>(value => Release(value));
        }

        /// <summary>
        /// Constructs a CloseableReference of byte[] with provided
        /// </summary>
        /// <param name="size">Byte array size</param>
        /// <returns>CloseableReference of byte[]</returns>
        public CloseableReference<byte[]> Get(int size)
        {
            return CloseableReference<byte[]>.of(_delegatePool.Get(size), _resourceReleaser);
        }

        /// <summary>
        /// Releases the given value to the pool.
        /// </summary>
        /// <param name="value">Byte array</param>
        public void Release(byte[] value)
        {
            _delegatePool.Release(value);
        }

        /// <summary>
        /// Gets the memory stats regarding buckets used, memory caps, reused values.
        /// </summary>
        /// <returns>The memory stats regarding buckets used, memory caps, reused values.</returns>
        public Dictionary<string, int> GetStats()
        {
            return _delegatePool.GetStats();
        }

        /// <summary>
        /// Gets the min buffer size
        /// </summary>
        /// <returns>The min buffer size</returns>
        public int GetMinBufferSize()
        {
            return _delegatePool.GetMinBufferSize();
        }
    }
}
