using System;

namespace FBCore.Tests.Common.References
{
    class MockDisposable : IDisposable
    {
        public int DisposeCallCount { get; private set; }

        public void Dispose()
        {
            ++DisposeCallCount;
        }
    }
}
