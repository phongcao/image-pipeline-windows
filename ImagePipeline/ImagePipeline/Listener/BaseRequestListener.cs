using ImagePipeline.Request;
using System;
using System.Collections.Generic;

namespace ImagePipeline.Listener
{
    /// <summary>
    /// Listener for <see cref="ImageRequest"/>.
    /// </summary>
    public class BaseRequestListener : IRequestListener
    {
        /// <summary>
        /// Called when request is about to be submitted to the Orchestrator's executor queue.
        /// <param name="request">which triggered the event</param>
        /// <param name="callerContext">context of the caller of the request</param>
        /// <param name="requestId">unique id generated automatically for each request submission</param>
        /// <param name="isPrefetch">whether the request is a prefetch or not</param>
        /// </summary>
        public void OnRequestStart(
            ImageRequest request, 
            object callerContext, 
            string requestId, 
            bool isPrefetch)
        {
        }

        /// <summary>
        /// Called after successful completion of the request (all producers completed successfully).
        /// <param name="request">which triggered the event</param>
        /// <param name="requestId">unique id generated automatically for each request submission</param>
        /// <param name="isPrefetch">whether the request is a prefetch or not</param>
        /// </summary>
        public void OnRequestSuccess(ImageRequest request, string requestId, bool isPrefetch)
        {
        }

        /// <summary>
        /// Called after failure to complete the request (some producer failed).
        /// <param name="request">which triggered the event</param>
        /// <param name="requestId">unique id generated automatically for each request submission</param>
        /// <param name="error">cause of failure</param>
        /// <param name="isPrefetch">whether the request is a prefetch or not</param>
        /// </summary>
        public void OnRequestFailure(
            ImageRequest request, 
            string requestId, 
            Exception error, 
            bool isPrefetch)
        {
        }

        /// <summary>
        /// Called after the request is cancelled.
        /// <param name="requestId">unique id generated automatically for each request submission</param>
        /// </summary>
        public void OnRequestCancellation(string requestId)
        {
        }

        /// <summary>
        /// Called whenever a producer starts processing unit of work. This method 
        /// might be called multiple times, but between any two consecutive calls to 
        /// OnProducerStart OnProducerFinishWithSuccess will be called exactly once.
        /// </summary>
        public void OnProducerStart(string requestId, string producerName)
        {
        }

        /// <summary>
        /// Called whenever an important producer-specific event occurs. This may only 
        /// be called if OnProducerStart has been called, but corresponding 
        /// OnProducerFinishWith method has not been called yet.
        /// </summary>
        public void OnProducerEvent(string requestId, string producerName, string eventName)
        {
        }

        /// <summary>
        /// Called when a producer successfully finishes processing current unit of work.
        ///
        /// <param name="requestId">Request id</param>
        /// <param name="producerName">Producer name</param>
        /// <param name="extraMap">Additional parameters about the producer. This map is 
        /// immutable and will throw an exception if attempts are made to modify it.</param>
        /// </summary>
        public void OnProducerFinishWithSuccess(
            string requestId, 
            string producerName, 
            IDictionary<string, string> extraMap)
        {
        }

        /// <summary>
        /// Called when producer finishes processing current unit of work due to an error.
        ///
        /// <param name="requestId">Request id</param>
        /// <param name="producerName">Producer name</param>
        /// <param name="extraMap">Additional parameters about the producer. This map is 
        /// immutable and will throw an exception if attempts are made to modify it.</param>
        /// <param name="error">Error</param>
        /// </summary>
        public void OnProducerFinishWithFailure(
            string requestId,
            string producerName,
            Exception error,
            IDictionary<string, string> extraMap)
        {
        }

        /// <summary>
        /// Called once when producer finishes due to cancellation.
        ///
        /// <param name="requestId">Request id</param>
        /// <param name="producerName">Producer name</param>
        /// <param name="extraMap">Additional parameters about the producer. This map is 
        /// immutable and will throw an exception if attempts are made to modify it.</param>
        /// </summary>
        public void OnProducerFinishWithCancellation(
            string requestId, 
            string producerName, 
            IDictionary<string, string> extraMap)
        {
        }

        /// <summary>
        /// @return true if listener makes use of extra map
        /// <param name="requestId"></param>
        /// </summary>
        public bool RequiresExtraMap(string requestId)
        {
            return false;
        }
    }
}
