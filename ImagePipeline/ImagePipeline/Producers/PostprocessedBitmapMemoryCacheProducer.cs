using System;
using FBCore.Common.References;
using ImagePipeline.Image;
using ImagePipeline.Cache;
using Cache.Common;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Memory cache producer for the bitmap memory cache.
    /// </summary>
    public class PostprocessedBitmapMemoryCacheProducer : IProducer<CloseableReference<CloseableImage>>
    {
        internal const string PRODUCER_NAME = "PostprocessedBitmapMemoryCacheProducer";

        private IMemoryCache<ICacheKey, CloseableImage> _memoryCache;
        private ICacheKeyFactory _cacheKeyFactory;
        private IProducer<CloseableReference<CloseableImage>> _inputProducer;

        /// <summary>
        /// Instantiates the <see cref="PostprocessedBitmapMemoryCacheProducer"/>
        /// </summary>
        public PostprocessedBitmapMemoryCacheProducer(
            IMemoryCache<ICacheKey, CloseableImage> memoryCache,
            ICacheKeyFactory cacheKeyFactory,
            IProducer<CloseableReference<CloseableImage>> inputProducer)
        {
            _memoryCache = memoryCache;
            _cacheKeyFactory = cacheKeyFactory;
            _inputProducer = inputProducer;
        }

        /// <summary>
        /// Start producing results for given context. Provided consumer is notified whenever 
        /// progress is made (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(
            IConsumer<CloseableReference<CloseableImage>> consumer, 
            IProducerContext context)
        {
            throw new NotImplementedException();
        }
    }
}
