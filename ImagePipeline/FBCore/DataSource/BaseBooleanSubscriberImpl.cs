using System;

namespace FBCore.DataSource
{
    /// <summary>
    /// Provides custom implementation for <see cref="BaseBooleanSubscriber"/> 
    /// </summary>
    public class BaseBooleanSubscriberImpl : BaseBooleanSubscriber
    {
        private Action<bool> _onNewResultImplFunc;
        private Action<IDataSource<bool>> _onFailureImplFunc;

        /// <summary>
        /// Instantiates the <see cref="BaseBooleanSubscriberImpl"/>
        /// </summary>
        /// <param name="onNewResultImplFunc"></param>
        /// <param name="onFailureImplFunc"></param>
        private BaseBooleanSubscriberImpl(
            Action<bool> onNewResultImplFunc,
            Action<IDataSource<bool>> onFailureImplFunc)
        {
            _onNewResultImplFunc = onNewResultImplFunc;
            _onFailureImplFunc = onFailureImplFunc;
        }

        /// <summary>
        /// Instantiates the <see cref="BaseBooleanSubscriberImpl"/>
        /// </summary>
        /// <param name="onNewResultImplFunc"></param>
        public BaseBooleanSubscriberImpl(Action<bool> onNewResultImplFunc) : this(
            onNewResultImplFunc,
            (_) => {})
        {
        }

        /// <summary>
        /// Implementation for OnNewResult
        /// </summary>
        /// <param name="isFoundInDisk"></param>
        public override void OnNewResultImpl(bool isFoundInDisk)
        {
            _onNewResultImplFunc(isFoundInDisk);
        }

        /// <summary>
        /// Implementation for OnFailure
        /// </summary>
        /// <param name="dataSource"></param>
        public override void OnFailureImpl(IDataSource<bool> dataSource)
        {
            _onFailureImplFunc(dataSource);
        }
    }
}
