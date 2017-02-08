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
    public class BitmapMemoryCacheProducer : IProducer<CloseableReference<CloseableImage>>
    {
        internal const string PRODUCER_NAME = "BitmapMemoryCacheProducer";
        internal const string VALUE_FOUND = "cached_value_found";

        private readonly IMemoryCache<ICacheKey, CloseableImage> _memoryCache;
        private readonly ICacheKeyFactory _cacheKeyFactory;
        private readonly IProducer<CloseableReference<CloseableImage>> _inputProducer;

        /// <summary>
        /// Instantiates the <see cref="BitmapMemoryCacheProducer"/>
        /// </summary>
        public BitmapMemoryCacheProducer(
            IMemoryCache<ICacheKey, CloseableImage> memoryCache,
            ICacheKeyFactory cacheKeyFactory,
            IProducer<CloseableReference<CloseableImage>> inputProducer)
        {
            _memoryCache = memoryCache;
            _cacheKeyFactory = cacheKeyFactory;
            _inputProducer = inputProducer;
        }

        /// <summary>
        /// Start producing results for given context. Provided consumer is notified 
        /// whenever progress is made (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(
            IConsumer<CloseableReference<CloseableImage>> consumer,
            IProducerContext producerContext)
        {
            IProducerListener listener = producerContext.Listener;
            string requestId = producerContext.Id;
            listener.OnProducerStart(requestId, ProducerName);
            ImageRequest imageRequest = producerContext.ImageRequest;
            object callerContext = producerContext.CallerContext;
            ICacheKey cacheKey = _cacheKeyFactory.GetBitmapCacheKey(imageRequest, callerContext);
            IDictionary<string, string> extraMap = default(IDictionary<string, string>);

            CloseableReference<CloseableImage> cachedReference = _memoryCache.Get(cacheKey);

            if (cachedReference != null)
            {
                bool isFinal = cachedReference.Get().QualityInfo.IsOfFullQuality;
                if (isFinal)
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

                    consumer.OnProgressUpdate(1f);
                }

                consumer.OnNewResult(cachedReference, isFinal);
                cachedReference.Dispose();
                if (isFinal)
                {
                    return;
                }
            }

            if (producerContext.LowestPermittedRequestLevel >= RequestLevel.BITMAP_MEMORY_CACHE)
            {
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

                consumer.OnNewResult(null, true);
                return;
            }

            extraMap = new Dictionary<string, string>()
            {
                {  VALUE_FOUND, "false" }
            };

            IConsumer<CloseableReference<CloseableImage>> wrappedConsumer = 
                WrapConsumer(consumer, cacheKey);

            listener.OnProducerFinishWithSuccess(
                requestId,
                ProducerName,
                listener.RequiresExtraMap(requestId) ?
                new ReadOnlyDictionary<string, string>(extraMap) : 
                null);

            _inputProducer.ProduceResults(wrappedConsumer, producerContext);
        }

        /// <summary>
        /// Wraps the target consumer by the BitmapMemoryCacheConsumer
        /// </summary>
        protected virtual IConsumer<CloseableReference<CloseableImage>> WrapConsumer(
            IConsumer<CloseableReference<CloseableImage>> consumer,
            ICacheKey cacheKey)
        {
            return new BitmapMemoryCacheConsumer(
                _memoryCache,
                consumer,
                cacheKey);
        }

        /// <summary>
        /// Gets the producer name
        /// </summary>
        protected virtual string ProducerName
        {
            get
            {
                return PRODUCER_NAME;
            }
        }

        private class BitmapMemoryCacheConsumer : DelegatingConsumer<
            CloseableReference<CloseableImage>, 
            CloseableReference<CloseableImage>>
        {
            private IMemoryCache<ICacheKey, CloseableImage> _memoryCache;
            private ICacheKey _cacheKey;

            internal BitmapMemoryCacheConsumer(
                IMemoryCache<ICacheKey, CloseableImage> memoryCache,
                IConsumer<CloseableReference<CloseableImage>> consumer,
                ICacheKey cacheKey) : 
                base(consumer)
            {
                _memoryCache = memoryCache;
                _cacheKey = cacheKey;
            }

            protected override void OnNewResultImpl(
                CloseableReference<CloseableImage> newResult, 
                bool isLast)
            {
                // Ignore invalid intermediate results and forward the null result if last
                if (newResult == null)
                {
                    if (isLast)
                    {
                        Consumer.OnNewResult(null, true);
                    }

                    return;
                }

                // Stateful results cannot be cached and are just forwarded
                if (newResult.Get().Stateful)
                {
                    Consumer.OnNewResult(newResult, isLast);
                    return;
                }

                // If the intermediate result is not of a better quality than the cached result,
                // forward the already cached result and don't cache the new result.
                if (!isLast)
                {
                    CloseableReference<CloseableImage> currentCachedResult = _memoryCache.Get(_cacheKey);
                    if (currentCachedResult != null)
                    {
                        try
                        {
                            IQualityInfo newInfo = newResult.Get().QualityInfo;
                            IQualityInfo cachedInfo = currentCachedResult.Get().QualityInfo;
                            if (cachedInfo.IsOfFullQuality || cachedInfo.Quality >= newInfo.Quality)
                            {
                                Consumer.OnNewResult(currentCachedResult, false);
                                return;
                            }
                        }
                        finally
                        {
                            CloseableReference<CloseableImage>.CloseSafely(currentCachedResult);
                        }
                    }
                }

                // Cache and forward the new result
                CloseableReference<CloseableImage> newCachedResult = 
                    _memoryCache.Cache(_cacheKey, newResult);

                try
                {
                    if (isLast)
                    {
                        Consumer.OnProgressUpdate(1f);
                    }

                    Consumer.OnNewResult(
                        (newCachedResult != null) ? newCachedResult : newResult, isLast);
                }
                finally
                {
                    CloseableReference<CloseableImage>.CloseSafely(newCachedResult);
                }
            }
        }
    }
}
