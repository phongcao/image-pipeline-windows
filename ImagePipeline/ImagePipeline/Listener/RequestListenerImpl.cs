using ImagePipeline.Producers;
using ImagePipeline.Request;
using System;
using System.Collections.Generic;

namespace ImagePipeline.Listener
{
    /// <summary>
    /// Provides custom implementation for <see cref="IRequestListener"/>
    /// </summary>
    public class RequestListenerImpl : IRequestListener
    {
        private ProducerListenerImpl _producerListener;
        private Action<ImageRequest, object, string, bool> _onRequestStartFunc;
        private Action<ImageRequest, string, bool> _onRequestSuccessFunc;
        private Action<ImageRequest, string, Exception, bool> _onRequestFailureFunc;
        private Action<string> _onRequestCancellationFunc;

        /// <summary>
        /// Instantiates the <see cref="RequestListenerImpl"/>
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
        /// Called whenever a producer starts processing unit of work. This method might be called multiple
        /// times, but between any two consecutive calls to onProducerStart onProducerFinishWithSuccess
        /// will be called exactly once.
        /// </summary>
        public void OnProducerStart(string requestId, string producerName)
        {
            _producerListener.OnProducerStartFunc(requestId, producerName);
        }

        /// <summary>
        /// Called whenever an important producer-specific event occurs. This may only be called if
        /// onProducerStart has been called, but corresponding OnProducerFinishWith method has not been
        /// called yet.
        /// </summary>
        public void OnProducerEvent(string requestId, string producerName, string eventName)
        {
            _producerListener.OnProducerEventFunc(requestId, producerName, eventName);
        }

        /// <summary>
        /// Called when a producer successfully finishes processing current unit of work.
        ///
        /// <param name="requestId">Request id</param>
        /// <param name="producerName">Producer name</param>
        /// <param name="extraMap">Additional parameters about the producer. This map is immutable and will</param>
        /// throw an exception if attempts are made to modify it.
        /// </summary>
        public void OnProducerFinishWithSuccess(
            string requestId,
            string producerName,
            IDictionary<string, string> extraMap)
        {
            _producerListener.OnProducerFinishWithSuccessFunc(requestId, producerName, extraMap);
        }

        /// <summary>
        /// Called when producer finishes processing current unit of work due to an error.
        ///
        /// <param name="requestId">Request id</param>
        /// <param name="producerName">Producer name</param>
        /// <param name="extraMap">Additional parameters about the producer. This map is immutable and will</param>
        /// <param name="error">Error</param>
        /// throw an exception if attempts are made to modify it.
        /// </summary>
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
        ///
        /// <param name="requestId">Request id</param>
        /// <param name="producerName">Producer name</param>
        /// <param name="extraMap">Additional parameters about the producer. This map is immutable and will</param>
        /// throw an exception if attempts are made to modify it.
        /// </summary>
        public void OnProducerFinishWithCancellation(
            string requestId,
            string producerName,
            IDictionary<string, string> extraMap)
        {
            _producerListener.OnProducerFinishWithCancellationFunc(requestId, producerName, extraMap);
        }

        /// <summary>
        /// @return true if listener makes use of extra map
        /// <param name="requestId"></param>
        /// </summary>
        public bool RequiresExtraMap(string requestId)
        {
            return _producerListener.RequiresExtraMapFunc(requestId);
        }

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
            _onRequestStartFunc(request, callerContext, requestId, isPrefetch);
        }

        /// <summary>
        /// Called after successful completion of the request (all producers completed successfully).
        /// <param name="request">which triggered the event</param>
        /// <param name="requestId">unique id generated automatically for each request submission</param>
        /// <param name="isPrefetch">whether the request is a prefetch or not</param>
        /// </summary>
        public void OnRequestSuccess(ImageRequest request, string requestId, bool isPrefetch)
        {
            _onRequestSuccessFunc(request, requestId, isPrefetch);
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
            _onRequestFailureFunc(request, requestId, error, isPrefetch);
        }

        /// <summary>
        /// Called after the request is cancelled.
        /// <param name="requestId">unique id generated automatically for each request submission</param>
        /// </summary>
        public void OnRequestCancellation(string requestId)
        {
            _onRequestCancellationFunc(requestId);
        }
    }
}
