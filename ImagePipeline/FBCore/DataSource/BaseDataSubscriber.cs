namespace FBCore.DataSource
{
    /// <summary>
    /// Base implementation of <see cref="IDataSubscriber{T}"/> that ensures that 
    /// the data source is closed when the subscriber has finished with it.
    /// <para />
    /// Sample usage:
    /// 
    /// <code>
    /// dataSource.Subscribe(
    ///   new BaseDataSubscriberImpl(
    ///     // OnNewResultImpl
    ///     (dataSource) =>
    ///     {
    ///         // Store image ref to be released later.
    ///         _closeableImageRef = dataSource.GetResult();
    ///     
    ///         // Use the image.
    ///         UpdateImage(_closeableImageRef);
    ///     
    ///         // No need to do any cleanup of the data source.
    ///     },
    ///     // OnFailureImpl
    ///     (_) =>
    ///     {
    ///         // No cleanup of the data source required here.
    ///     });
    /// </code>
    /// 
    /// </summary>
    public abstract class BaseDataSubscriber<T> : IDataSubscriber<T>
    {
        /// <summary>
        /// Test-only variables
        ///
        /// <para /><b>DO NOT USE in application code.</b>
        /// </summary>
        private int _onNewResultCallCount;
        private int _onFailureCallCount;
        private int _onCancellationCallCount;
        private int _onProgressUpdateCallCount;
        private IDataSource<T> _dataSource;

        /// <summary>
        /// Called whenever a new value is ready to be retrieved from the DataSource.
        ///
        /// <para />To retrieve the new value, call <code> dataSource.GetResult()</code>.
        ///
        /// <para />To determine if the new value is the last, use <code> dataSource.IsFinished</code>.
        ///
        /// <param name="dataSource"></param>
        /// </summary>
        public void OnNewResult(IDataSource<T> dataSource)
        {
            // For unit test
            ++_onNewResultCallCount;
            _dataSource = dataSource;

            // IsFinished should be checked before calling OnNewResultImpl(), otherwise
            // there would be a race condition: the final data source result might be ready before
            // we call IsFinished here, which would lead to the loss of the final result
            // (because of an early dataSource.Close() call).
            bool shouldClose = dataSource.IsFinished;
            try
            {
                OnNewResultImpl(dataSource);
            }
            finally
            {
                if (shouldClose)
                {
                    dataSource.Close();
                }
            }
        }

        /// <summary>
        /// Called whenever an error occurs inside of the pipeline.
        ///
        /// <para />No further results will be produced after this method is called.
        ///
        /// <para />The throwable resulting from the failure can be obtained using
        /// <code> dataSource.GetFailureCause</code>.
        ///
        /// <param name="dataSource"></param>
        /// </summary>
        public void OnFailure(IDataSource<T> dataSource)
        {
            // For unit test
            ++_onFailureCallCount;
            _dataSource = dataSource;

            try
            {
                OnFailureImpl(dataSource);
            }
            finally
            {
                dataSource.Close();
            }
        }

        /// <summary>
        /// Called whenever the request is cancelled (a request being cancelled means that is was closed
        /// before it finished).
        ///
        /// <para />No further results will be produced after this method is called.
        ///
        /// <param name="dataSource"></param>
        /// </summary>
        public void OnCancellation(IDataSource<T> dataSource)
        {
            // For unit test
            ++_onCancellationCallCount;
            _dataSource = dataSource;
        }

        /// <summary>
        /// Called when the progress updates.
        ///
        /// <param name="dataSource"></param>
        /// </summary>
        public void OnProgressUpdate(IDataSource<T> dataSource)
        {
            // For unit test
            ++_onProgressUpdateCallCount;
            _dataSource = dataSource;
        }

        /// <summary>
        /// Implementation for OnNewResult
        /// </summary>
        /// <param name="dataSource"></param>
        public abstract void OnNewResultImpl(IDataSource<T> dataSource);

        /// <summary>
        /// Implementation for OnFailure
        /// </summary>
        /// <param name="dataSource"></param>
        public abstract void OnFailureImpl(IDataSource<T> dataSource);

        /// <summary>
        /// For unit test
        /// </summary>
        internal int OnNewResultCallCount
        {
            get
            {
                return _onNewResultCallCount;
            }
        }

        /// <summary>
        /// For unit test
        /// </summary>
        internal int OnFailureCallCount
        {
            get
            {
                return _onFailureCallCount;
            }
        }

        /// <summary>
        /// For unit test
        /// </summary>
        internal int OnCancellationCallCount
        {
            get
            {
                return _onCancellationCallCount;
            }
        }

        /// <summary>
        /// For unit test
        /// </summary>
        internal IDataSource<T> DataSource
        {
            get
            {
                return _dataSource;
            }
        }

        /// <summary>
        /// For unit test
        /// </summary>
        internal bool HasZeroInteractions
        {
            get
            {
                return (_onNewResultCallCount == 0) &&
                    (_onFailureCallCount == 0) &&
                    (_onCancellationCallCount == 0) &&
                    (_onProgressUpdateCallCount == 0);
            }
        }

        /// <summary>
        /// For unit test
        /// </summary>
        internal void Reset()
        {
            _onNewResultCallCount = 0;
            _onFailureCallCount = 0;
            _onCancellationCallCount = 0;
            _onProgressUpdateCallCount = 0;
            _dataSource = null;
        }
    }
}
