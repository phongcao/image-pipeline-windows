using System;
using System.Collections.Generic;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Provides custom implementation for <see cref="StatefulProducerRunnable{T}"/>
    /// </summary>
    public class StatefulProducerRunnableImpl<T> : StatefulProducerRunnable<T>
    {
        private Action<T> _onSuccessFunc;
        private Action<Exception> _onFailureFunc;
        private Action _onCancellation;
        private Func<T, IDictionary<string, string>> _getExtraMapOnSuccessFunc;
        private Func<Exception, IDictionary<string, string>> _getExtraMapOnFailureFunc;
        private Func<IDictionary<string, string>> _getExtraMapOnCancellationFunc;
        private Action<T> _disposeResultFunc;
        private Func<T> _getResultFunc;

        /// <summary>
        /// Instantiates the <see cref="StatefulProducerRunnableImpl{T}"/>
        /// </summary>
        public StatefulProducerRunnableImpl(
            IConsumer<T> consumer,
            IProducerListener producerListener,
            string producerName,
            string requestId,
            Action<T> onSuccessFunc,
            Action<Exception> onFailureFunc,
            Action onCancellation,
            Func<T, IDictionary<string, string>> getExtraMapOnSuccessFunc,
            Func<Exception, IDictionary<string, string>> getExtraMapOnFailureFunc,
            Func<IDictionary<string, string>> getExtraMapOnCancellationFunc,
            Action<T> disposeResultFunc,
            Func<T> getResultFunc) : base(
                consumer,
                producerListener,
                producerName,
                requestId)
        {
            _onSuccessFunc = onSuccessFunc;
            _onFailureFunc = onFailureFunc;
            _onCancellation = onCancellation;
            _getExtraMapOnSuccessFunc = getExtraMapOnSuccessFunc;
            _getExtraMapOnFailureFunc = getExtraMapOnFailureFunc;
            _getExtraMapOnCancellationFunc = getExtraMapOnCancellationFunc;
            _disposeResultFunc = disposeResultFunc;
            _getResultFunc = getResultFunc;
        }

        /// <summary>
        /// Called after computing result successfully.
        /// <param name="result"></param>
        /// </summary>
        protected override void OnSuccess(T result)
        {
            if (_onSuccessFunc == null)
            {
                base.OnSuccess(result);
            }
            else
            {
                _onSuccessFunc(result);
            }
        }

        /// <summary>
        /// Called if exception occurred during computation.
        /// <param name="e"></param>
        /// </summary>
        protected override void OnFailure(Exception e)
        {
            if (_onSuccessFunc == null)
            {
                base.OnFailure(e);
            }
            else
            {
                _onFailureFunc(e);
            }
        }

        /// <summary>
        /// Called when the runnable is cancelled.
        /// </summary>
        protected override void OnCancellation()
        {
            if (_onCancellation == null)
            {
                base.OnCancellation();
            }
            else
            {
                _onCancellation();
            }
        }

        /// <summary>
        /// Create extra map for result
        /// </summary>
        protected override IDictionary<string, string> GetExtraMapOnSuccess(T result)
        {
            if (_getExtraMapOnSuccessFunc == null)
            {
                return base.GetExtraMapOnSuccess(result);
            }
            else
            {
                return _getExtraMapOnSuccessFunc(result);
            }
        }

        /// <summary>
        /// Create extra map for exception
        /// </summary>
        protected override IDictionary<string, string> GetExtraMapOnFailure(Exception exception)
        {
            if (_getExtraMapOnFailureFunc == null)
            {
                return base.GetExtraMapOnFailure(exception);
            }
            else
            {
                return _getExtraMapOnFailureFunc(exception);
            }
        }

        /// <summary>
        /// Create extra map for cancellation
        /// </summary>
        protected override IDictionary<string, string> GetExtraMapOnCancellation()
        {
            if (_getExtraMapOnCancellationFunc == null)
            {
                return base.GetExtraMapOnCancellation();
            }
            else
            {
                return _getExtraMapOnCancellationFunc();
            }
        }

        /// <summary>
        /// Called after OnSuccess callback completes in order to dispose the result.
        /// <param name="result"></param>
        /// </summary>
        protected override void DisposeResult(T result)
        {
            _disposeResultFunc?.Invoke(result);
        }

        /// <summary>
        /// Gets the result of the runnable
        /// </summary>
        protected override T GetResult()
        {
            return _getResultFunc();
        }
    }
}
