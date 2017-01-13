using ImagePipeline.Common;
using ImagePipeline.Request;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// ProducerContext that allows the client to change its internal state.
    /// </summary>
    public class SettableProducerContext : BaseProducerContext
    {
        /// <summary>
        /// Instantiates the <see cref="SettableProducerContext"/>
        /// </summary>
        public SettableProducerContext(
            ImageRequest imageRequest,
            string id,
            IProducerListener producerListener,
            object callerContext,
            RequestLevel lowestPermittedRequestLevel,
            bool isPrefetch,
            bool isIntermediateResultExpected,
            Priority priority) : base(
                imageRequest,
                id,
                producerListener,
                callerContext,
                lowestPermittedRequestLevel,
                isPrefetch,
                isIntermediateResultExpected,
                priority)
        {
        }

        /// <summary>
        /// Set whether the request is a prefetch request or not.
        /// <param name="isPrefetch"></param>
        /// </summary>
        public void SetIsPrefetch(bool isPrefetch)
        {
            CallOnIsPrefetchChanged(SetIsPrefetchNoCallbacks(isPrefetch));
        }

        /// <summary>
        /// Set whether intermediate result is expected or not
        /// <param name="isIntermediateResultExpected"></param>
        /// </summary>
        public void SetIsIntermediateResultExpected(bool isIntermediateResultExpected)
        {
            CallOnIsIntermediateResultExpectedChanged(
                SetIsIntermediateResultExpectedNoCallbacks(isIntermediateResultExpected));
        }

        /// <summary>
        /// Set the priority of the request
        /// <param name="priority"></param>
        /// </summary>
        public void SetPriority(Priority priority)
        {
            CallOnPriorityChanged(SetPriorityNoCallbacks(priority));
        }
    }
}
