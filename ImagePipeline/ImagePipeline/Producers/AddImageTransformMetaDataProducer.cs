using ImagePipeline.Image;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Add image transform meta data producer
    ///
    /// <para />Extracts meta data from the results passed down from
    /// the next producer, and adds it to the result that it returns
    /// to the consumer.
    /// </summary>
    public class AddImageTransformMetaDataProducer : IProducer<EncodedImage>
    {
        private readonly IProducer<EncodedImage> _inputProducer;

        /// <summary>
        /// Instantiates the <see cref="AddImageTransformMetaDataProducer"/>.
        /// </summary>
        /// <param name="inputProducer">The input producer.</param>
        public AddImageTransformMetaDataProducer(IProducer<EncodedImage> inputProducer)
        {
            _inputProducer = inputProducer;
        }

        /// <summary>
        /// Start producing results for given context.
        /// Provided consumer is notified whenever progress is made
        /// (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(IConsumer<EncodedImage> consumer, IProducerContext context)
        {
            _inputProducer.ProduceResults(new AddImageTransformMetaDataConsumer(consumer), context);
        }

        private class AddImageTransformMetaDataConsumer : DelegatingConsumer<EncodedImage, EncodedImage>
        {
            internal AddImageTransformMetaDataConsumer(IConsumer<EncodedImage> consumer) :
                base(consumer)
            {
            }

            protected override void OnNewResultImpl(EncodedImage newResult, bool isLast)
            {
                if (newResult == null)
                {
                    Consumer.OnNewResult(null, isLast);
                    return;
                }

                if (!EncodedImage.IsMetaDataAvailable(newResult))
                {
                    newResult.ParseMetaDataAsync().Wait();
                }

                Consumer.OnNewResult(newResult, isLast);
            }
        }
    }
}
