using FBCore.Concurrency;
using System;

namespace FBCore.DataSource
{
    /// <summary>
    /// An alternative to Java Futures for the image pipeline.
    ///
    /// <para />Unlike Futures, DataSource can issue a series of results, rather than just one. A prime
    /// example is decoding progressive images, which have a series of intermediate results before the
    /// final one.
    ///
    /// <para />DataSources MUST be closed (Close() is called on the DataSource) else resources may leak.
    ///
    /// </summary>
    public interface IDataSource<T>
    {
        /// <summary>
        /// @return true if the data source is closed, false otherwise
        /// </summary>
        bool IsClosed();

        /// <summary>
        /// The most recent result of the asynchronous computation.
        ///
        /// <para />The caller gains ownership of the object and is responsible for releasing it.
        /// Note that subsequent calls to getResult might give different results. Later results should be
        /// considered to be of higher quality.
        ///
        /// <para />This method will return null in the following cases:
        /// when the DataSource does not have a result (<code> HasResult</code> returns false).
        /// when the last result produced was null.
        /// @return current best result
        /// </summary>
        T GetResult();

        /// <summary>
        /// @return true if any result (possibly of lower quality) is available right now, false otherwise
        /// </summary>
        bool HasResult();

        /// <summary>
        /// @return true if request is finished, false otherwise
        /// </summary>
        bool IsFinished();

        /// <summary>
        /// @return true if request finished due to error
        /// </summary>
        bool HasFailed();

        /// <summary>
        /// @return failure cause if the source has failed, else null
        /// </summary>
        Exception GetFailureCause();

        /// <summary>
        /// @return progress in range [0, 1]
        /// </summary>
        float GetProgress();

        /// <summary>
        /// Cancels the ongoing request and releases all associated resources.
        ///
        /// <para />Subsequent calls to <see cref="GetResult"/> will return null.
        /// @return true if the data source is closed for the first time
        /// </summary>
        bool Close();

        /// <summary>
        /// Subscribe for notifications whenever the state of the DataSource changes.
        ///
        /// <para />All changes will be observed on the provided executor.
        /// <param name="dataSubscriber"></param>
        /// <param name="executor"></param>
        /// </summary>
        void Subscribe(IDataSubscriber<T> dataSubscriber, IExecutorService executor);
    }
}
