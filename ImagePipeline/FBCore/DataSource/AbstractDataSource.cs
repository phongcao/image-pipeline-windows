using FBCore.Common.Internal;
using FBCore.Concurrency;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FBCore.DataSource
{
    /// <summary>
    /// An abstract implementation of <see cref="IDataSource{T}"/> interface.
    ///
    /// <para /> It is highly recommended that other data sources extend this class as it takes care of the
    /// state, as well as of notifying listeners when the state changes.
    ///
    /// <para /> Subclasses should override <see cref="CloseResult"/> if results need clean up
    /// </summary>
    public abstract class AbstractDataSource<T> : IDataSource<T>
    {
        private readonly object _gate = new object();

        /// <summary>
        /// Describes state of data source
        /// </summary>
        private enum DataSourceStatus
        {
            // data source has not finished yet
            IN_PROGRESS,

            // data source has finished with success
            SUCCESS,

            // data source has finished with failure
            FAILURE,
        }

        private DataSourceStatus _dataSourceStatus;

        private bool _isClosed;

        private T _result = default(T);

        private Exception _failure = default(Exception);

        private float _progress = 0;

        private readonly BlockingCollection<KeyValuePair<IDataSubscriber<T>, IExecutorService>> _subscribers;

        /// <summary>
        /// Instantiates the <see cref="AbstractDataSource{T}"/>
        /// </summary>
        protected AbstractDataSource()
        {
            _isClosed = false;
            _dataSourceStatus = DataSourceStatus.IN_PROGRESS;
            _subscribers = new BlockingCollection<KeyValuePair<IDataSubscriber<T>, IExecutorService>>();
        }

        /// <summary>
        /// @return true if the data source is closed, false otherwise
        /// </summary>
        public virtual bool IsClosed()
        {
            lock (_gate)
            {
                return _isClosed;
            }
        }

        /// <summary>
        /// @return true if request is finished, false otherwise
        /// </summary>
        public virtual bool IsFinished()
        {
            lock (_gate)
            {
                return _dataSourceStatus != DataSourceStatus.IN_PROGRESS;
            }
        }

        /// <summary>
        /// @return true if any result (possibly of lower quality) is available right now, false otherwise
        /// </summary>
        public virtual bool HasResult()
        {
            lock (_gate)
            {
                return _result != null;
            }
        }

        /// <summary>
        /// The most recent result of the asynchronous computation.
        ///
        /// <para />The caller gains ownership of the object and is responsible for releasing it.
        /// Note that subsequent calls to GetResult might give different results. Later results should be
        /// considered to be of higher quality.
        ///
        /// <para />This method will return null in the following cases:
        /// when the DataSource does not have a result (<code> HasResult</code> returns false).
        /// when the last result produced was null.
        /// @return current best result
        /// </summary>
        public virtual T GetResult()
        {
            lock (_gate)
            {
                return _result;
            }
        }

        /// <summary>
        /// @return true if request finished due to error
        /// </summary>
        public virtual bool HasFailed()
        {
            lock (_gate)
            {
                return _dataSourceStatus == DataSourceStatus.FAILURE;
            }
        }

        /// <summary>
        /// @return failure cause if the source has failed, else null
        /// </summary>
        public virtual Exception GetFailureCause()
        {
            lock (_gate)
            {
                return _failure;
            }
        }

        /// <summary>
        /// @return progress in range [0, 1]
        /// </summary>
        public virtual float GetProgress()
        {
            lock (_gate)
            {
                return _progress;
            }
        }

        /// <summary>
        /// Cancels the ongoing request and releases all associated resources.
        ///
        /// <para />Subsequent calls to <see cref="GetResult"/> will return null.
        /// @return true if the data source is closed for the first time
        /// </summary>
        public virtual bool Close()
        {
            T resultToClose;

            lock (_gate)
            { 
                if (_isClosed)
                {
                    return false;
                }

                _isClosed = true;
                resultToClose = _result;
                _result = default(T);
            }

            if (resultToClose != null)
            {
                CloseResult(resultToClose);
            }

            if (!IsFinished())
            {
                NotifyDataSubscribers();
            }

            lock (_gate)
            {
                _subscribers.Dispose();
            }

            return true;
        }

        /// <summary>
        /// Subclasses should override this method to close the result that is not needed anymore.
        ///
        /// <para /> This method is called in two cases:
        /// 1. to clear the result when data source gets closed
        /// 2. to clear the previous result when a new result is set
        /// </summary>
        public virtual void CloseResult(T result)
        {
            // default implementation does nothing
        }

        /// <summary>
        /// Subscribe for notifications whenever the state of the DataSource changes.
        ///
        /// <para />All changes will be observed on the provided executor.
        /// <param name="dataSubscriber"></param>
        /// <param name="executor"></param>
        /// </summary>
        public virtual void Subscribe(IDataSubscriber<T> dataSubscriber, IExecutorService executor)
        {
            Preconditions.CheckNotNull(dataSubscriber);
            Preconditions.CheckNotNull(executor);
            bool shouldNotify;

            lock (_gate)
            {
                if (_isClosed)
                {
                    return;
                }

                if (_dataSourceStatus == DataSourceStatus.IN_PROGRESS)
                {
                    _subscribers.Add(new KeyValuePair<IDataSubscriber<T>, IExecutorService>(dataSubscriber, executor));
                }

                shouldNotify = HasResult() || IsFinished() || WasCancelled();
            }

            if (shouldNotify)
            {
                NotifyDataSubscriber(dataSubscriber, executor, HasFailed(), WasCancelled());
            }
        }

        private void NotifyDataSubscribers()
        {
            bool isFailure = HasFailed();
            bool isCancellation = WasCancelled();
            foreach (var pair in _subscribers)
            {
                NotifyDataSubscriber(pair.Key, pair.Value, isFailure, isCancellation);
            }
        }

        private void NotifyDataSubscriber(
            IDataSubscriber<T> dataSubscriber,
            IExecutorService executor,
            bool isFailure,
            bool isCancellation)
        {
            executor.Execute(() =>
            {
                if (isFailure)
                {
                    dataSubscriber.OnFailure(this);
                }
                else if (isCancellation)
                {
                    dataSubscriber.OnCancellation(this);
                }
                else
                {
                    dataSubscriber.OnNewResult(this);
                }
            });
        }

        private bool WasCancelled()
        {
            return IsClosed() && !IsFinished();
        }

        /// <summary>
        /// Subclasses should invoke this method to set the result to <code> value</code>.
        ///
        /// <para /> This method will return <code> true</code> if the value was successfully set, or
        /// <code> false</code> if the data source has already been set, failed or closed.
        ///
        /// <para /> If the value was successfully set and <code> isLast</code> is <code> true</code>, state of the
        /// data source will be set to <see cref="DataSourceStatus.SUCCESS"/>.
        ///
        /// <para /> <see cref="CloseResult"/> will be called for the previous result if the new value was
        /// successfully set, OR for the new result otherwise.
        ///
        /// <para /> This will also notify the subscribers if the value was successfully set.
        ///
        /// <para /> Do NOT call this method from a synchronized block as it invokes external code of the
        /// subscribers.
        ///
        /// <param name="value">the value that was the result of the task.</param>
        /// <param name="isLast">whether or not the value is last.</param>
        /// @return true if the value was successfully set.
        /// </summary>
        public virtual bool SetResult(T value, bool isLast)
        {
            bool result = SetResultInternal(value, isLast);
            if (result)
            {
                NotifyDataSubscribers();
            }

            return result;
        }

        /// <summary>
        /// Subclasses should invoke this method to set the failure.
        ///
        /// <para /> This method will return <code> true</code> if the failure was successfully set, or
        /// <code> false</code> if the data source has already been set, failed or closed.
        ///
        /// <para /> If the failure was successfully set, state of the data source will be set to
        /// <see cref="DataSourceStatus.FAILURE"/>.
        ///
        /// <para /> This will also notify the subscribers if the failure was successfully set.
        ///
        /// <para /> Do NOT call this method from a synchronized block as it invokes external code of the
        /// subscribers.
        ///
        /// <param name="throwable">the failure cause to be set.</param>
        /// @return true if the failure was successfully set.
        /// </summary>
        public virtual bool SetFailure(Exception throwable)
        {
            bool result = SetFailureInternal(throwable);
            if (result)
            {
                NotifyDataSubscribers();
            }

            return result;
        }

        /// <summary>
        /// Subclasses should invoke this method to set the progress.
        ///
        /// <para /> This method will return <code> true</code> if the progress was successfully set, or
        /// <code> false</code> if the data source has already been set, failed or closed.
        ///
        /// <para /> This will also notify the subscribers if the progress was successfully set.
        ///
        /// <para /> Do NOT call this method from a synchronized block as it invokes external code of the
        /// subscribers.
        ///
        /// <param name="progress">the progress in range [0, 1] to be set.</param>
        /// @return true if the progress was successfully set.
        /// </summary>
        public virtual bool SetProgress(float progress)
        {
            bool result = SetProgressInternal(progress);
            if (result)
            {
                NotifyProgressUpdate();
            }

            return result;
        }

        private bool SetResultInternal(T value, bool isLast)
        {
            T resultToClose = default(T);

            try
            {
                lock (_gate)
                {
                    if (_isClosed || _dataSourceStatus != DataSourceStatus.IN_PROGRESS)
                    {
                        resultToClose = value;
                        return false;
                    }
                    else
                    {
                        if (isLast)
                        {
                            _dataSourceStatus = DataSourceStatus.SUCCESS;
                            _progress = 1;
                        }

                        if (!Equals(_result, value))
                        {
                            resultToClose = _result;
                            _result = value;
                        }

                        return true;
                    }
                }
            }
            finally
            {
                if (resultToClose != null)
                {
                    CloseResult(resultToClose);
                }
            }
        }

        private bool SetFailureInternal(Exception failure)
        {
            lock (_gate)
            {
                if (_isClosed || _dataSourceStatus != DataSourceStatus.IN_PROGRESS)
                {
                    return false;
                }
                else
                {
                    _dataSourceStatus = DataSourceStatus.FAILURE;
                    _failure = failure;
                    return true;
                }
            }
        }

        private bool SetProgressInternal(float progress)
        {
            lock (_gate)
            {
                if (_isClosed || _dataSourceStatus != DataSourceStatus.IN_PROGRESS)
                {
                    return false;
                }
                else if (progress < _progress)
                {
                    return false;
                }
                else
                {
                    _progress = progress;
                    return true;
                }
            }
        }

        /// <summary>
        /// Notifies progress update
        /// </summary>
        protected void NotifyProgressUpdate()
        {
            foreach (var pair in _subscribers)
            {
                IDataSubscriber<T> subscriber = pair.Key;
                IExecutorService executor = pair.Value;
                executor.Execute(() =>
                {
                    subscriber.OnProgressUpdate(this);
                });
            }
        }
    }
}
