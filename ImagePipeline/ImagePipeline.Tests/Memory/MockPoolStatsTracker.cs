using ImagePipeline.Memory;

namespace ImagePipeline.Tests.Memory
{
    /// <summary>
    /// Mock pool stats tracker for unit tests
    /// </summary>
    public class MockPoolStatsTracker : PoolStatsTracker
    {
        /// <summary>
        /// Mock OnAlloc
        /// </summary>
        public override void OnAlloc(int size)
        {
        }

        /// <summary>
        /// Mock OnFree
        /// </summary>
        public override void OnFree(int sizeInBytes)
        {
        }

        /// <summary>
        /// Mock OnHardCapReached
        /// </summary>
        public override void OnHardCapReached()
        {
        }

        /// <summary>
        /// Mock OnSoftCapReached
        /// </summary>
        public override void OnSoftCapReached()
        {
        }

        /// <summary>
        /// Mock OnValueRelease
        /// </summary>
        public override void OnValueRelease(int sizeInBytes)
        {
        }

        /// <summary>
        /// Mock OnValueReuse
        /// </summary>
        public override void OnValueReuse(int bucketedSize)
        {
        }

        /// <summary>
        /// Mock SetBasePool
        /// </summary>
        public override void SetBasePool<T>(BasePool<T> basePool)
        {
        }
    }
}
