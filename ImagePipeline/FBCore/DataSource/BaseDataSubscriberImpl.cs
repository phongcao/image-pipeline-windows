using System;

namespace FBCore.DataSource
{
    /// <summary>
    /// Provides the custom implementation for <see cref="BaseDataSubscriber{T}"/>.
    /// </summary>
    public class BaseDataSubscriberImpl<T> : BaseDataSubscriber<T>
    {
        private Action<IDataSource<T>> _onNewResultImplFunc;
        private Action<IDataSource<T>> _onFailureImplFunc;

        /// <summary>
        /// Instantiates the <see cref="BaseDataSubscriberImpl{T}"/>.
        /// </summary>
        public BaseDataSubscriberImpl(
            Action<IDataSource<T>> onNewResultImplFunc,
            Action<IDataSource<T>> onFailureImplFunc)
        {
            _onNewResultImplFunc = onNewResultImplFunc;
            _onFailureImplFunc = onFailureImplFunc;
        }

        /// <summary>
        /// Implementation for OnNewResult.
        /// </summary>
        public override void OnNewResultImpl(IDataSource<T> dataSource)
        {
            _onNewResultImplFunc(dataSource);
        }

        /// <summary>
        /// Implementation for OnFailure.
        /// </summary>
        public override void OnFailureImpl(IDataSource<T> dataSource)
        {
            _onFailureImplFunc(dataSource);
        }
    }
}
