using FBCore.Common.References;
using FBCore.DataSource;
using System;

namespace ImagePipeline.Datasource
{
    /// <summary>
    /// A <see cref="IDataSource{T}"/> whose result may be set by a 
    /// <see cref="Set(CloseableReference{T})"/> or <see cref="SetException"/> 
    /// call. It may also be closed.
    ///
    /// <para />This data source has no intermediate results - calling 
    /// <see cref="Set(CloseableReference{T})"/> means that the data source is finished.
    /// </summary>
    public sealed class SettableDataSource<T> : AbstractDataSource<CloseableReference<T>>
    {
        private SettableDataSource()
        {
        }

        /// <summary>
        /// Creates a new <code> SettableDataSource</code>
        /// </summary>
        public static SettableDataSource<V> Create<V>()
        {
            return new SettableDataSource<V>();
        }

        /// <summary>
        /// Sets the value of this data source.
        ///
        /// <para /> This method will return <code> true</code> if the value was successfully set, or
        /// <code> false</code> if the data source has already been set, failed or closed.
        ///
        /// <para /> Passed CloseableReference is cloned, caller of this method still owns passed reference
        /// after the method returns.
        ///
        /// <param name="valueRef">closeable reference to the value the data source should hold.</param>
        /// @return true if the value was successfully set.
        /// </summary>
        public bool Set(CloseableReference<T> valueRef)
        {
            CloseableReference<T> clonedRef = CloseableReference<T>.CloneOrNull(valueRef);
            return SetResult(clonedRef, /* isLast */ true);
        }

        /// <summary>
        /// Sets the data source to having failed with the given exception.
        ///
        /// <para /> This method will return <code> true</code> if the exception was successfully set, or
        /// <code> false</code> if the data source has already been set, failed or closed.
        ///
        /// <param name="throwable">the exception the data source should hold.</param>
        /// @return true if the exception was successfully set.
        /// </summary>
        public bool SetException(Exception throwable)
        {
            return SetFailure(throwable);
        }

        /// <summary>
        /// Sets the progress.
        ///
        /// <param name="progress">the progress in range [0, 1] to be set.</param>
        /// @return true if the progress was successfully set.
        /// </summary>
        public override bool SetProgress(float progress)
        {
            return base.SetProgress(progress);
        }

        /// <summary>
        /// Gets the result if any, null otherwise.
        ///
        /// <para /> Value will be cloned and it's the caller's responsibility to close the returned value.
        /// </summary>
        public override CloseableReference<T> GetResult()
        {
            return CloseableReference<T>.CloneOrNull(base.GetResult());
        }

        /// <summary>
        /// This method is called in two cases:
        /// 1. to clear the result when data source gets closed
        /// 2. to clear the previous result when a new result is set
        /// </summary>
        public override void CloseResult(CloseableReference<T> result)
        {
            CloseableReference<T>.CloseSafely(result);
        }
    }
}
