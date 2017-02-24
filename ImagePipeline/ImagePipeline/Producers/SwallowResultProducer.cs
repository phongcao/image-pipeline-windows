namespace ImagePipeline.Producers
{
    /// <summary>
    /// Swallow result producer.
    ///
    /// <para />This producer just inserts a consumer that swallows results
    /// into the stack of consumers.
    /// </summary>
    public class SwallowResultProducer<T> : IProducer<object>
    {
        private readonly IProducer<T> _inputProducer;

        /// <summary>
        /// Instantiates the <see cref="SwallowResultProducer{T}"/>.
        /// </summary>
        public SwallowResultProducer(IProducer<T> inputProducer)
        {
            _inputProducer = inputProducer;
        }

        /// <summary>
        /// Start producing results for given context.
        /// Provided consumer is notified whenever progress is made
        /// (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(IConsumer<object> consumer, IProducerContext producerContext)
        {
            DelegatingConsumer<T, object> swallowResultConsumer =
                new SwallowResultConsumer(consumer);

            _inputProducer.ProduceResults(swallowResultConsumer, producerContext);
        }

        private class SwallowResultConsumer : DelegatingConsumer<T, object>
        {
            internal SwallowResultConsumer(IConsumer<object> consumer) :
                base(consumer)
            {
            }

            protected override void OnNewResultImpl(T newResult, bool isLast)
            {
                if (isLast)
                {
                    Consumer.OnNewResult(null, isLast);
                }
            }
        }
    }
}
