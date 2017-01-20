using FBCore.Common.Internal;
using System.Collections.Generic;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Uses IExecutorService to move further computation to different thread
    /// </summary>
    public class ThreadHandoffProducer<T> : IProducer<T>
    {
        internal static readonly string PRODUCER_NAME = "BackgroundThreadHandoffProducer";

        private readonly IProducer<T> _inputProducer;
        private readonly ThreadHandoffProducerQueue _threadHandoffProducerQueue;

        /// <summary>
        /// Instantiates the <see cref="ThreadHandoffProducer{T}"/>
        /// </summary>
        public ThreadHandoffProducer(
            IProducer<T> inputProducer,
            ThreadHandoffProducerQueue inputThreadHandoffProducerQueue)
        {
            _inputProducer = Preconditions.CheckNotNull(inputProducer);
            _threadHandoffProducerQueue = inputThreadHandoffProducerQueue;
        }

        /// <summary>
        /// Start producing results for given context. Provided consumer is notified whenever 
        /// progress is made (new value is ready or error occurs).
        /// </summary>

        public void ProduceResults(IConsumer<T> consumer, IProducerContext context)
        {
            IProducerListener producerListener = context.Listener;
            string requestId = context.Id;
            StatefulProducerRunnable<T> statefulRunnable = new StatefulProducerRunnableImpl<T>(
                consumer,
                producerListener,
                PRODUCER_NAME,
                requestId,
                (T ignored) =>
                {
                    producerListener.OnProducerFinishWithSuccess(requestId, PRODUCER_NAME, null);
                    _inputProducer.ProduceResults(consumer, context);
                },
                (_) => { },
                () => { },
                (_) => 
                {
                    return default(IDictionary<string, string>);
                },
                (_) =>
                {
                    return default(IDictionary<string, string>);
                },
                () => 
                {
                    return default(IDictionary<string, string>);
                },
                (_) => { },
                () => 
                {
                    return default(T);
                });

            context.AddCallbacks(new BaseProducerContextCallbacks(() =>
            {
                statefulRunnable.Cancel();
                _threadHandoffProducerQueue.Remove(statefulRunnable.Runnable);
            },
            () => { },
            () => { },
            () => { }));

            _threadHandoffProducerQueue.AddToQueueOrExecute(statefulRunnable.Runnable);
        }
    }
}
