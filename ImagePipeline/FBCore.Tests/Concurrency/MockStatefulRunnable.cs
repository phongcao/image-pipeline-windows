using FBCore.Concurrency;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FBCore.Tests.Concurrency
{
    class MockStatefulRunnable<T> : StatefulRunnable<T>
    {
        private T _result;
        private int _onSuccessCallCount;
        private int _onFailureCallCount;
        private int _onCancellationCallCount;
        private int _disposeResultCallCount;
        private int _getResultCallCount;
        private Exception _exceptionOnSuccess;
        private Exception _exceptionGetResult;

        /// <summary>
        /// Called after computing result successfully.
        /// <param name="result"></param>
        /// </summary>
        protected internal override void OnSuccess(T result)
        {
            Interlocked.Increment(ref _onSuccessCallCount);

            if (_exceptionOnSuccess != null)
            {
                throw _exceptionOnSuccess;
            }
        }

        /// <summary>
        /// Called if exception occurred during computation.
        /// <param name="e"></param>
        /// </summary>
        protected internal override void OnFailure(Exception e)
        {
            Interlocked.Increment(ref _onFailureCallCount);
        }

        /// <summary>
        /// Called when the runnable is cancelled.
        /// </summary>
        protected internal override void OnCancellation()
        {
            Interlocked.Increment(ref _onCancellationCallCount);
        }

        /// <summary>
        /// Called after OnSuccess callback completes in order to dispose the result.
        /// <param name="result"></param>
        /// </summary>
        protected internal override void DisposeResult(T result)
        {
            Interlocked.Increment(ref _disposeResultCallCount);
            CallbackResult = result;
        }

        /// <summary>
        /// Gets the result of the runnable
        /// </summary>
        protected internal override Task<T> GetResult()
        {
            Interlocked.Increment(ref _getResultCallCount);

            if (_exceptionGetResult != null)
            {
                throw _exceptionGetResult;
            }

            return Task.FromResult(_result);
        }

        internal int OnSuccessCallCount
        {
            get
            {
                return Volatile.Read(ref _onSuccessCallCount);
            }
            set
            {
                Interlocked.Exchange(ref _onSuccessCallCount, value);
            }
        }

        internal int OnFailureCallCount
        {
            get
            {
                return Volatile.Read(ref _onFailureCallCount);
            }
            set
            {
                Interlocked.Exchange(ref _onFailureCallCount, value);
            }
        }

        internal int OnCancellationCallCount
        {
            get
            {
                return Volatile.Read(ref _onCancellationCallCount);
            }
            set
            {
                Interlocked.Exchange(ref _onCancellationCallCount, value);
            }
        }

        internal int DisposeResultCallCount
        {
            get
            {
                return Volatile.Read(ref _disposeResultCallCount);
            }
            set
            {
                Interlocked.Exchange(ref _disposeResultCallCount, value);
            }
        }

        internal int GetResultCallCount
        {
            get
            {
                return Volatile.Read(ref _getResultCallCount);
            }
            set
            {
                Interlocked.Exchange(ref _getResultCallCount, value);
            }
        }

        /// <summary>
        /// Sets the mock result
        /// </summary>
        internal void SetResult(T result)
        {
            _result = result;
        }

        internal void SetInternalState(int state)
        {
            Interlocked.Exchange(ref _state, state);
        }

        internal void ThrowExceptionOnSuccess(Exception exception)
        {
            _exceptionOnSuccess = exception;
        }

        internal void ThrowExceptionGetResult(Exception exception)
        {
            _exceptionGetResult = exception;
        }

        internal T CallbackResult { get; set; }
    }
}
