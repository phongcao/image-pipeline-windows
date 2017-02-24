using FBCore.Common.References;
using FBCore.Concurrency;
using ImagePipeline.Image;
using System;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// A producer that creates video thumbnails.
    ///
    /// <para />At present, these thumbnails are created on the managed
    /// memory rather than being pinned purgeables. This is deemed okay
    /// as the thumbnails are only very small.
    /// </summary>
    public class LocalVideoThumbnailProducer : IProducer<CloseableReference<CloseableImage>>
    {
        internal const string PRODUCER_NAME = "VideoThumbnailProducer";

        private readonly IExecutorService _executor;

        /// <summary>
        /// Instantiates the <see cref="LocalVideoThumbnailProducer"/>.
        /// </summary>
        public LocalVideoThumbnailProducer(IExecutorService executor)
        {
            _executor = executor;
        }

        /// <summary>
        /// Start producing results for given context.
        /// Provided consumer is notified whenever progress is made
        /// (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(
            IConsumer<CloseableReference<CloseableImage>> consumer,
            IProducerContext producerContext)
        {
            throw new NotImplementedException();
        }
    }
}
