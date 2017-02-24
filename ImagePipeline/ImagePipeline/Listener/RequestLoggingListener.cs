using ImagePipeline.Request;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ImagePipeline.Listener
{
    /// <summary>
    /// Listener for <see cref="ImageRequest"/>.
    /// </summary>
    public class RequestLoggingListener : IRequestListener
    {
        private readonly object _gate = new object();

        private readonly IDictionary<KeyValuePair<string, string>, long?> _producerStartTimeMap;
        private readonly IDictionary<string, long?> _requestStartTimeMap;

        /// <summary>
        /// Instantiates the <see cref="RequestLoggingListener"/>.
        /// </summary>
        public RequestLoggingListener()
        {
            _producerStartTimeMap = new Dictionary<KeyValuePair<string, string>, long?>();
            _requestStartTimeMap = new Dictionary<string, long?>();
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
            lock (_gate)
            {
                Debug.WriteLine($"time { GetTime() }: OnRequestSubmit: {{requestId: { requestId }, callerContext: { callerContext.ToString() }, isPrefetch: { isPrefetch }}}");
                _requestStartTimeMap.Add(requestId, GetTime());
            }
        }

        /// <summary>
        /// Called whenever a producer starts processing unit of work.
        /// This method might be called multiple times, but between any
        /// two consecutive calls to OnProducerStart and 
        /// OnProducerFinishWithSuccess will be called exactly once.
        /// </summary>
        public void OnProducerStart(string requestId, string producerName)
        {
            lock (_gate)
            {
                KeyValuePair<string, string> mapKey = 
                    new KeyValuePair<string, string>(requestId, producerName);
                long startTime = GetTime();
                _producerStartTimeMap.Add(mapKey, startTime);
                Debug.WriteLine($"time { startTime }: OnProducerStart: {{requestId: { requestId }, producer: { producerName }}}");
            }
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
            lock (_gate)
            {
                KeyValuePair<string, string> mapKey =
                    new KeyValuePair<string, string>(requestId, producerName);
                long? startTime = default(long?);
                if (_producerStartTimeMap.TryGetValue(mapKey, out startTime))
                {
                    _producerStartTimeMap.Remove(mapKey);
                }

                long currentTime = GetTime();
                Debug.WriteLine($"time { currentTime }: OnProducerFinishWithSuccess: " + 
                    $"{{requestId: { requestId }, producer: { producerName }, elapsedTime: { GetElapsedTime(startTime, currentTime) } ms, extraMap: { extraMap }}}");
            }
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
            lock (_gate)
            {
                KeyValuePair<string, string> mapKey =
                    new KeyValuePair<string, string>(requestId, producerName);
                long? startTime = default(long?);
                if (_producerStartTimeMap.TryGetValue(mapKey, out startTime))
                {
                    _producerStartTimeMap.Remove(mapKey);
                }

                long currentTime = GetTime();
                Debug.WriteLine($"time { currentTime }: OnProducerFinishWithFailure: " +
                    $"{{requestId: { requestId }, stage: { producerName }, elapsedTime: { GetElapsedTime(startTime, currentTime) } ms, extraMap: { extraMap }}}, error: { error.Message }");
            }
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
            lock (_gate)
            {
                KeyValuePair<string, string> mapKey =
                    new KeyValuePair<string, string>(requestId, producerName);
                long? startTime = default(long?);
                if (_producerStartTimeMap.TryGetValue(mapKey, out startTime))
                {
                    _producerStartTimeMap.Remove(mapKey);
                }

                long currentTime = GetTime();
                Debug.WriteLine($"time { currentTime }: OnProducerFinishWithCancellation: " +
                    $"{{requestId: { requestId }, stage: { producerName }, elapsedTime: { GetElapsedTime(startTime, currentTime) } ms, extraMap: { extraMap }}}");
            }
        }

        /// <summary>
        /// Called whenever an important producer-specific event occurs.
        /// This may only be called if OnProducerStart has been called,
        /// but corresponding OnProducerFinishWith method has not been
        /// called yet.
        /// </summary>
        public void OnProducerEvent(string requestId, string producerName, string producerEventName)
        {
            lock (_gate)
            {
                KeyValuePair<string, string> mapKey =
                    new KeyValuePair<string, string>(requestId, producerName);
                long? startTime = default(long?);
                _producerStartTimeMap.TryGetValue(mapKey, out startTime);
                long currentTime = GetTime();
                Debug.WriteLine(
                    $"time { GetTime() }: OnProducerEvent: {{requestId: { requestId }, stage: { producerName }, eventName: { producerEventName }; elapsedTime: { GetElapsedTime(startTime, currentTime) } ms}}");
            }
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
        public void OnRequestSuccess(
            ImageRequest request,
            string requestId,
            bool isPrefetch)
        {
            lock (_gate)
            {
                long? startTime = default(long?);
                if (_requestStartTimeMap.TryGetValue(requestId, out startTime))
                {
                    _requestStartTimeMap.Remove(requestId);
                }

                long currentTime = GetTime();
                Debug.WriteLine(
                    $"time { currentTime }: OnRequestSuccess: {{requestId: { requestId }, elapsedTime: { GetElapsedTime(startTime, currentTime) } ms}}");
            }
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
            lock (_gate)
            {
                long? startTime = default(long?);
                if (_requestStartTimeMap.TryGetValue(requestId, out startTime))
                {
                    _requestStartTimeMap.Remove(requestId);
                }

                long currentTime = GetTime();
                Debug.WriteLine(
                    $"time { currentTime }: OnRequestFailure: {{requestId: { requestId }, elapsedTime: { GetElapsedTime(startTime, currentTime) } ms, throwable: { error.Message }}}");
            }
        }

        /// <summary>
        /// Called after the request is cancelled.
        /// </summary>
        /// <param name="requestId">
        /// Unique id generated automatically for each request submission.
        /// </param>
        public void OnRequestCancellation(string requestId)
        {
            lock (_gate)
            {
                long? startTime = default(long?);
                if (_requestStartTimeMap.TryGetValue(requestId, out startTime))
                {
                    _requestStartTimeMap.Remove(requestId);
                }

                long currentTime = GetTime();
                Debug.WriteLine(
                    $"time { currentTime }: OnRequestCancellation: {{requestId: { requestId }, elapsedTime: { GetElapsedTime(startTime, currentTime) } ms}}");
            }
        }

        /// <summary>
        /// Checks if listener makes use of extra map.
        /// </summary>
        /// <param name="requestId">The request id.</param>
        /// <returns>
        /// true if listener makes use of extra map.
        /// </returns>
        public bool RequiresExtraMap(string requestId)
        {
            return true;
        }

        private long GetElapsedTime(long? startTime, long endTime)
        {
            if (startTime.HasValue)
            {
                return endTime - startTime.Value;
            }

            return -1;
        }

        private long GetTime()
        {
            return Environment.TickCount;
        }
    }
}
