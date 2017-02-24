using ImagePipeline.Producers;
using ImagePipeline.Request;
using System;
using System.Collections.Generic;

namespace ImagePipeline.Listener
{
    /// <summary>
    /// Provides custom implementation for <see cref="IRequestListener"/>.
    /// </summary>
    public class RequestListenerImpl : IRequestListener
    {
        private ProducerListenerImpl _producerListener;
        private Action<ImageRequest, object, string, bool> _onRequestStartFunc;
        private Action<ImageRequest, string, bool> _onRequestSuccessFunc;
        private Action<ImageRequest, string, Exception, bool> _onRequestFailureFunc;
        private Action<string> _onRequestCancellationFunc;

        /// <summary>
        /// Instantiates the <see cref="RequestListenerImpl"/>.
        /// </summary>
        public RequestListenerImpl(
            ProducerListenerImpl producerListener,
            Action<ImageRequest, object, string, bool> onRequestStartFunc,
            Action<ImageRequest, string, bool> onRequestSuccessFunc,
            Action<ImageRequest, string, Exception, bool> onRequestFailureFunc,
            Action<string> onRequestCancellationFunc)
        {
            _producerListener = producerListener;
            _onRequestStartFunc = onRequestStartFunc;
            _onRequestSuccessFunc = onRequestSuccessFunc;
            _onRequestFailureFunc = onRequestFailureFunc;
            _onRequestCancellationFunc = onRequestCancellationFunc;
        }

        /// <summary>
        /// Called whenever a producer starts processing unit of work.
        /// This method might be called multiple times, but between any
        /// two consecutive calls to OnProducerStart and 
        /// OnProducerFinishWithSuccess will be called exactly once.
        /// </summary>
        public void OnProducerStart(string requestId, string producerName)
        {
            _producerListener.OnProducerStartFunc(requestId, producerName);
        }

        /// <summary>
        /// Called whenever an important producer-specific event occurs.
        /// This may only be called if OnProducerStart has been called,
        /// but corresponding OnProducerFinishWith method has not been
        /// called yet.
        /// </summary>
        public void OnProducerEvent(string requestId, string producerName, string eventName)
        {
            _producerListener.OnProducerEventFunc(requestId, producerName, eventName);
        }

        /// <summary>
        /// Called when a producer successfully finishes processing current
        /// unit of work.
        /// </summary>
        /// <param name="requestId">Request id.</param>
        /// <param name="producerName">Producer name.</param>
        /// <param name="extraMap">
        /// Additional parameters about the producer. This map is immutable
        /// and will throw an exception if attempts are made to modify it.
        /// </param>
        public void OnProducerFinishWithSuccess(
            string requestId,
            string producerName,
            IDictionary<string, string> extraMap)
        {
            _producerListener.OnProducerFinishWithSuccessFunc(requestId, producerName, extraMap);
        }

        /// <summary>
        /// Called when producer finishes processing current unit of work due
        /// to an error.
        /// </summary>
        /// <param name="requestId">Request id.</param>
        /// <param name="producerName">Producer name.</param>
        /// <param name="extraMap">
        /// Additional parameters about the producer. This map is immutable
        /// and will throw an exception if attempts are made to modify it.
        /// </param>
        /// <param name="error">Error.</param>
        public void OnProducerFinishWithFailure(
            string requestId,
            string producerName,
            Exception error,
            IDictionary<string, string> extraMap)
        {
            _producerListener.OnProducerFinishWithFailureFunc(requestId, producerName, error, extraMap);
        }

        /// <summary>
        /// Called once when producer finishes due to cancellation.
        /// </summary>
        /// <param name="requestId">Request id.</param>
        /// <param name="producerName">Producer name.</param>
        /// <param name="extraMap">
        /// Additional parameters about the producer. This map is immutable
        /// and will throw an exception if attempts are made to modify it.
        /// </param>
        public void OnProducerFinishWithCancellation(
            string requestId,
            string producerName,
            IDictionary<string, string> extraMap)
        {
            _producerListener.OnProducerFinishWithCancellationFunc(requestId, producerName, extraMap);
        }

        /// <summary>
        /// <returns>true if listener makes use of extra map</returns>
        /// <param name="requestId"></param>
        /// </summary>
        public bool RequiresExtraMap(string requestId)
        {
            return _producerListener.RequiresExtraMapFunc(requestId);
        }

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
        public void OnRequestStart(
            ImageRequest request,
            object callerContext,
            string requestId,
            bool isPrefetch)
        {
            _onRequestStartFunc(request, callerContext, requestId, isPrefetch);
        }

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
        public void OnRequestSuccess(ImageRequest request, string requestId, bool isPrefetch)
        {
            _onRequestSuccessFunc(request, requestId, isPrefetch);
        }

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
        public void OnRequestFailure(
            ImageRequest request,
            string requestId,
            Exception error,
            bool isPrefetch)
        {
            _onRequestFailureFunc(request, requestId, error, isPrefetch);
        }

        /// <summary>
        /// Called after the request is cancelled.
        /// </summary>
        /// <param name="requestId">
        /// Unique id generated automatically for each request submission.
        /// </param>
        public void OnRequestCancellation(string requestId)
        {
            _onRequestCancellationFunc(requestId);
        }
    }
}
