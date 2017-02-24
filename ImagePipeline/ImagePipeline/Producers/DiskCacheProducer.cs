using Cache.Common;
using FBCore.Common.Internal;
using ImagePipeline.Cache;
using ImagePipeline.Image;
using ImagePipeline.Request;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Disk cache producer.
    ///
    /// <para />This producer looks in the disk cache for the requested image.
    /// If the image is found, then it is passed to the consumer. If the image
    /// is not found, then the request is passed to the next producer in the
    /// sequence. Any results that the producer returns are passed to the
    /// consumer, and the last result is also put into the disk cache.
    ///
    /// <para />This implementation delegates disk cache requests to
    /// <see cref="BufferedDiskCache"/>.
    /// </summary>
    public class DiskCacheProducer : IProducer<EncodedImage>
    {
        internal const string PRODUCER_NAME = "DiskCacheProducer";
        internal const string VALUE_FOUND = "cached_value_found";

        private readonly BufferedDiskCache _defaultBufferedDiskCache;
        private readonly BufferedDiskCache _smallImageBufferedDiskCache;
        private readonly ICacheKeyFactory _cacheKeyFactory;
        private readonly IProducer<EncodedImage> _inputProducer;
        private readonly bool _chooseCacheByImageSize;
        private readonly int _forceSmallCacheThresholdBytes;

        /// <summary>
        /// Instantiates the <see cref="DiskCacheProducer"/>.
        /// </summary>
        public DiskCacheProducer(
            BufferedDiskCache defaultBufferedDiskCache,
            BufferedDiskCache smallImageBufferedDiskCache,
            ICacheKeyFactory cacheKeyFactory,
            IProducer<EncodedImage> inputProducer,
            int forceSmallCacheThresholdBytes)
        {
            _defaultBufferedDiskCache = defaultBufferedDiskCache;
            _smallImageBufferedDiskCache = smallImageBufferedDiskCache;
            _cacheKeyFactory = cacheKeyFactory;
            _inputProducer = inputProducer;
            _forceSmallCacheThresholdBytes = forceSmallCacheThresholdBytes;
            _chooseCacheByImageSize = (forceSmallCacheThresholdBytes > 0);
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
            ImageRequest imageRequest = producerContext.ImageRequest;
            if (!imageRequest.IsDiskCacheEnabled)
            {
                MaybeStartInputProducer(consumer, consumer, producerContext);
                return;
            }

            producerContext.Listener.OnProducerStart(producerContext.Id, PRODUCER_NAME);
            ICacheKey cacheKey = _cacheKeyFactory.GetEncodedCacheKey(
                imageRequest, producerContext.CallerContext);

            bool isSmallRequest = (imageRequest.CacheChoice == CacheChoice.SMALL);
            BufferedDiskCache preferredCache = isSmallRequest ?
                _smallImageBufferedDiskCache : 
                _defaultBufferedDiskCache;

            AtomicBoolean isCancelled = new AtomicBoolean(false);
            Task<EncodedImage> diskLookupTask;
            if (_chooseCacheByImageSize)
            {
                bool alreadyInSmall = _smallImageBufferedDiskCache.ContainsSync(cacheKey);
                bool alreadyInMain = _defaultBufferedDiskCache.ContainsSync(cacheKey);
                BufferedDiskCache firstCache;
                BufferedDiskCache secondCache;
                if (alreadyInSmall || !alreadyInMain)
                {
                    firstCache = _smallImageBufferedDiskCache;
                    secondCache = _defaultBufferedDiskCache;
                }
                else
                {
                    firstCache = _defaultBufferedDiskCache;
                    secondCache = _smallImageBufferedDiskCache;
                }

                diskLookupTask = firstCache.Get(cacheKey, isCancelled);
                diskLookupTask = diskLookupTask.ContinueWith(
                    task =>
                    {
                        if (IsTaskCancelled(task) || (!task.IsFaulted && task.Result != null))
                        {
                            return task;
                        }

                        return secondCache.Get(cacheKey, isCancelled);
                    },
                    TaskContinuationOptions.ExecuteSynchronously)
                    .Unwrap();
            }
            else
            {
                diskLookupTask = preferredCache.Get(cacheKey, isCancelled);
            }

            diskLookupTask.ContinueWith(
                task =>
                {
                    OnFinishDiskReads(
                        task,
                        consumer,
                        preferredCache,
                        cacheKey,
                        producerContext);
                },
                TaskContinuationOptions.ExecuteSynchronously);

            SubscribeTaskForRequestCancellation(isCancelled, producerContext);
        }

        private void OnFinishDiskReads(
          Task<EncodedImage> task,
          IConsumer<EncodedImage> consumer,
          BufferedDiskCache preferredCache,
          ICacheKey preferredCacheKey,
          IProducerContext producerContext)
        {
            string requestId = producerContext.Id;
            IProducerListener listener = producerContext.Listener;

            if (IsTaskCancelled(task))
            {
                listener.OnProducerFinishWithCancellation(requestId, PRODUCER_NAME, null);
                consumer.OnCancellation();
            }
            else if (task.IsFaulted)
            {
                listener.OnProducerFinishWithFailure(requestId, PRODUCER_NAME, task.Exception, null);
                MaybeStartInputProducer(
                    consumer,
                    new DiskCacheConsumer(
                        _defaultBufferedDiskCache,
                        _smallImageBufferedDiskCache,
                        _chooseCacheByImageSize,
                        _forceSmallCacheThresholdBytes,
                        consumer, 
                        preferredCache, 
                        preferredCacheKey),
                    producerContext);
            }
            else
            {
                EncodedImage cachedReference = task.Result;
                if (cachedReference != null)
                {
                    listener.OnProducerFinishWithSuccess(
                        requestId,
                        PRODUCER_NAME,
                        GetExtraMap(listener, requestId, true));

                    consumer.OnProgressUpdate(1);
                    consumer.OnNewResult(cachedReference, true);
                    cachedReference.Dispose();
                }
                else
                {
                    listener.OnProducerFinishWithSuccess(
                        requestId,
                        PRODUCER_NAME,
                        GetExtraMap(listener, requestId, false));

                    MaybeStartInputProducer(
                        consumer,
                        new DiskCacheConsumer(
                            _defaultBufferedDiskCache,
                            _smallImageBufferedDiskCache,
                            _chooseCacheByImageSize,
                            _forceSmallCacheThresholdBytes,
                            consumer,
                            preferredCache,
                            preferredCacheKey),
                        producerContext);
                }
            }
        }

        private static bool IsTaskCancelled(Task task)
        {
            return task.IsCanceled || 
                (task.IsFaulted && task.Exception.GetType() == typeof(OperationCanceledException));
        }

        private void MaybeStartInputProducer(
            IConsumer<EncodedImage> consumerOfDiskCacheProducer,
            IConsumer<EncodedImage> consumerOfInputProducer,
            IProducerContext producerContext)
        {
            if (producerContext.LowestPermittedRequestLevel >= RequestLevel.DISK_CACHE)
            {
                consumerOfDiskCacheProducer.OnNewResult(null, true);
                return;
            }

            _inputProducer.ProduceResults(consumerOfInputProducer, producerContext);
        }

        internal static IDictionary<string, string> GetExtraMap(
            IProducerListener listener,
            string requestId,
            bool valueFound)
        {
            if (!listener.RequiresExtraMap(requestId))
            {
                return null;
            }

            var extraMap = new Dictionary<string, string>()
            {
                {  VALUE_FOUND, valueFound.ToString() }
            };

            return new ReadOnlyDictionary<string, string>(extraMap);
        }

        private void SubscribeTaskForRequestCancellation(
            AtomicBoolean isCancelled,
            IProducerContext producerContext)
        {
            producerContext.AddCallbacks(
                new BaseProducerContextCallbacks(
                    () =>
                    {
                        isCancelled.Value = true;
                    },
                    () => { },
                    () => { },
                    () => { }));
        }

        /// <summary>
        /// Consumer that consumes results from next producer in the sequence.
        ///
        /// <para />The consumer puts the last result received into disk
        /// cache, and passes all results (success or failure) down to the
        /// next consumer.
        /// </summary>
        private class DiskCacheConsumer : DelegatingConsumer<EncodedImage, EncodedImage>
        {
            private readonly BufferedDiskCache _defaultBufferedDiskCache;
            private readonly BufferedDiskCache _smallImageBufferedDiskCache;
            private readonly bool _chooseCacheByImageSize;
            private readonly int _forceSmallCacheThresholdBytes;
            private readonly BufferedDiskCache _cache;
            private readonly ICacheKey _cacheKey;

            /// <summary>
            /// Instantiates the <see cref="DiskCacheConsumer"/>.
            /// </summary>
            internal DiskCacheConsumer(
                BufferedDiskCache defaultBufferedDiskCache,
                BufferedDiskCache smallImageBufferedDiskCache,
                bool chooseCacheByImageSize,
                int forceSmallCacheThresholdBytes,
                IConsumer<EncodedImage> consumer,
                BufferedDiskCache cache,
                ICacheKey cacheKey) : 
                base(consumer)
            {
                _defaultBufferedDiskCache = defaultBufferedDiskCache;
                _smallImageBufferedDiskCache = smallImageBufferedDiskCache;
                _chooseCacheByImageSize = chooseCacheByImageSize;
                _forceSmallCacheThresholdBytes = forceSmallCacheThresholdBytes;
                _cache = cache;
                _cacheKey = cacheKey;
            }

            protected override void OnNewResultImpl(EncodedImage newResult, bool isLast)
            {
                if (newResult != null && isLast)
                {
                    if (_chooseCacheByImageSize)
                    {
                        int size = newResult.Size;
                        if (size > 0 && size < _forceSmallCacheThresholdBytes)
                        {
                            _smallImageBufferedDiskCache.Put(_cacheKey, newResult);
                        }
                        else
                        {
                            _defaultBufferedDiskCache.Put(_cacheKey, newResult);
                        }
                    }
                    else
                    {
                        _cache.Put(_cacheKey, newResult);
                    }
                }

                Consumer.OnNewResult(newResult, isLast);
            }
        }
    }
}
