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
        /// Called when request is about to be submitted to the Orchestrator's
        /// executor queue.
        /// </summary>
        /// <param name="request">Which triggered the event.</param>
        /// <param name="callerContext">
        /// Context of the caller of the request.
        /// </param>
        /// <param name="requestId">
        /// Unique id generated automatically for each request submission.
        /// </param>
        /// <param name="isPrefetch">
        /// Whether the request is a prefetch or not.
        /// </param>
        void OnRequestStart(
            ImageRequest request,
            object callerContext,
            string requestId,
            bool isPrefetch);

        /// <summary>
        /// Called after successful completion of the request
        /// (all producers completed successfully).
        /// </summary>
        /// <param name="request">Which triggered the event.</param>
        /// <param name="requestId">
        /// Unique id generated automatically for each request submission.
        /// </param>
        /// <param name="isPrefetch">
        /// Whether the request is a prefetch or not.
        /// </param>
        void OnRequestSuccess(ImageRequest request, string requestId, bool isPrefetch);

        /// <summary>
        /// Called after failure to complete the request (some producer failed).
        /// </summary>
        /// <param name="request">Which triggered the event.</param>
        /// <param name="requestId">
        /// Unique id generated automatically for each request submission.
        /// </param>
        /// <param name="error">Cause of failure.</param>
        /// <param name="isPrefetch">
        /// Whether the request is a prefetch or not.
        /// </param>
        void OnRequestFailure(
            ImageRequest request,
            string requestId,
            Exception error,
            bool isPrefetch);

        /// <summary>
        /// Called after the request is cancelled.
        /// </summary>
        /// <param name="requestId">
        /// Unique id generated automatically for each request submission.
        /// </param>
        void OnRequestCancellation(string requestId);
    }
}
