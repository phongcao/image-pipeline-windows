using FBCore.Common.References;

namespace FBCore.Tests.Common.References
{
    class MockResourceReleaser<T> : IResourceReleaser<T>
    {
        public int ReleaseCallCount { get; private set; }

        public void Release(T value)
        {
            ++ReleaseCallCount;
        }
    }
}
