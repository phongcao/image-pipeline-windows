namespace ImagePipeline.Producers
{
    /// <summary>
    /// Building block for image processing in the image pipeline.
    ///
    /// <para /> Execution of image request consists of multiple different tasks such as network fetch,
    /// disk caching, memory caching, decoding, applying transformations etc. Producer{T} represents
    /// single task whose result is an instance of T. Breaking entire request into sequence of
    /// Producers allows us to construct different requests while reusing the same blocks.
    ///
    /// <para /> Producer supports multiple values and streaming.
    ///
    /// </summary>
    public interface IProducer<T>
    {
        /// <summary>
        /// Start producing results for given context. Provided consumer is notified whenever 
        /// progress is made (new value is ready or error occurs).
        /// </summary>
        void ProduceResults(IConsumer<T> consumer, IProducerContext context);
    }
}
