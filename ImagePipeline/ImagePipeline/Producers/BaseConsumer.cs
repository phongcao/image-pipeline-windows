using System;
using System.Diagnostics;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Base implementation of Consumer that implements error handling conforming to the
    /// Consumer's contract.
    ///
    /// <para /> This class also prevents execution of callbacks if one of final methods was called before:
    /// OnFinish(isLast = true), onFailure or onCancellation.
    ///
    /// <para /> All callbacks are executed within a synchronized block, so that clients can act as if all
    /// callbacks are called on single thread.
    ///
    /// </summary>
    public abstract class BaseConsumer<T> : IConsumer<T>
    {
        private readonly object _gate = new object();

        /// <summary>
        /// Set to true when OnNewResult(isLast = true), OnFailure or OnCancellation is called. Further
        /// calls to any of the 3 methods are not propagated
        /// </summary>
        private bool _isFinished;

        /// <summary>
        /// Instantiates the <see cref="BaseConsumer{T}"/>
        /// </summary>
        public BaseConsumer()
        {
            _isFinished = false;
        }

        /// <summary>
        /// Called by a producer whenever new data is produced. This method should not throw an exception.
        ///
        /// <para /> In case when result is closeable resource producer will close it after onNewResult returns.
        /// Consumer needs to make copy of it if the resource must be accessed after that. Fortunately,
        /// with CloseableReferences, that should not impose too much overhead.
        ///
        /// <param name="newResult"></param>
        /// <param name="isLast">true if newResult is the last result</param>
        /// </summary>
        public void OnNewResult(T newResult, bool isLast)
        {
            lock (_gate)
            {
                if (_isFinished)
                {
                    return;
                }

                _isFinished = isLast;

                try
                {
                    OnNewResultImpl(newResult, isLast);
                }
                catch (Exception e)
                {
                    OnUnhandledException(e);
                }
            }
        }

        /// <summary>
        /// Called by a producer whenever it terminates further work due to Throwable being thrown. This
        /// method should not throw an exception.
        /// </summary>
        public void OnFailure(Exception error)
        {
            lock (_gate)
            {
                if (_isFinished)
                {
                    return;
                }

                _isFinished = true;

                try
                {
                    OnFailureImpl(error);
                }
                catch (Exception e)
                {
                    OnUnhandledException(e);
                }
            }
        }

        /// <summary>
        /// Called by a producer whenever it is cancelled and won't produce any more results
        /// </summary>
        public void OnCancellation()
        {
            lock (_gate)
            {
                if (_isFinished)
                {
                    return;
                }

                _isFinished = true;

                try
                {
                    OnCancellationImpl();
                }
                catch (Exception e)
                {
                    OnUnhandledException(e);
                }
            }
        }

        /// <summary>
        /// Called when the progress updates.
        ///
        /// <param name="progress">in range [0, 1]</param>
        /// </summary>
        public void OnProgressUpdate(float progress)
        {
            lock (_gate)
            {
                if (_isFinished)
                {
                    return;
                }

                try
                {
                    OnProgressUpdateImpl(progress);
                }
                catch (Exception e)
                {
                    OnUnhandledException(e);
                }
            }
        }

        /// <summary>
        /// Called by OnNewResult, override this method instead.
        /// </summary>
        protected abstract void OnNewResultImpl(T newResult, bool isLast);

        /// <summary>
        /// Called by OnFailure, override this method instead
        /// </summary>
        protected abstract void OnFailureImpl(Exception error);

        /// <summary>
        /// Called by OnCancellation, override this method instead
        /// </summary>
        protected abstract void OnCancellationImpl();

        /// <summary>
        /// Called when the progress updates
        /// </summary>
        protected virtual void OnProgressUpdateImpl(float progress)
        {
        }

        /// <summary>
        /// Called whenever OnNewResultImpl or OnFailureImpl throw an exception
        /// </summary>
        protected void OnUnhandledException(Exception error)
        {
            Debug.WriteLine("Unhandled exception in ", GetType().Name);
        }
    }
}
