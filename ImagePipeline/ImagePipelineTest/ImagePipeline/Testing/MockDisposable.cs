using System;
using System.Threading;

namespace ImagePipeline.Testing
{
    /// <summary>
    /// Mock Disposable class
    /// </summary>
    public class MockDisposable : IDisposable
    {
        private int _disposeCallCount = 0;

        /// <summary>
        /// Returns true if Dispose method has been invoked
        /// </summary>
        public bool IsDisposed
        {
            get
            {
                return Volatile.Read(ref _disposeCallCount) > 0;
            }
        }

        /// <summary>
        /// Increases the counter
        /// </summary>
        public void Dispose()
        {
            Interlocked.Increment(ref _disposeCallCount);
        }
    }
}
