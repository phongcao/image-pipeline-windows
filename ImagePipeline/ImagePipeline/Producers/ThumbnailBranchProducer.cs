using ImagePipeline.Image;
using System;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Producer that will attempt to retrieve a thumbnail from one or
    /// more producers.
    ///
    /// <para />The producer will try to get a result from each producer
    /// only if there is a good chance of it being able to produce a
    /// sufficient result.
    ///
    /// <para />If no underlying producer can provide a suitable result,
    /// null result is returned to the consumer.
    /// </summary>
    public class ThumbnailBranchProducer : IProducer<EncodedImage>
    {
        internal const string PRODUCER_NAME = "ThumbnailBranchProducer";

        /// <summary>
        /// Instantiates the <see cref="ThumbnailBranchProducer"/>
        /// </summary>
        public ThumbnailBranchProducer(params IThumbnailProducer<EncodedImage>[] thumbnailProducers)
        {
        }

        /// <summary>
        /// Start producing results for given context.
        /// Provided consumer is notified whenever progress is made
        /// (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(
            IConsumer<EncodedImage> consumer,
            IProducerContext producerContext)
        {
            throw new NotImplementedException();
        }
    }
}
