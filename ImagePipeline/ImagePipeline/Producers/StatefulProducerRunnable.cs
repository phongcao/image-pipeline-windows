using FBCore.Concurrency;
using System;
using System.Collections.Generic;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// <see cref="StatefulRunnable{T}"/> intended to be used by producers.
    ///
    /// <para /> Class implements common functionality related to handling producer instrumentation and
    /// resource management.
    /// </summary>
    public abstract class StatefulProducerRunnable<T> : StatefulRunnable<T>
    {
        private readonly IConsumer<T> _consumer;
        private readonly IProducerListener _producerListener;
        private readonly string _producerName;
        private readonly string _requestId;

        /// <summary>
        /// Instantiates the <see cref="StatefulProducerRunnable{T}"/>
        /// </summary>
        public StatefulProducerRunnable(
            IConsumer<T> consumer,
            IProducerListener producerListener,
            string producerName,
            string requestId)
        {
            _consumer = consumer;
            _producerListener = producerListener;
            _producerName = producerName;
            _requestId = requestId;

            _producerListener.OnProducerStart(_requestId, _producerName);
        }

        /// <summary>
        /// Called after computing result successfully.
        /// <param name="result"></param>
        /// </summary>
        protected override void OnSuccess(T result)
        {
            _producerListener.OnProducerFinishWithSuccess(
                _requestId,
                _producerName,
                _producerListener.RequiresExtraMap(_requestId) ? GetExtraMapOnSuccess(result) : null);
            _consumer.OnNewResult(result, true);
        }

        /// <summary>
        /// Called if exception occurred during computation.
        /// <param name="e"></param>
        /// </summary>
        protected override void OnFailure(Exception e)
        {
            _producerListener.OnProducerFinishWithFailure(
                _requestId,
                _producerName,
                e,
                _producerListener.RequiresExtraMap(_requestId) ? GetExtraMapOnFailure(e) : null);
            _consumer.OnFailure(e);
        }

        /// <summary>
        /// Called when the runnable is cancelled.
        /// </summary>
        protected override void OnCancellation()
        {
            _producerListener.OnProducerFinishWithCancellation(
                _requestId,
                _producerName,
                _producerListener.RequiresExtraMap(_requestId) ? GetExtraMapOnCancellation() : null);
            _consumer.OnCancellation();
        }

        /// <summary>
        /// Create extra map for result
        /// </summary>
        protected virtual IDictionary<string, string> GetExtraMapOnSuccess(T result)
        {
            return null;
        }

        /// <summary>
        /// Create extra map for exception
        /// </summary>
        protected virtual IDictionary<string, string> GetExtraMapOnFailure(Exception exception)
        {
            return null;
        }

        /// <summary>
        /// Create extra map for cancellation
        /// </summary>
        protected virtual IDictionary<string, string> GetExtraMapOnCancellation()
        {
            return null;
        }

        /// <summary>
        /// Called after OnSuccess callback completes in order to dispose the result.
        /// <param name="result"></param>
        /// </summary>
        protected override abstract void DisposeResult(T result);
    }
}
