namespace ImagePipeline.Producers
{
    /// <summary>
    /// Producer that never produces anything, but just returns null.
    ///
    /// <para />This producer can be used to terminate a sequence, e.g.
    /// for a bitmap cache get only sequence, just use
    /// BitmapMemoryCacheGetProducer followed by NullProducer.
    /// </summary>
    public class NullProducer<T> : IProducer<T>
    {
        /// <summary>
        /// Start producing results for given context.
        /// Provided consumer is notified whenever progress is made
        /// (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(IConsumer<T> consumer, IProducerContext context)
        {
            consumer.OnNewResult(default(T), true);
        }
    }
}
