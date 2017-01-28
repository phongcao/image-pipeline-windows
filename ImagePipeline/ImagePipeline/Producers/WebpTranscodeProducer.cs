using System;
using FBCore.Common.Internal;
using FBCore.Concurrency;
using ImagePipeline.Image;
using ImagePipeline.Memory;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Transcodes WebP to JPEG / PNG.
    ///
    /// <para /> If processed image is one of VP8, VP8X or VP8L non-animated WebPs then 
    /// it is transcoded to jpeg if the decoder on the running version of Android does not 
    /// support this format. This was the case prior to version 4.2.1.
    /// <para /> If the image is not WebP, no transformation is applied.
    /// </summary>
    public class WebpTranscodeProducer : IProducer<EncodedImage>
    {
        private const string PRODUCER_NAME = "WebpTranscodeProducer";
        private const int DEFAULT_JPEG_QUALITY = 80;

        private readonly IExecutorService _executor;
        private readonly IPooledByteBufferFactory _pooledByteBufferFactory;
        private readonly IProducer<EncodedImage> _inputProducer;

        /// <summary>
        /// Instantiates the <see cref="WebpTranscodeProducer"/>
        /// </summary>
        public WebpTranscodeProducer(
            IExecutorService executor,
            IPooledByteBufferFactory pooledByteBufferFactory,
            IProducer<EncodedImage> inputProducer)
        {
            _executor = Preconditions.CheckNotNull(executor);
            _pooledByteBufferFactory = Preconditions.CheckNotNull(pooledByteBufferFactory);
            _inputProducer = Preconditions.CheckNotNull(inputProducer);
        }

        /// <summary>
        /// Start producing results for given context. Provided consumer is notified whenever 
        /// progress is made (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(IConsumer<EncodedImage> consumer, IProducerContext context)
        {
            throw new NotImplementedException();
        }
    }
}
