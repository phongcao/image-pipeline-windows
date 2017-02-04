using Cache.Common;
using FBCore.Common.References;
using ImagePipeline.Cache;
using ImagePipeline.Image;
using ImagePipeline.Request;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Memory cache producer for the bitmap memory cache.
    /// </summary>
    public class PostprocessedBitmapMemoryCacheProducer : IProducer<CloseableReference<CloseableImage>>
    {
        internal const string PRODUCER_NAME = "PostprocessedBitmapMemoryCacheProducer";
        internal const string VALUE_FOUND = "cached_value_found";

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
            IProducerContext producerContext)
        {
            IProducerListener listener = producerContext.Listener;
            string requestId = producerContext.Id;
            ImageRequest imageRequest = producerContext.ImageRequest;
            object callerContext = producerContext.CallerContext;

            // If there's no postprocessor or the postprocessor doesn't require caching, forward results.
            IPostprocessor postprocessor = imageRequest.Postprocessor;
            if (postprocessor == null || postprocessor.PostprocessorCacheKey == null)
            {
                _inputProducer.ProduceResults(consumer, producerContext);
                return;
            }

            listener.OnProducerStart(requestId, ProducerName);
            ICacheKey cacheKey = _cacheKeyFactory.GetPostprocessedBitmapCacheKey(
                imageRequest, callerContext);

            CloseableReference<CloseableImage> cachedReference = _memoryCache.Get(cacheKey);
            var extraMap = default(Dictionary<string, string>);
            if (cachedReference != null)
            {
                extraMap = new Dictionary<string, string>()
                {
                    {  VALUE_FOUND, "true" }
                };

                listener.OnProducerFinishWithSuccess(
                    requestId,
                    ProducerName,
                    listener.RequiresExtraMap(requestId) ? 
                    new ReadOnlyDictionary<string, string>(extraMap) : 
                    null);

                consumer.OnProgressUpdate(1.0f);
                consumer.OnNewResult(cachedReference, true);
                cachedReference.Dispose();
            }
            else
            {
                bool isRepeatedProcessor = postprocessor.GetType() == typeof(IRepeatedPostprocessor);
                IConsumer<CloseableReference<CloseableImage>> cachedConsumer = 
                    new CachedPostprocessorConsumer(
                    consumer,
                    cacheKey,
                    isRepeatedProcessor,
                    _memoryCache);

                extraMap = new Dictionary<string, string>()
                {
                    {  VALUE_FOUND, "false" }
                };

                listener.OnProducerFinishWithSuccess(
                    requestId,
                    ProducerName,
                    listener.RequiresExtraMap(requestId) ?
                    new ReadOnlyDictionary<string, string>(extraMap) : 
                    null);

                _inputProducer.ProduceResults(cachedConsumer, producerContext);
            }
        }

        internal class CachedPostprocessorConsumer : 
            DelegatingConsumer<CloseableReference<CloseableImage>, CloseableReference<CloseableImage>> 
        {
            private readonly ICacheKey _cacheKey;
            private readonly bool _isRepeatedProcessor;
            private readonly IMemoryCache<ICacheKey, CloseableImage> _memoryCache;

            public CachedPostprocessorConsumer(
                IConsumer<CloseableReference<CloseableImage>> consumer,
                ICacheKey cacheKey,
                bool isRepeatedProcessor,
                IMemoryCache<ICacheKey, CloseableImage> memoryCache) :
                base(consumer)
            {
                _cacheKey = cacheKey;
                _isRepeatedProcessor = isRepeatedProcessor;
                _memoryCache = memoryCache;
            }

            protected override void OnNewResultImpl(
                CloseableReference<CloseableImage> newResult, bool isLast)
            {
                // ignore invalid intermediate results and forward the null result if last
                if (newResult == null)
                {
                    if (isLast)
                    {
                        Consumer.OnNewResult(null, true);
                    }

                    return;
                }

                // ignore intermediate results for non-repeated postprocessors
                if (!isLast && !_isRepeatedProcessor)
                {
                    return;
                }

                // cache and forward the new result
                CloseableReference<CloseableImage> newCachedResult =
                    _memoryCache.Cache(_cacheKey, newResult);

                try
                {
                    Consumer.OnProgressUpdate(1f);
                    Consumer.OnNewResult(
                        (newCachedResult != null) ? newCachedResult : newResult, isLast);
                }
                finally
                {
                    CloseableReference<CloseableImage>.CloseSafely(newCachedResult);
                }
            }
        }

        /// <summary>
        /// Gets the producer name.
        /// </summary>
        protected string ProducerName
        {
            get
            {
                return PRODUCER_NAME;
            }
        }
    }
}
