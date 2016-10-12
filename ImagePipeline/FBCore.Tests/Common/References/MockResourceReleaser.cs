using FBCore.Common.References;
using System.Threading;

namespace FBCore.Tests.Common.References
{
    class MockResourceReleaser<T> : IResourceReleaser<T>
    {
        private int _releaseCallCount = 0;

        public bool Released
        {
            get
            {
                return Volatile.Read(ref _releaseCallCount) > 0;
            }
        }

        public void Release(T value)
        {
            Interlocked.Increment(ref _releaseCallCount);
        }
    }
}
