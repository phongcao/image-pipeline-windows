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
        /// Called whenever a new value is ready to be retrieved from the IDataSource.
        ///
        /// <para />To retrieve the new value, call <code> dataSource.GetResult()</code>.
        ///
        /// <para />To determine if the new value is the last, use 
        /// <code> dataSource.IsFinished</code>.
        ///
        /// <param name="dataSource"></param>
        /// </summary>
        public void OnNewResult(IDataSource<T> dataSource)
        {
            // IsFinished should be checked before calling OnNewResultImpl(), otherwise
            // there would be a race condition: the final data source result might be ready before
            // we call IsFinished here, which would lead to the loss of the final result
            // (because of an early dataSource.Dipose() call).
            bool shouldClose = dataSource.IsFinished();

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
        /// Called whenever the request is cancelled (a request being cancelled means that is 
        /// was closed before it finished).
        ///
        /// <para />No further results will be produced after this method is called.
        ///
        /// <param name="dataSource"></param>
        /// </summary>
        public void OnCancellation(IDataSource<T> dataSource)
        {
        }

        /// <summary>
        /// Called when the progress updates.
        ///
        /// <param name="dataSource"></param>
        /// </summary>
        public void OnProgressUpdate(IDataSource<T> dataSource)
        {
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
    }
}
