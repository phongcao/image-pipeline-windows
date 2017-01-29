using System;
using FBCore.Concurrency;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using FBCore.Common.Internal;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Resizes and rotates JPEG image according to the EXIF orientation data.
    ///
    /// <para /> If the image is not JPEG, no transformation is applied.
    /// <para />Should not be used if downsampling is in use.
    /// </summary>
    public class ResizeAndRotateProducer : IProducer<EncodedImage>
    {
        private const string PRODUCER_NAME = "ResizeAndRotateProducer";
        private const string ORIGINAL_SIZE_KEY = "Original size";
        private const string REQUESTED_SIZE_KEY = "Requested size";
        private const string FRACTION_KEY = "Fraction";

        internal const int DEFAULT_JPEG_QUALITY = 85;
        internal const int MAX_JPEG_SCALE_NUMERATOR = 8; // JpegTranscoder.SCALE_DENOMINATOR;
        internal const int MIN_TRANSFORM_INTERVAL_MS = 100;

        internal const float ROUNDUP_FRACTION = 2.0f / 3;

        private readonly IExecutorService _executor;
        private readonly IPooledByteBufferFactory _pooledByteBufferFactory;
        private readonly IProducer<EncodedImage> _inputProducer;

        /// <summary>
        /// Instantiates the <see cref="ResizeAndRotateProducer"/>
        /// </summary>
        public ResizeAndRotateProducer(
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
            // TODO: Adding TransformingConsumer
            // _inputProducer.ProduceResults(new TransformingConsumer(consumer, context), context);
            _inputProducer.ProduceResults(consumer, context);
        }
    }
}
