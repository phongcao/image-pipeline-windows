using FBCore.Common.Internal;
using FBCore.Concurrency;
using System;
using System.Collections.Concurrent;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Only permits a configurable number of requests to be kicked off simultaneously. 
    /// If that number is exceeded, then requests are queued up and kicked off once other 
    /// requests complete.
    /// </summary>
    public class ThrottlingProducer<T> : IProducer<T>
    {
        internal const string PRODUCER_NAME = "ThrottlingProducer";

        private readonly IProducer<T> _inputProducer;
        private readonly int _maxSimultaneousRequests;

        private readonly ConcurrentQueue<Tuple<IConsumer<T>, IProducerContext>> _pendingRequests;
        private readonly IExecutorService _executor;

        /// <summary>
        /// Instantiates the <see cref="ThrottlingProducer{T}"/>
        /// </summary>
        public ThrottlingProducer(
            int maxSimultaneousRequests,
            IExecutorService executor,
            IProducer<T> inputProducer)
        {
            _maxSimultaneousRequests = maxSimultaneousRequests;
            _executor = Preconditions.CheckNotNull(executor);
            _inputProducer = Preconditions.CheckNotNull(inputProducer);
            _pendingRequests = new ConcurrentQueue<Tuple<IConsumer<T>, IProducerContext>>();
        }

        /// <summary>
        /// Start producing results for given context. Provided consumer is notified whenever 
        /// progress is made (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(IConsumer<T> consumer, IProducerContext context)
        {
            throw new NotImplementedException();
        }
    }
}
