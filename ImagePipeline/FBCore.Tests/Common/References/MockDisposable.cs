using System;
using System.Threading;

namespace FBCore.Tests.Common.References
{
    class MockDisposable : IDisposable
    {
        private int _disposeCallCount = 0;

        public bool Disposed
        {
            get
            {
                return Volatile.Read(ref _disposeCallCount) > 0;
            }
        }

        public void Dispose()
        {
            Interlocked.Increment(ref _disposeCallCount);
        }
    }
}
