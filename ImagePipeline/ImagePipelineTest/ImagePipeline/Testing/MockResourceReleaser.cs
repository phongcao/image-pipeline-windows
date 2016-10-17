using FBCore.Common.References;
using System.Threading;

namespace ImagePipeline.Testing
{
    /// <summary>
    /// Mock ResourceReleaser method
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class MockResourceReleaser<T> : IResourceReleaser<T>
    {
        private int _releaseCallCount = 0;

        /// <summary>
        /// Returns the counter
        /// </summary>
        public int ReleasedCallCount
        {
            get
            {
                return Volatile.Read(ref _releaseCallCount);
            }
        }

        /// <summary>
        /// Increased counter
        /// </summary>
        /// <param name="value"></param>
        public void Release(T value)
        {
            Interlocked.Increment(ref _releaseCallCount);
        }
    }
}
