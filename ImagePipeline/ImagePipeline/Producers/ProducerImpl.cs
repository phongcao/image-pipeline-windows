using System;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Provides custom implementation for <see cref="IProducer{T}"/>
    /// </summary>
    public class ProducerImpl<T> : IProducer<T>
    {
        Action<IConsumer<T>, IProducerContext> _produceResultsFunc;

        /// <summary>
        /// Instantites the <see cref="ProducerImpl{T}"/>
        /// </summary>
        public ProducerImpl(Action<IConsumer<T>, IProducerContext> produceResultsFunc)
        {
            _produceResultsFunc = produceResultsFunc;
        }

        /// <summary>
        /// Start producing results for given context. Provided consumer is notified whenever 
        /// progress is made (new value is ready or error occurs).
        /// <param name="consumer"></param>
        /// <param name="context"></param>
        /// </summary>
        public void ProduceResults(IConsumer<T> consumer, IProducerContext context)
        {
            _produceResultsFunc(consumer, context);
        }
    }
}
