using FBCore.Common.Internal;
using ImagePipeline.Common;
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

        private readonly IThumbnailProducer<EncodedImage>[] _thumbnailProducers;

        /// <summary>
        /// Instantiates the <see cref="ThumbnailBranchProducer"/>
        /// </summary>
        public ThumbnailBranchProducer(params IThumbnailProducer<EncodedImage>[] thumbnailProducers)
        {
            _thumbnailProducers = Preconditions.CheckNotNull(thumbnailProducers);
            Preconditions.CheckElementIndex(0, _thumbnailProducers.Length);
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
            if (producerContext.ImageRequest.ResizeOptions == null)
            {
                consumer.OnNewResult(null, true);
            }
            else
            {
                bool requested = ProduceResultsFromThumbnailProducer(0, consumer, producerContext);
                if (!requested)
                {
                    consumer.OnNewResult(null, true);
                }
            }
        }

        private bool ProduceResultsFromThumbnailProducer(
            int startIndex,
            IConsumer<EncodedImage> consumer,
            IProducerContext context)
        {
            int producerIndex = FindFirstProducerForSize(
                startIndex, context.ImageRequest.ResizeOptions);

            if (producerIndex == -1)
            {
                return false;
            }

            _thumbnailProducers[producerIndex].ProduceResults(
                new ThumbnailConsumer(this, consumer, context, producerIndex), context);

            return true;
        }

        private int FindFirstProducerForSize(int startIndex, ResizeOptions resizeOptions)
        {
            for (int i = startIndex; i < _thumbnailProducers.Length; i++)
            {
                if (_thumbnailProducers[i].CanProvideImageForSize(resizeOptions))
                {
                    return i;
                }
            }

            return -1;
        }

        private class ThumbnailConsumer : DelegatingConsumer<EncodedImage, EncodedImage>
        {
            private ThumbnailBranchProducer _parent;
            private IProducerContext _producerContext;
            private int _producerIndex;
            private ResizeOptions _resizeOptions;

            /// <summary>
            /// Instantiates the <see cref="ThumbnailConsumer"/>.
            /// </summary>
            public ThumbnailConsumer(
                ThumbnailBranchProducer parent,
                IConsumer<EncodedImage> consumer,
                IProducerContext producerContext, int producerIndex) :
                    base(consumer)
            {
                _parent = parent;
                _producerContext = producerContext;
                _producerIndex = producerIndex;
                _resizeOptions = _producerContext.ImageRequest.ResizeOptions;
            }

            protected override void OnNewResultImpl(EncodedImage newResult, bool isLast)
            {
                if (newResult != null &&
                    (!isLast || ThumbnailSizeChecker.IsImageBigEnough(newResult, _resizeOptions)))
                {
                    Consumer.OnNewResult(newResult, isLast);
                }
                else if (isLast)
                {
                    EncodedImage.CloseSafely(newResult);

                    bool fallback = _parent.ProduceResultsFromThumbnailProducer(
                        _producerIndex + 1,
                        Consumer,
                        _producerContext);

                    if (!fallback)
                    {
                        Consumer.OnNewResult(null, true);
                    }
                }
            }

            protected override void OnFailureImpl(Exception t)
            {
                bool fallback = _parent.ProduceResultsFromThumbnailProducer(
                    _producerIndex + 1, Consumer, _producerContext);

                if (!fallback)
                {
                    Consumer.OnFailure(t);
                }
            }
        }
    }
}
