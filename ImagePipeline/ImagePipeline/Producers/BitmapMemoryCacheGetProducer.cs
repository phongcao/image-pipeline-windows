using Cache.Common;
using FBCore.Common.References;
using ImagePipeline.Cache;
using ImagePipeline.Image;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Bitmap memory cache producer that is read-only.
    /// </summary>
    public class BitmapMemoryCacheGetProducer : BitmapMemoryCacheProducer
    {
        internal new const string PRODUCER_NAME = "BitmapMemoryCacheGetProducer";

        /// <summary>
        /// Instantiates the <see cref="BitmapMemoryCacheGetProducer"/>
        /// </summary>
        public BitmapMemoryCacheGetProducer(
            IMemoryCache<ICacheKey, CloseableImage> memoryCache,
            ICacheKeyFactory cacheKeyFactory,
            IProducer<CloseableReference<CloseableImage>> inputProducer) : 
            base(memoryCache, cacheKeyFactory, inputProducer)
        {
        }

        /// <summary>
        /// Wraps the target consumer by the BitmapMemoryCacheConsumer
        /// </summary>
        protected override IConsumer<CloseableReference<CloseableImage>> WrapConsumer(
            IConsumer<CloseableReference<CloseableImage>> consumer,
            ICacheKey cacheKey)
        {
            // Since this cache is read-only, we can pass our consumer directly to the next producer
            return consumer;
        }

        /// <summary>
        /// Gets the producer name
        /// </summary>
        protected override string ProducerName
        {
            get
            {
                return PRODUCER_NAME;
            }
        }
    }
}
