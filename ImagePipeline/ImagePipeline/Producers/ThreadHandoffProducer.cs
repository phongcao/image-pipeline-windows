using FBCore.Common.Internal;

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
    //            StatefulProducerRunnable<T> statefulRunnable = new StatefulProducerRunnable<T>(
    //                consumer,
    //                producerListener,
    //                PRODUCER_NAME,
    //                requestId) {
    //        @Override
    //        protected void onSuccess(T ignored)
    //        {
    //            producerListener.onProducerFinishWithSuccess(requestId, PRODUCER_NAME, null);
    //            mInputProducer.produceResults(consumer, context);
    //        }

    //        @Override
    //        protected void disposeResult(T ignored) { }

    //        @Override
    //        protected T getResult() throws Exception
    //        {
    //        return null;
    //        }
    //    };
    //    context.addCallbacks(
    //        new BaseProducerContextCallbacks()
    //    {
    //        @Override
    //            public void onCancellationRequested()
    //    {
    //        statefulRunnable.cancel();
    //        mThreadHandoffProducerQueue.remove(statefulRunnable);
    //    }
    //});
            //ThreadHandoffProducerQueue.addToQueueOrExecute(statefulRunnable);
        }
    }
}
