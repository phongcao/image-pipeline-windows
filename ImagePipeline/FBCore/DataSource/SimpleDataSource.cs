using FBCore.Common.Internal;
using System;

namespace FBCore.DataSource
{
    /// <summary>
    /// Settable <see cref="IDataSource{T}"/>.
    /// </summary>
    public class SimpleDataSource<T> : AbstractDataSource<T>
    {
        private SimpleDataSource()
        {
        }

        /// <summary>
        /// Creates a new <see cref="SimpleDataSource{T}"/>.
        /// </summary>
        public static SimpleDataSource<T> Create()
        {
            return new SimpleDataSource<T>();
        }

        /// <summary>
        /// Sets the result to <code>value</code>.
        ///
        /// <para />This method will return <code>true</code> if the value
        /// was successfully set, or <code>false</code> if the data source
        /// has already been set, failed or closed.
        ///
        /// <para />If the value was successfully set and <code>isLast</code>
        /// is <code>true</code>, state of the data source will be set to
        /// <see cref="AbstractDataSource{T}.DataSourceStatus.SUCCESS"/>.
        ///
        /// <para />This will also notify the subscribers if the value was
        /// successfully set.
        /// </summary>
        /// <param name="value">The value to be set.</param>
        /// <param name="isLast">Whether or not the value is last.</param>
        /// <returns>true if the value was successfully set.</returns>
        public override bool SetResult(T value, bool isLast)
        {
            return base.SetResult(Preconditions.CheckNotNull(value), isLast);
        }

        /// <summary>
        /// Sets the value as the last result.
        /// </summary>
        public bool SetResult(T value)
        {
            return base.SetResult(Preconditions.CheckNotNull(value), /*isLast*/ true);
        }

        /// <summary>
        /// Sets the failure.
        ///
        /// <para />This method will return <code>true</code> if the failure
        /// was successfully set, or <code>false</code> if the data source
        /// has already been set, failed or closed.
        ///
        /// <para />If the failure was successfully set, state of the data
        /// source will be set to
        /// <see cref="AbstractDataSource{T}.DataSourceStatus.FAILURE"/>.
        ///
        /// <para /> This will also notify the subscribers if the failure
        /// was successfully set.
        /// </summary>
        /// <param name="failure">The failure cause to be set.</param>
        /// <returns>true if the failure was successfully set.</returns>
        public override bool SetFailure(Exception failure)
        {
            return base.SetFailure(Preconditions.CheckNotNull(failure));
        }

        /// <summary>
        /// Sets the progress.
        /// </summary>
        /// <param name="progress">
        /// The progress in range [0, 1] to be set.
        /// </param>
        /// <returns>
        /// true if the progress was successfully set.
        /// </returns>
        public override bool SetProgress(float progress)
        {
            return base.SetProgress(progress);
        }
    }
}
