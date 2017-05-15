using FBCore.Common.Internal;
using FBCore.Concurrency;
using System;
using System.Collections.Concurrent;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Only permits a configurable number of requests to be kicked off
    /// simultaneously. If that number is exceeded, then requests are
    /// queued up and kicked off once other requests complete.
    /// </summary>
    public class ThrottlingProducer<T> : IProducer<T>
    {
        internal const string PRODUCER_NAME = "ThrottlingProducer";

        private readonly object _gate = new object();
        private readonly IProducer<T> _inputProducer;
        private readonly int _maxSimultaneousRequests;

        private readonly ConcurrentQueue<Tuple<IConsumer<T>, IProducerContext>> _pendingRequests;
        private readonly IExecutorService _executor;

        private int _numCurrentRequests;

        /// <summary>
        /// Instantiates the <see cref="ThrottlingProducer{T}"/>.
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
            _numCurrentRequests = 0;
        }

        /// <summary>
        /// Start producing results for given context.
        /// Provided consumer is notified whenever progress is made
        /// (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(IConsumer<T> consumer, IProducerContext producerContext)
        {
            IProducerListener producerListener = producerContext.Listener;
            producerListener.OnProducerStart(producerContext.Id, PRODUCER_NAME);

            bool delayRequest;
            lock (_gate)
            {
                if (_numCurrentRequests >= _maxSimultaneousRequests)
                {
                    _pendingRequests.Enqueue(
                        new Tuple<IConsumer<T>, IProducerContext>(consumer, producerContext));

                    delayRequest = true;
                }
                else
                {
                    _numCurrentRequests++;
                    delayRequest = false;
                }
            }

            if (!delayRequest)
            {
                ProduceResultsInternal(consumer, producerContext);
            }
        }

        void ProduceResultsInternal(IConsumer<T> consumer, IProducerContext producerContext)
        {
            IProducerListener producerListener = producerContext.Listener;
            producerListener.OnProducerFinishWithSuccess(producerContext.Id, PRODUCER_NAME, null);
            _inputProducer.ProduceResults(new ThrottlerConsumer(this, consumer), producerContext);
        }

        private class ThrottlerConsumer : DelegatingConsumer<T, T>
        {
            private ThrottlingProducer<T> _parent;

            /// Instantiates the <see cref="ThrottlerConsumer"/>.
            internal ThrottlerConsumer(
                ThrottlingProducer<T> parent,
                IConsumer<T> consumer) : base(consumer)
            {
                _parent = parent;
            }

            protected override void OnNewResultImpl(T newResult, bool isLast)
            {
                Consumer.OnNewResult(newResult, isLast);
                if (isLast)
                {
                    OnRequestFinished();
                }
            }

            protected override void OnFailureImpl(Exception t)
            {
                Consumer.OnFailure(t);
                OnRequestFinished();
            }

            protected override void OnCancellationImpl()
            {
                Consumer.OnCancellation();
                OnRequestFinished();
            }

            private void OnRequestFinished()
            {
                var nextRequestPair = default(Tuple<IConsumer<T>, IProducerContext>);
                lock (_parent._gate)
                {
                    if (!_parent._pendingRequests.TryDequeue(out nextRequestPair))
                    {
                        _parent._numCurrentRequests--;
                    }
                }

                if (nextRequestPair != default(Tuple<IConsumer<T>, IProducerContext>))
                {
                    _parent._executor.Execute(() =>
                    {
                        _parent.ProduceResultsInternal(nextRequestPair.Item1, nextRequestPair.Item2);
                    });
                }
            }
        }
    }
}
