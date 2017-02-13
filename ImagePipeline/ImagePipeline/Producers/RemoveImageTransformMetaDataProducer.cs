using FBCore.Common.References;
using ImagePipeline.Image;
using ImagePipeline.Memory;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Remove image transform meta data producer.
    ///
    /// <para />Remove the ImageTransformMetaData object from the results passed down from 
    /// the next producer, and adds it to the result that it returns to the consumer.
    /// </summary>
    public class RemoveImageTransformMetaDataProducer :
        IProducer<CloseableReference<IPooledByteBuffer>>
    {
        private readonly IProducer<EncodedImage> _inputProducer;

        /// <summary>
        /// Instantiates the <see cref="RemoveImageTransformMetaDataProducer"/>
        /// </summary>
        public RemoveImageTransformMetaDataProducer(IProducer<EncodedImage> inputProducer)
        {
            _inputProducer = inputProducer;
        }

        /// <summary>
        /// Start producing results for given context. Provided consumer is notified whenever 
        /// progress is made (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(
            IConsumer<CloseableReference<IPooledByteBuffer>> consumer, 
            IProducerContext context)
        {
            _inputProducer.ProduceResults(
                new RemoveImageTransformMetaDataConsumer(this, consumer), context);
        }

        private class RemoveImageTransformMetaDataConsumer : 
            DelegatingConsumer<EncodedImage, CloseableReference<IPooledByteBuffer>>
        {
            private RemoveImageTransformMetaDataProducer _parent;

            internal RemoveImageTransformMetaDataConsumer(
                RemoveImageTransformMetaDataProducer parent,
                IConsumer<CloseableReference<IPooledByteBuffer>> consumer) : base(consumer)
            {
                _parent = parent;
            }

            protected override void OnNewResultImpl(EncodedImage newResult, bool isLast)
            {
                CloseableReference<IPooledByteBuffer> ret = null;

                try
                {
                    if (EncodedImage.IsValid(newResult))
                    {
                        ret = newResult.GetByteBufferRef();
                    }

                    Consumer.OnNewResult(ret, isLast);
                }
                finally
                {
                    CloseableReference<IPooledByteBuffer>.CloseSafely(ret);
                }
            }
        }
    }
}
