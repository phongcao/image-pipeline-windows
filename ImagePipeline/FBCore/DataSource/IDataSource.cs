using FBCore.Concurrency;
using System;

namespace FBCore.DataSource
{
    /// <summary>
    /// An alternative to Java Futures for the image pipeline.
    ///
    /// <para />Unlike Futures, IDataSource can issue a series of results,
    /// rather than just one. A prime example is decoding progressiveimages,
    /// which have a series of intermediate results before the final one.
    ///
    /// <para />IDataSource MUST be closed (Close() is called on the
    /// IDataSource) else resources may leak.
    ///
    /// </summary>
    public interface IDataSource<T>
    {
        /// <summary>
        /// Checks if the data source is closed.
        /// </summary>
        /// <returns>
        /// true if the data source is closed, false otherwise.
        /// </returns>
        bool IsClosed();

        /// <summary>
        /// The most recent result of the asynchronous computation.
        ///
        /// <para />The caller gains ownership of the object and is
        /// responsible for releasing it.
        /// Note that subsequent calls to GetResult might give
        /// different results. Later results should be considered to
        /// be of higher quality.
        ///
        /// <para />This method will return null in the following cases:
        ///     1. When the IDataSource does not have a result
        ///     (<code> HasResult</code> returns false).
        ///     2. When the last result produced was null.
        /// </summary>
        /// <returns>Current best result.</returns>
        T GetResult();

        /// <summary>
        /// Checks if any result (possibly of lower quality) is available
        /// right now.
        /// </summary>
        /// <returns>
        /// true if any result (possibly of lower quality) is available
        /// right now, false otherwise.
        /// </returns>
        bool HasResult();

        /// <summary>
        /// Checks if request is finished.
        /// </summary>
        /// <returns>
        /// true if request is finished, false otherwise.
        /// </returns>
        bool IsFinished();

        /// <summary>
        /// Checks if request finished due to error.
        /// </summary>
        /// <returns>
        /// true if request finished due to error.
        /// </returns>
        bool HasFailed();

        /// <summary>
        /// Gets the failure cause if the source has failed.
        /// </summary>
        /// <returns>
        /// Failure cause if the source has failed, else null.
        /// </returns>
        Exception GetFailureCause();

        /// <summary>
        /// Gets the progress.
        /// </summary>
        /// <returns>Progress in range [0, 1].</returns>
        float GetProgress();

        /// <summary>
        /// Cancels the ongoing request and releases all associated
        /// resources.
        ///
        /// <para />Subsequent calls to <see cref="GetResult"/>
        /// will return null.
        /// </summary>
        /// <returns>
        /// true if the data source is closed for the first time.
        /// </returns>
        bool Close();

        /// <summary>
        /// Subscribe for notifications whenever the state of the
        /// IDataSource changes.
        ///
        /// <para />All changes will be observed on the provided
        /// executor.
        /// </summary>
        void Subscribe(
            IDataSubscriber<T> dataSubscriber,
            IExecutorService executor);
    }
}
