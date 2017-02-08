using FBCore.Common.References;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using System.IO;
using Windows.Storage.Streams;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Remove image transform meta data producer.
    ///
    /// <para />Remove the ImageTransformMetaData object from the results passed down from 
    /// the next producer, and adds it to the result that it returns to the consumer.
    /// </summary>
    public class RemoveImageTransformMetaDataProducer :
        IProducer<CloseableReference<IRandomAccessStream>>
    {
        private readonly IProducer<EncodedImage> _inputProducer;

        /// <summary>
        /// Instantiates the <see cref="RemoveImageTransformMetaDataProducer"/>
        /// </summary>
        public RemoveImageTransformMetaDataProducer(
            IProducer<EncodedImage> inputProducer)
        {
            _inputProducer = inputProducer;
        }

        /// <summary>
        /// Start producing results for given context. Provided consumer is notified whenever 
        /// progress is made (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(
            IConsumer<CloseableReference<IRandomAccessStream>> consumer, 
            IProducerContext context)
        {
            _inputProducer.ProduceResults(new RemoveImageTransformMetaDataConsumer(consumer), context);
        }

        private class RemoveImageTransformMetaDataConsumer : 
            DelegatingConsumer<EncodedImage, CloseableReference<IRandomAccessStream>>
        {
            internal RemoveImageTransformMetaDataConsumer(
                IConsumer<CloseableReference<IRandomAccessStream>> consumer) : base(consumer)
            {
            }

            protected override void OnNewResultImpl(EncodedImage newResult, bool isLast)
            {
                CloseableReference<IRandomAccessStream> ret = null;

                try
                {
                    if (EncodedImage.IsValid(newResult))
                    {
                        ret = CloseableReference<IRandomAccessStream>.of(
                            newResult.GetInputStream().AsRandomAccessStream());
                    }

                    Consumer.OnNewResult(ret, isLast);
                }
                finally
                {
                    CloseableReference<IRandomAccessStream>.CloseSafely(ret);
                }
            }
        }
    }
}
