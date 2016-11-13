using System;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Helper class for <see cref="BaseConsumer{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BaseConsumerHelper<T> : BaseConsumer<T>
    {
        private Action<T, bool> _onNewResultImplFunc;
        private Action<Exception> _onFailureImplFunc;
        private Action _onCancellationImplFunc;
        private Action<float> _onProgressUpdateImplFunc;

        /// <summary>
        /// Instantiates the <see cref="BaseConsumerHelper{T}"/>
        /// </summary>
        /// <param name="onNewResultImplFunc"></param>
        /// <param name="onFailureImplFunc"></param>
        /// <param name="onCancellationImplFunc"></param>
        /// <param name="onProgressUpdateImplFunc"></param>
        public BaseConsumerHelper(
            Action<T, bool> onNewResultImplFunc,
            Action<Exception> onFailureImplFunc,
            Action onCancellationImplFunc,
            Action<float> onProgressUpdateImplFunc)
        {
            _onNewResultImplFunc = onNewResultImplFunc;
            _onFailureImplFunc = onFailureImplFunc;
            _onCancellationImplFunc = onCancellationImplFunc;
            _onProgressUpdateImplFunc = onProgressUpdateImplFunc;
        }

        /// <summary>
        /// Called by OnNewResult, override this method instead.
        /// </summary>
        protected override void OnNewResultImpl(T newResult, bool isLast)
        {
            _onNewResultImplFunc(newResult, isLast);
        }

        /// <summary>
        /// Called by OnFailure, override this method instead
        /// </summary>
        protected override void OnFailureImpl(Exception error)
        {
            _onFailureImplFunc(error);
        }

        /// <summary>
        /// Called by OnCancellation, override this method instead
        /// </summary>
        protected override void OnCancellationImpl()
        {
            _onCancellationImplFunc();
        }

        /// <summary>
        /// Called when the progress updates
        /// </summary>
        protected override void OnProgressUpdateImpl(float progress)
        {
            if (_onProgressUpdateImplFunc == null)
            {
                base.OnProgressUpdateImpl(progress);
            }
            else
            {
                _onProgressUpdateImplFunc(progress);
            }
        }
    }
}
