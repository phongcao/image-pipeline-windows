using ImagePipeline.Image;
using ImagePipeline.Request;
using System;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Producer that coordinates fetching two separate images.
    ///
    /// <para />The first producer is kicked off, and once it has returned all its 
    /// results, the second producer is kicked off if necessary.
    /// </summary>
    public class BranchOnSeparateImagesProducer : IProducer<EncodedImage>
    {
        private readonly IProducer<EncodedImage> _inputProducer1;
        private readonly IProducer<EncodedImage> _inputProducer2;

        /// <summary>
        /// Instantiates the <see cref="BranchOnSeparateImagesProducer"/>
        /// </summary>
        public BranchOnSeparateImagesProducer(
            IProducer<EncodedImage> inputProducer1,
            IProducer<EncodedImage> inputProducer2)
        {
            _inputProducer1 = inputProducer1;
            _inputProducer2 = inputProducer2;
        }

        /// <summary>
        /// Start producing results for given context. Provided consumer is notified whenever 
        /// progress is made (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(
            IConsumer<EncodedImage> consumer,
            IProducerContext context)
        {
            OnFirstImageConsumer onFirstImageConsumer = new OnFirstImageConsumer(
                _inputProducer2, consumer, context);
            _inputProducer1.ProduceResults(onFirstImageConsumer, context);
        }

        private class OnFirstImageConsumer : DelegatingConsumer<EncodedImage, EncodedImage> 
        {
            private IProducer<EncodedImage> _inputProducer2;
            private IProducerContext _producerContext;

            /// <summary>
            /// Instantiates the <see cref="OnFirstImageConsumer"/>
            /// </summary>
            internal OnFirstImageConsumer(
                IProducer<EncodedImage> inputProducer2,
                IConsumer<EncodedImage> consumer,
                IProducerContext producerContext) :
                base(consumer)
            {
                _inputProducer2 = inputProducer2;
                _producerContext = producerContext;
            }

            protected override void OnNewResultImpl(EncodedImage newResult, bool isLast)
            {
                ImageRequest request = _producerContext.ImageRequest;
                bool isGoodEnough = 
                    ThumbnailSizeChecker.IsImageBigEnough(newResult, request.ResizeOptions);

                if (newResult != null && (isGoodEnough || request.IsLocalThumbnailPreviewsEnabled))
                {
                    Consumer.OnNewResult(newResult, isLast && isGoodEnough);
                }

                if (isLast && !isGoodEnough)
                {
                    EncodedImage.CloseSafely(newResult);

                    _inputProducer2.ProduceResults(Consumer, _producerContext);
                }
            }

            protected override void OnFailureImpl(Exception t)
            {
                _inputProducer2.ProduceResults(Consumer, _producerContext);
            }
        }
    }
}
