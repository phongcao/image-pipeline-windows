using System.Threading.Tasks;

namespace FBCore.DataSource
{
    /// <summary>
    /// Base implementation of <see cref="IDataSubscriber{T}"/> that
    /// ensures that the data source is closed when the subscriber
    /// has finished with it.
    /// <para />
    /// Sample usage:
    /// 
    /// <code>
    /// imagePipeline.IsInDiskCache(
    ///     uri,
    ///     new BaseBooleanSubscriberImpl(
    ///         (isFound) => 
    ///         {
    ///             // caller's code here
    ///         }
    ///     );
    /// </code>
    /// 
    /// </summary>
    public abstract class BaseBooleanSubscriber : IDataSubscriber<bool>
    {
        /// <summary>
        /// Called whenever a new value is ready to be retrieved from
        /// the IDataSource.
        ///
        /// <para />To retrieve the new value, call
        /// <code>dataSource.GetResult()</code>.
        ///
        /// <para />To determine if the new value is the last, use 
        /// <code>dataSource.IsFinished</code>.
        /// </summary>
        /// <param name="dataSource">The data source.</param>
        public async Task OnNewResult(IDataSource<bool> dataSource)
        {
            try
            {
                await OnNewResultImpl(dataSource.GetResult()).ConfigureAwait(false);
            }
            finally
            {
                dataSource.Close();
            }
        }

        /// <summary>
        /// Called whenever an error occurs inside of the pipeline.
        ///
        /// <para />No further results will be produced after this
        /// method is called.
        ///
        /// <para />The throwable resulting from the failure can be
        /// obtained using <code>dataSource.GetFailureCause</code>.
        /// </summary>
        /// <param name="dataSource">The data source.</param>
        public void OnFailure(IDataSource<bool> dataSource)
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
        /// Called whenever the request is cancelled (a request being
        /// cancelled means that is was closed before it finished).
        ///
        /// <para />No further results will be produced after this
        /// method is called.
        /// </summary>
        /// <param name="dataSource">The data source.</param>
        public void OnCancellation(IDataSource<bool> dataSource)
        {
        }

        /// <summary>
        /// Called when the progress updates.
        /// </summary>
        /// <param name="dataSource">The data source.</param>
        public void OnProgressUpdate(IDataSource<bool> dataSource)
        {
        }

        /// <summary>
        /// Implementation for OnNewResult.
        /// </summary>
        public abstract Task OnNewResultImpl(bool isFoundInDisk);

        /// <summary>
        /// Implementation for OnFailure.
        /// </summary>
        public abstract void OnFailureImpl(IDataSource<bool> dataSource);
    }
}
