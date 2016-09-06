using FBCore.Common.Internal;
using FBCore.Common.Memory;
using FBCore.Common.References;
using System.Collections.Generic;

namespace ImagePipeline.Memory
{
    /**
     * A special byte-array pool designed to minimize allocations.
     *
     * <p>The length of each bucket's free list is capped at the number of threads using the pool.
     * <p>The free list of each bucket uses {@link OOMSoftReference}s.
     */
    public class FlexByteArrayPool
    {
        private readonly IResourceReleaser<byte[]> _resourceReleaser;
        internal SoftRefByteArrayPool _delegatePool;

        public FlexByteArrayPool(
            IMemoryTrimmableRegistry memoryTrimmableRegistry,
            PoolParams args)
        {
            Preconditions.CheckArgument(args.MaxNumThreads > 0);
            _delegatePool = new SoftRefByteArrayPool(memoryTrimmableRegistry, args, NoOpPoolStatsTracker.GetInstance());
            _resourceReleaser = new FlexByteArrayResourceReleaser(this);
        }

        public CloseableReference<byte[]> Get(int size)
        {
            return CloseableReference<byte[]>.of(_delegatePool.Get(size), _resourceReleaser);
        }

        public void Release(byte[] value)
        {
            _delegatePool.Release(value);
        }

        public Dictionary<string, int> GetStats()
        {
            return _delegatePool.GetStats();
        }

        public int GetMinBufferSize()
        {
            return _delegatePool.GetMinBufferSize();
        }
    }
}
