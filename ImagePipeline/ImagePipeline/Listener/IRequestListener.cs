using ImagePipeline.Producers;
using ImagePipeline.Request;
using System;

namespace ImagePipeline.Listener
{
    /// <summary>
    /// Listener for <see cref="ImageRequest"/>.
    /// </summary>
    public interface IRequestListener : IProducerListener
    {
        /// <summary>
        /// Called when request is about to be submitted to the Orchestrator's executor queue.
        /// <param name="request">which triggered the event</param>
        /// <param name="callerContext">context of the caller of the request</param>
        /// <param name="requestId">unique id generated automatically for each request submission</param>
        /// <param name="isPrefetch">whether the request is a prefetch or not</param>
        /// </summary>
        void OnRequestStart(
            ImageRequest request,
            object callerContext,
            string requestId,
            bool isPrefetch);

        /// <summary>
        /// Called after successful completion of the request (all producers completed successfully).
        /// <param name="request">which triggered the event</param>
        /// <param name="requestId">unique id generated automatically for each request submission</param>
        /// <param name="isPrefetch">whether the request is a prefetch or not</param>
        /// </summary>
        void OnRequestSuccess(ImageRequest request, string requestId, bool isPrefetch);

        /// <summary>
        /// Called after failure to complete the request (some producer failed).
        /// <param name="request">which triggered the event</param>
        /// <param name="requestId">unique id generated automatically for each request submission</param>
        /// <param name="error">cause of failure</param>
        /// <param name="isPrefetch">whether the request is a prefetch or not</param>
        /// </summary>
        void OnRequestFailure(
            ImageRequest request,
            string requestId,
            Exception error,
            bool isPrefetch);

        /// <summary>
        /// Called after the request is cancelled.
        /// <param name="requestId">unique id generated automatically for each request submission</param>
        /// </summary>
        void OnRequestCancellation(string requestId);
    }
}
