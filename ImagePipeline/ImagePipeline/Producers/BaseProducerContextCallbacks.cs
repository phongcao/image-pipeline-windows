using System;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Empty implementation of <see cref="IProducerContextCallbacks"/>.
    /// </summary>
    public class BaseProducerContextCallbacks : IProducerContextCallbacks
    {
        private Action _onCancellationRequestedFunc;
        private Action _onIsPrefetchChangedFunc;
        private Action _onIsIntermediateResultExpectedChangedFunc;
        private Action _onPriorityChangedFunc;

        /// <summary>
        /// Instantiates the <see cref="BaseProducerContextCallbacks"/>.
        /// </summary>
        public BaseProducerContextCallbacks(
            Action onCancellationRequestedFunc,
            Action onIsPrefetchChangedFunc,
            Action onIsIntermediateResultExpectedChangedFunc,
            Action onPriorityChangedFunc)
        {
            _onCancellationRequestedFunc = onCancellationRequestedFunc;
            _onIsPrefetchChangedFunc = onIsPrefetchChangedFunc;
            _onIsIntermediateResultExpectedChangedFunc = onIsIntermediateResultExpectedChangedFunc;
            _onPriorityChangedFunc = onPriorityChangedFunc;
        }

        /// <summary>
        /// Method that is called when a client cancels the request.
        /// </summary>
        public void OnCancellationRequested()
        {
            _onCancellationRequestedFunc();
        }

        /// <summary>
        /// Method that is called when a request is no longer a prefetch
        /// or vice versa.
        /// </summary>
        public void OnIsPrefetchChanged()
        {
            _onIsPrefetchChangedFunc();
        }

        /// <summary>
        /// Method that is called when intermediate results start or
        /// stop being expected.
        /// </summary>
        public void OnIsIntermediateResultExpectedChanged()
        {
            _onIsIntermediateResultExpectedChangedFunc();
        }

        /// <summary>
        /// Method that is called when the priority of the request changes.
        /// </summary>
        public void OnPriorityChanged()
        {
            _onPriorityChangedFunc();
        }
    }
}
