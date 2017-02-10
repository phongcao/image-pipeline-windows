using System;
using System.Threading.Tasks;

namespace FBCore.DataSource
{
    /// <summary>
    /// Provides the custom implementation for <see cref="BaseDataSubscriber{T}"/>.
    /// </summary>
    public class BaseDataSubscriberImpl<T> : BaseDataSubscriber<T>
    {
        private Func<IDataSource<T>, Task> _onNewResultImplFunc;
        private Action<IDataSource<T>> _onFailureImplFunc;

        /// <summary>
        /// Instantiates the <see cref="BaseDataSubscriberImpl{T}"/>.
        /// </summary>
        public BaseDataSubscriberImpl(
            Func<IDataSource<T>, Task> onNewResultImplFunc,
            Action<IDataSource<T>> onFailureImplFunc)
        {
            _onNewResultImplFunc = onNewResultImplFunc;
            _onFailureImplFunc = onFailureImplFunc;
        }

        /// <summary>
        /// Implementation for OnNewResult.
        /// </summary>
        public override Task OnNewResultImpl(IDataSource<T> dataSource)
        {
            return _onNewResultImplFunc(dataSource);
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
