using System.Threading.Tasks;

namespace FBCore.DataSource
{
    /// <summary>
    /// Subscribes to IDataSource{T}.
    /// </summary>
    public interface IDataSubscriber<T>
    {
        /// <summary>
        /// Called whenever a new value is ready to be retrieved from the DataSource.
        ///
        /// <para />To retrieve the new value, call <code> dataSource.GetResult()</code>.
        ///
        /// <para />To determine if the new value is the last, use <code> dataSource.IsFinished</code>.
        ///
        /// <param name="dataSource"></param>
        /// </summary>
        Task OnNewResult(IDataSource<T> dataSource);

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
        void OnFailure(IDataSource<T> dataSource);

        /// <summary>
        /// Called whenever the request is cancelled (a request being cancelled means that is was closed
        /// before it finished).
        ///
        /// <para />No further results will be produced after this method is called.
        ///
        /// <param name="dataSource"></param>
        /// </summary>
        void OnCancellation(IDataSource<T> dataSource);

        /// <summary>
        /// Called when the progress updates.
        ///
        /// <param name="dataSource"></param>
        /// </summary>
        void OnProgressUpdate(IDataSource<T> dataSource);
    }
}
