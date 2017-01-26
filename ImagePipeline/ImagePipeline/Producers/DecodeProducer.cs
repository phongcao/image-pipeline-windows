using FBCore.Common.Internal;
using FBCore.Common.References;
using FBCore.Concurrency;
using ImagePipeline.Decoder;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using System;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Decodes images.
    ///
    /// <p/> Progressive JPEGs are decoded progressively as new data arrives.
    /// </summary>
    public class DecodeProducer : IProducer<CloseableReference<CloseableImage>>
    {
        /// <summary>
        /// The producer name
        /// </summary>
        public const string PRODUCER_NAME = "DecodeProducer";

        /// <summary>
        /// Keys for extra map
        /// </summary>
        private const string BITMAP_SIZE_KEY = "bitmapSize";
        private const string HAS_GOOD_QUALITY_KEY = "hasGoodQuality";
        private const string IMAGE_TYPE_KEY = "imageType";
        private const string IS_FINAL_KEY = "isFinal";

        private readonly IByteArrayPool _byteArrayPool;
        private readonly IExecutorService _executor;
        private readonly ImageDecoder _imageDecoder;
        private readonly IProgressiveJpegConfig _progressiveJpegConfig;
        private readonly IProducer<EncodedImage> _inputProducer;
        private readonly bool _downsampleEnabled;
        private readonly bool _downsampleEnabledForNetwork;

        /// <summary>
        /// Instantiates the <see cref="DecodeProducer"/>
        /// </summary>
        public DecodeProducer(
            IByteArrayPool byteArrayPool,
            IExecutorService executor,
            ImageDecoder imageDecoder,
            IProgressiveJpegConfig progressiveJpegConfig,
            bool downsampleEnabled,
            bool downsampleEnabledForNetwork,
            IProducer<EncodedImage> inputProducer)
        {
            _byteArrayPool = Preconditions.CheckNotNull(byteArrayPool);
            _executor = Preconditions.CheckNotNull(executor);
            _imageDecoder = Preconditions.CheckNotNull(imageDecoder);
            _progressiveJpegConfig = Preconditions.CheckNotNull(progressiveJpegConfig);
            _downsampleEnabled = downsampleEnabled;
            _downsampleEnabledForNetwork = downsampleEnabledForNetwork;
            _inputProducer = Preconditions.CheckNotNull(inputProducer);
        }

        /// <summary>
        /// Start producing results for given context. Provided consumer is notified whenever 
        /// progress is made (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(
            IConsumer<CloseableReference<CloseableImage>> consumer,
            IProducerContext producerContext)
        {
            throw new NotImplementedException();
        }
    }
}
