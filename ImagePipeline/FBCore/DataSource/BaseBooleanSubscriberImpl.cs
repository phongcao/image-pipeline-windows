using System;
using System.Threading.Tasks;

namespace FBCore.DataSource
{
    /// <summary>
    /// Provides custom implementation for <see cref="BaseBooleanSubscriber"/>. 
    /// </summary>
    public class BaseBooleanSubscriberImpl : BaseBooleanSubscriber
    {
        private Func<bool, Task> _onNewResultImplFunc;
        private Action<IDataSource<bool>> _onFailureImplFunc;

        /// <summary>
        /// Instantiates the <see cref="BaseBooleanSubscriberImpl"/>.
        /// </summary>
        private BaseBooleanSubscriberImpl(
            Func<bool, Task> onNewResultImplFunc,
            Action<IDataSource<bool>> onFailureImplFunc)
        {
            _onNewResultImplFunc = onNewResultImplFunc;
            _onFailureImplFunc = onFailureImplFunc;
        }

        /// <summary>
        /// Instantiates the <see cref="BaseBooleanSubscriberImpl"/>.
        /// </summary>
        public BaseBooleanSubscriberImpl(Func<bool, Task> onNewResultImplFunc) : 
            this(onNewResultImplFunc, (_) => { })
        {
        }

        /// <summary>
        /// Implementation for OnNewResult.
        /// </summary>
        public override Task OnNewResultImpl(bool isFoundInDisk)
        {
            return _onNewResultImplFunc(isFoundInDisk);
        }

        /// <summary>
        /// Implementation for OnFailure.
        /// </summary>
        public override void OnFailureImpl(IDataSource<bool> dataSource)
        {
            _onFailureImplFunc(dataSource);
        }
    }
}
