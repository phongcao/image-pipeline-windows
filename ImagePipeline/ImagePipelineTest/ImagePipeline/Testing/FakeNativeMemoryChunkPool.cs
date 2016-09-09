using ImagePipeline.Memory;
using System.Collections.Generic;

namespace ImagePipeline.Testing
{
    /// <summary>
    /// A 'fake' <see cref="NativeMemoryChunkPool"/> instance as a test helper
    /// </summary>
    public class FakeNativeMemoryChunkPool : NativeMemoryChunkPool
    {
        /// <summary>
        /// Instantiates the <see cref="FakeNativeMemoryChunkPool"/>.
        /// </summary>
        public FakeNativeMemoryChunkPool() : this(new PoolParams(128, GetBucketSizes()))
        {
        }

        /// <summary>
        /// Instantiates the <see cref="FakeNativeMemoryChunkPool"/>.
        /// </summary>
        public FakeNativeMemoryChunkPool(PoolParams poolParams) : base(
            new MockMemoryTrimmableRegistry(),
            poolParams,
            new MockPoolStatsTracker())
        {
        }

        /// <summary>
        /// Fake Alloc method
        /// </summary>
        /// <param name="bucketedSize"></param>
        /// <returns></returns>
        protected override NativeMemoryChunk Alloc(int bucketedSize)
        {
            return new FakeNativeMemoryChunk(bucketedSize);
        }

        /// <summary>
        /// Fake GetBucketSizes method
        /// </summary>
        /// <returns></returns>
        private static Dictionary<int, int> GetBucketSizes()
        {
            Dictionary<int, int> bucketSizes = new Dictionary<int, int>();
            bucketSizes.Add(4, 10);
            bucketSizes.Add(8, 10);
            bucketSizes.Add(16, 10);
            bucketSizes.Add(32, 10);
            return bucketSizes;
        }
    }
}
