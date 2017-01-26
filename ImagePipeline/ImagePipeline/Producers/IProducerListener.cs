using System;
using System.Collections.Generic;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Instrumentation for Producers.
    ///
    /// <para /> Implementation of a producer should call these methods when appropriate 
    /// to notify other components interested in execution progress.
    /// </summary>
    public interface IProducerListener
    {
        /// <summary>
        /// Called whenever a producer starts processing unit of work. This method might 
        /// be called multiple times, but between any two consecutive calls to OnProducerStart 
        /// OnProducerFinishWithSuccess will be called exactly once.
        /// </summary>
        void OnProducerStart(string requestId, string producerName);

        /// <summary>
        /// Called whenever an important producer-specific event occurs. This may only be called 
        /// if OnProducerStart has been called, but corresponding OnProducerFinishWith method has 
        /// not been called yet.
        /// </summary>
        void OnProducerEvent(string requestId, string producerName, string eventName);

        /// <summary>
        /// Called when a producer successfully finishes processing current unit of work.
        ///
        /// <param name="requestId">Request id</param>
        /// <param name="producerName">Producer name</param>
        /// <param name="extraMap">Additional parameters about the producer. This map is immutable 
        /// and will throw an exception if attempts are made to modify it.</param>
        /// </summary>
        void OnProducerFinishWithSuccess(
            string requestId,
            string producerName,
            IDictionary<string, string> extraMap);

        /// <summary>
        /// Called when producer finishes processing current unit of work due to an error.
        ///
        /// <param name="requestId">Request id</param>
        /// <param name="producerName">Producer name</param>
        /// <param name="error">Error</param>
        /// <param name="extraMap">Additional parameters about the producer. This map is immutable 
        /// and will throw an exception if attempts are made to modify it.</param>
        /// </summary>
        void OnProducerFinishWithFailure(
            string requestId,
            string producerName,
            Exception error,
            IDictionary<string, string> extraMap);

        /// <summary>
        /// Called once when producer finishes due to cancellation.
        ///
        /// <param name="requestId">Request id</param>
        /// <param name="producerName">Producer name</param>
        /// <param name="extraMap">Additional parameters about the producer. This map is immutable 
        /// and will throw an exception if attempts are made to modify it.</param>
        /// </summary>
        void OnProducerFinishWithCancellation(
            string requestId,
            string producerName,
            IDictionary<string, string> extraMap);

        /// <summary>
        /// @return true if listener makes use of extra map
        /// <param name="requestId"></param>
        /// </summary>
        bool RequiresExtraMap(string requestId);
    }
}
