using System;
using System.Threading;
using System.Threading.Tasks;

namespace FBCore.Concurrency
{
    /// <summary>
    /// Abstraction for computation.
    ///
    /// <para /> Computation expressed as StatefulRunnable can be cancelled, but only if it 
    /// has not started yet.
    ///
    /// <para /> For better decoupling of the code computing the result and the code that 
    /// handles it, 4 separate methods are provided: GetResult, OnSuccess, OnFailure and 
    /// OnCancellation.
    ///
    /// <para /> This runnable can be run only once. Subsequent calls to run method won't 
    /// have any effect.
    /// </summary>
    public abstract class StatefulRunnable<T>
    {
        /// <summary>
        /// State Created
        /// </summary>
        internal const int STATE_CREATED = 0;

        /// <summary>
        /// State Started
        /// </summary>
        internal const int STATE_STARTED = 1;

        /// <summary>
        /// State Cancelled
        /// </summary>
        internal const int STATE_CANCELLED = 2;

        /// <summary>
        /// State Finished
        /// </summary>
        internal const int STATE_FINISHED = 3;

        /// <summary>
        /// State Failed
        /// </summary>
        internal const int STATE_FAILED = 4;

        /// <summary>
        /// State
        /// </summary>
        protected int _state;

        /// <summary>
        /// Runnable
        /// </summary>
        private Func<Task> _runnable;

        /// <summary>
        /// Returns the runnable.
        /// </summary>
        public Func<Task> Runnable
        {
            get
            {
                return _runnable;
            }
        }

        /// <summary>
        /// Instantiates the <see cref="StatefulRunnable{T}"/>
        /// </summary>
        public StatefulRunnable()
        {
            _runnable = async () =>
            {
                if (Interlocked.CompareExchange(
                    ref _state, STATE_STARTED, STATE_CREATED) != STATE_CREATED)
                {
                    return;
                }

                T result;
                try
                {
                    result = await GetResult().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Interlocked.Exchange(ref _state, STATE_FAILED);
                    OnFailure(e);
                    return;
                }

                Interlocked.Exchange(ref _state, STATE_FINISHED);
                try
                {
                    OnSuccess(result);
                }
                finally
                {
                    DisposeResult(result);
                }
            };

            Interlocked.Exchange(ref _state, STATE_CREATED);
        }

        /// <summary>
        /// Cancelling the runnable
        /// </summary>
        public void Cancel()
        {
            if (Interlocked.CompareExchange(
                ref _state, STATE_CANCELLED, STATE_CREATED) == STATE_CREATED)
            {
                OnCancellation();
            }
        }

        /// <summary>
        /// Called after computing result successfully.
        /// <param name="result"></param>
        /// </summary>
        protected internal virtual void OnSuccess(T result) { }

        /// <summary>
        /// Called if exception occurred during computation.
        /// <param name="e"></param>
        /// </summary>
        protected internal virtual void OnFailure(Exception e) { }

        /// <summary>
        /// Called when the runnable is cancelled.
        /// </summary>
        protected internal virtual void OnCancellation() { }

        /// <summary>
        /// Called after OnSuccess callback completes in order to dispose the result.
        /// <param name="result"></param>
        /// </summary>
        protected internal virtual void DisposeResult(T result) { }

        /// <summary>
        /// Gets the result of the runnable.
        /// </summary>
        protected internal abstract Task<T> GetResult();
    }
}
