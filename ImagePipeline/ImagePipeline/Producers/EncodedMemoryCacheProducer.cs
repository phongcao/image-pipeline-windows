using Cache.Common;
using FBCore.Common.References;
using ImagePipeline.Cache;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using ImagePipeline.Request;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Memory cache producer for the encoded memory cache.
    /// </summary>
    public class EncodedMemoryCacheProducer : IProducer<EncodedImage>
    {
        internal const string PRODUCER_NAME = "EncodedMemoryCacheProducer";
        internal const string VALUE_FOUND = "cached_value_found";

        private readonly IMemoryCache<ICacheKey, IPooledByteBuffer> _memoryCache;
        private readonly ICacheKeyFactory _cacheKeyFactory;
        private readonly IProducer<EncodedImage> _inputProducer;

        /// <summary>
        /// Instantiates the <see cref="EncodedMemoryCacheProducer"/>
        /// </summary>
        public EncodedMemoryCacheProducer(
            IMemoryCache<ICacheKey, IPooledByteBuffer> memoryCache,
            ICacheKeyFactory cacheKeyFactory,
            IProducer<EncodedImage> inputProducer)
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
            IConsumer<EncodedImage> consumer,
            IProducerContext producerContext)
        {
            string requestId = producerContext.Id;
            IProducerListener listener = producerContext.Listener;
            listener.OnProducerStart(requestId, PRODUCER_NAME);
            ImageRequest imageRequest = producerContext.ImageRequest;
            ICacheKey cacheKey = _cacheKeyFactory.GetEncodedCacheKey(
                imageRequest, producerContext.CallerContext);

            CloseableReference<IPooledByteBuffer> cachedReference = _memoryCache.Get(cacheKey);
            IDictionary<string, string> extraMap = default(IDictionary<string, string>);

            try
            {
                if (cachedReference != null)
                {
                    EncodedImage cachedEncodedImage = new EncodedImage(cachedReference);

                    try
                    {
                        extraMap = new Dictionary<string, string>()
                        {
                            {  VALUE_FOUND, "true" }
                        };

                        listener.OnProducerFinishWithSuccess(
                            requestId,
                            PRODUCER_NAME,
                            listener.RequiresExtraMap(requestId) ?
                            new ReadOnlyDictionary<string, string>(extraMap) :
                            null);
                        consumer.OnProgressUpdate(1f);
                        consumer.OnNewResult(cachedEncodedImage, true);
                        return;
                    }
                    finally
                    {
                        EncodedImage.CloseSafely(cachedEncodedImage);
                    }
                }

                if (producerContext.LowestPermittedRequestLevel >= RequestLevel.ENCODED_MEMORY_CACHE)
                {
                    extraMap = new Dictionary<string, string>()
                    {
                        {  VALUE_FOUND, "false" }
                    };

                    listener.OnProducerFinishWithSuccess(
                        requestId,
                        PRODUCER_NAME,
                        listener.RequiresExtraMap(requestId) ?
                            new ReadOnlyDictionary<string, string>(extraMap) :
                            null);

                    consumer.OnNewResult(null, true);
                    return;
                }

                IConsumer<EncodedImage> consumerOfInputProducer = 
                    new EncodedMemoryCacheConsumer(_memoryCache, consumer, cacheKey);

                extraMap = new Dictionary<string, string>()
                {
                    {  VALUE_FOUND, "false" }
                };

                listener.OnProducerFinishWithSuccess(
                      requestId,
                      PRODUCER_NAME,
                      listener.RequiresExtraMap(requestId) ?
                            new ReadOnlyDictionary<string, string>(extraMap) :
                            null);

                _inputProducer.ProduceResults(consumerOfInputProducer, producerContext);
            }
            finally
            {
                CloseableReference<IPooledByteBuffer>.CloseSafely(cachedReference);
            }
        }

        private class EncodedMemoryCacheConsumer : DelegatingConsumer<EncodedImage, EncodedImage>
        {
            private IMemoryCache<ICacheKey, IPooledByteBuffer> _memoryCache;
            private ICacheKey _cacheKey;

            internal EncodedMemoryCacheConsumer(
                IMemoryCache<ICacheKey, IPooledByteBuffer> memoryCache,
                IConsumer<EncodedImage> consumer,
                ICacheKey cacheKey) :
                base(consumer)
            {
                _memoryCache = memoryCache;
                _cacheKey = cacheKey;
            }

            protected override void OnNewResultImpl(EncodedImage newResult, bool isLast)
            {
                // Intermediate or null results are not cached, so we just forward them
                if (!isLast || newResult == null)
                {
                    Consumer.OnNewResult(newResult, isLast);
                    return;
                }

                // Cache and forward the last result
                CloseableReference<IPooledByteBuffer> reference = newResult.GetByteBufferRef();
                if (reference != null) 
                {
                    CloseableReference<IPooledByteBuffer> cachedResult;

                    try
                    {
                        cachedResult = _memoryCache.Cache(_cacheKey, reference);
                    }
                    finally
                    {
                        CloseableReference<IPooledByteBuffer>.CloseSafely(reference);
                    }

                    if (cachedResult != null)
                    {
                        EncodedImage cachedEncodedImage;
                        try
                        {
                            cachedEncodedImage = new EncodedImage(cachedResult);
                            cachedEncodedImage.CopyMetaDataFrom(newResult);
                        }
                        finally
                        {
                            CloseableReference<IPooledByteBuffer>.CloseSafely(cachedResult);
                        }
                        try
                        {
                            Consumer.OnProgressUpdate(1f);
                            Consumer.OnNewResult(cachedEncodedImage, true);
                            return;
                        }
                        finally
                        {
                            EncodedImage.CloseSafely(cachedEncodedImage);
                        }
                    }
                }

                Consumer.OnNewResult(newResult, true);
            }
        }
    }
}
