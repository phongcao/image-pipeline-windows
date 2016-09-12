using ImagePipeline.Memory;
using System.Threading;

namespace ImagePipeline.Testing
{
    /// <summary>
    /// Mock pool stats tracker for unit tests
    /// </summary>
    public class MockPoolStatsTracker : PoolStatsTracker
    {
        private int _allocCallCount = 0;
        private int _freeCallCount = 0;
        private int _releaseCallCount = 0;
        private int _reuseCallCount = 0;

        /// <summary>
        /// Returns how many times the Alloc method is invoked
        /// </summary>
        public int AllocCallCount
        {
            get
            {
                return Volatile.Read(ref _allocCallCount);
            }
        }

        /// <summary>
        /// Returns how many times the Alloc method is invoked
        /// </summary>
        public int FreeCallCount
        {
            get
            {
                return Volatile.Read(ref _freeCallCount);
            }
        }

        /// <summary>
        /// Returns how many times the Alloc method is invoked
        /// </summary>
        public int ReleaseCallCount
        {
            get
            {
                return Volatile.Read(ref _releaseCallCount);
            }
        }

        /// <summary>
        /// Returns how many times the Alloc method is invoked
        /// </summary>
        public int ReuseCallCount
        {
            get
            {
                return Volatile.Read(ref _reuseCallCount);
            }
        }

        /// <summary>
        /// Mock OnAlloc
        /// </summary>
        public override void OnAlloc(int size)
        {
            Interlocked.Increment(ref _allocCallCount);
        }

        /// <summary>
        /// Mock OnFree
        /// </summary>
        public override void OnFree(int sizeInBytes)
        {
            Interlocked.Increment(ref _freeCallCount);
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
            Interlocked.Increment(ref _releaseCallCount);
        }

        /// <summary>
        /// Mock OnValueReuse
        /// </summary>
        public override void OnValueReuse(int bucketedSize)
        {
            Interlocked.Increment(ref _reuseCallCount);
        }

        /// <summary>
        /// Mock SetBasePool
        /// </summary>
        public override void SetBasePool<T>(BasePool<T> basePool)
        {
        }
    }
}
