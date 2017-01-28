using Cache.Common;
using FBCore.Common.Internal;
using FBCore.Common.References;
using FBCore.Common.Util;
using FBCore.DataSource;
using ImagePipeline.Cache;
using ImagePipeline.Common;
using ImagePipeline.Datasource;
using ImagePipeline.Image;
using ImagePipeline.Listener;
using ImagePipeline.Memory;
using ImagePipeline.Producers;
using ImagePipeline.Request;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ImagePipeline.Core
{
    /// <summary>
    /// The entry point for the image pipeline.
    /// </summary>
    public class ImagePipeline
    {
        private static readonly OperationCanceledException PREFETCH_EXCEPTION =
            new OperationCanceledException("Prefetching is not enabled");

        private readonly ProducerSequenceFactory _producerSequenceFactory;
        private readonly IRequestListener _requestListener;
        private readonly ISupplier<bool> _isPrefetchEnabledSupplier;
        private readonly IMemoryCache<ICacheKey, CloseableImage> _bitmapMemoryCache;
        private readonly IMemoryCache<ICacheKey, IPooledByteBuffer> _encodedMemoryCache;
        private readonly BufferedDiskCache _mainBufferedDiskCache;
        private readonly BufferedDiskCache _smallImageBufferedDiskCache;
        private readonly ICacheKeyFactory _cacheKeyFactory;
        private readonly ThreadHandoffProducerQueue _threadHandoffProducerQueue;
        private long _idCounter;

        /// <summary>
        /// Instantiates the <see cref="ImagePipeline"/>.
        /// </summary>
        /// <param name="producerSequenceFactory">The factory that creates all producer sequences.</param>
        /// <param name="requestListeners">The listeners for the image requests.</param>
        /// <param name="isPrefetchEnabledSupplier">The supplier for enabling prefetch.</param>
        /// <param name="bitmapMemoryCache">The memory cache for CloseableImage.</param>
        /// <param name="encodedMemoryCache">The memory cache for IPooledByteBuffer.</param>
        /// <param name="mainBufferedDiskCache">The default buffered disk cache.</param>
        /// <param name="smallImageBufferedDiskCache">The buffered disk cache used for small images.</param>
        /// <param name="cacheKeyFactory">The factory that creates cache keys for the pipeline.</param>
        /// <param name="threadHandoffProducerQueue">Move further computation to different thread.</param>
        public ImagePipeline(
            ProducerSequenceFactory producerSequenceFactory,
            HashSet<IRequestListener> requestListeners,
            ISupplier<bool> isPrefetchEnabledSupplier,
            IMemoryCache<ICacheKey, CloseableImage> bitmapMemoryCache,
            IMemoryCache<ICacheKey, IPooledByteBuffer> encodedMemoryCache,
            BufferedDiskCache mainBufferedDiskCache,
            BufferedDiskCache smallImageBufferedDiskCache,
            ICacheKeyFactory cacheKeyFactory,
            ThreadHandoffProducerQueue threadHandoffProducerQueue)
        {
            _idCounter = 0;
            _producerSequenceFactory = producerSequenceFactory;
            _requestListener = new ForwardingRequestListener(requestListeners);
            _isPrefetchEnabledSupplier = isPrefetchEnabledSupplier;
            _bitmapMemoryCache = bitmapMemoryCache;
            _encodedMemoryCache = encodedMemoryCache;
            _mainBufferedDiskCache = mainBufferedDiskCache;
            _smallImageBufferedDiskCache = smallImageBufferedDiskCache;
            _cacheKeyFactory = cacheKeyFactory;
            _threadHandoffProducerQueue = threadHandoffProducerQueue;
        }

        /// <summary>
        /// Generates unique id for RequestFuture.
        ///
        /// @return unique id
        /// </summary>
        private string GenerateUniqueFutureId()
        {
            return Interlocked.Increment(ref _idCounter).ToString();
        }

        /// <summary>
        /// @deprecated Use GetDataSourceSupplier(ImageRequest imageRequest, object callerContext,
        /// RequestLevel requestLevel).
        /// instead.
        /// </summary>
        public ISupplier<IDataSource<CloseableReference<CloseableImage>>> GetDataSourceSupplier(
            ImageRequest imageRequest,
            object callerContext,
            bool bitmapCacheOnly)
        {
            RequestLevel requestLevel = bitmapCacheOnly ?
                new RequestLevel(RequestLevel.BITMAP_MEMORY_CACHE) :
                new RequestLevel(RequestLevel.FULL_FETCH);

            return GetDataSourceSupplier(imageRequest, callerContext, requestLevel);
        }

        /// <summary>
        /// Returns a DataSource supplier that will on get submit the request for execution 
        /// and return a IDataSource representing the pending results of the task.
        ///
        /// <param name="imageRequest">The request to submit (what to execute).</param>
        /// <param name="callerContext">The caller context of the caller of data source supplier.</param>
        /// <param name="requestLevel">Which level to look down until for the image.</param>
        /// @return a IDataSource representing pending results and completion of the request.
        /// </summary>
        public ISupplier<IDataSource<CloseableReference<CloseableImage>>> GetDataSourceSupplier(
            ImageRequest imageRequest,
            object callerContext,
            RequestLevel requestLevel)
        {
            return new SupplierImpl<IDataSource<CloseableReference<CloseableImage>>>(
                () =>
                {
                    return FetchDecodedImage(imageRequest, callerContext, requestLevel);
                },
                () =>
                {
                    return $"{ base.ToString() }{{uri={ imageRequest.SourceUri.ToString() }}}";
                });
        }

        /// <summary>
        /// Returns a DataSource supplier that will on get submit the request for execution 
        /// and return a IDataSource representing the pending results of the task.
        ///
        /// <param name="imageRequest">The request to submit (what to execute).</param>
        /// <param name="callerContext">The caller context of the caller of data source supplier.</param>
        /// @return a IDataSource representing pending results and completion of the request.
        /// </summary>
        public ISupplier<IDataSource<CloseableReference<IPooledByteBuffer>>> 
            GetEncodedImageDataSourceSupplier(
                ImageRequest imageRequest,
                object callerContext)
        {
            return new SupplierImpl<IDataSource<CloseableReference<IPooledByteBuffer>>>(
                () =>
                {
                    return FetchEncodedImage(imageRequest, callerContext);
                },
                () =>
                {
                    return $"{ base.ToString() }{{uri={ imageRequest.SourceUri.ToString() }}}";
                });
        }

        /// <summary>
        /// Submits a request for bitmap cache lookup.
        ///
        /// <param name="imageRequest">The request to submit.</param>
        /// <param name="callerContext">The caller context for image request.</param>
        /// @return a IDataSource representing the image.
        /// </summary>
        public IDataSource<CloseableReference<CloseableImage>> FetchImageFromBitmapCache(
            ImageRequest imageRequest,
            object callerContext)
        {
            return FetchDecodedImage(
                imageRequest,
                callerContext,
                new RequestLevel(RequestLevel.BITMAP_MEMORY_CACHE));
        }

        /// <summary>
        /// Submits a request for execution and returns a DataSource representing the 
        /// pending decoded image(s).
        /// <para />The returned DataSource must be closed once the client has finished with it.
        /// <param name="imageRequest">The request to submit.</param>
        /// <param name="callerContext">The caller context for image request.</param>
        /// @return a IDataSource representing the pending decoded image(s).
        /// </summary>
        public IDataSource<CloseableReference<CloseableImage>> FetchDecodedImage(
            ImageRequest imageRequest,
            object callerContext)
        {
            return FetchDecodedImage(
                imageRequest, 
                callerContext, 
                new RequestLevel(RequestLevel.FULL_FETCH));
        }

        /// <summary>
        /// Submits a request for execution and returns a DataSource representing the 
        /// pending decoded image(s).
        /// <para />The returned DataSource must be closed once the client has finished with it.
        /// <param name="imageRequest">The request to submit.</param>
        /// <param name="callerContext">The caller context for image request.</param>
        /// <param name="lowestPermittedRequestLevelOnSubmit">The lowest request level permitted for image request.</param>
        /// @return a IDataSource representing the pending decoded image(s).
        /// </summary>
        public IDataSource<CloseableReference<CloseableImage>> FetchDecodedImage(
            ImageRequest imageRequest,
            object callerContext,
            RequestLevel lowestPermittedRequestLevelOnSubmit)
        {
            try
            {
                IProducer<CloseableReference<CloseableImage>> producerSequence =
                    _producerSequenceFactory.GetDecodedImageProducerSequence(imageRequest);

                return SubmitFetchRequest(
                    producerSequence,
                    imageRequest,
                    lowestPermittedRequestLevelOnSubmit,
                    callerContext);
            }
            catch (Exception exception)
            {
                return DataSources.ImmediateFailedDataSource<CloseableReference<CloseableImage>>(exception);
            }
        }

        /// <summary>
        /// Submits a request for execution and returns a DataSource representing the pending 
        /// encoded image(s).
        ///
        /// <para /> The ResizeOptions in the imageRequest will be ignored for this fetch.
        ///
        /// <para />The returned DataSource must be closed once the client has finished with it.
        ///
        /// <param name="imageRequest">The request to submit.</param>
        /// <param name="callerContext">The caller context for image request.</param>
        /// @return a IDataSource representing the pending encoded image(s).
        /// </summary>
        public IDataSource<CloseableReference<IPooledByteBuffer>> FetchEncodedImage(
            ImageRequest imageRequest,
            object callerContext)
        {
            Preconditions.CheckNotNull(imageRequest.SourceUri);

            try
            {
                IProducer<CloseableReference<IPooledByteBuffer>> producerSequence =
                    _producerSequenceFactory.GetEncodedImageProducerSequence(imageRequest);

                // The resize options are used to determine whether images are going to be 
                // downsampled during decode or not. For the case where the image has to be 
                // downsampled and it's a local image it will be kept as a FileStream until 
                // decoding instead of reading it in memory. Since this method returns an 
                // encoded image, it should always be read into memory. Therefore, the resize 
                // options are ignored to avoid treating the image as if it was to be downsampled
                // during decode.
                if (imageRequest.ResizeOptions != null)
                {
                    imageRequest = ImageRequestBuilder.FromRequest(imageRequest)
                        .SetResizeOptions(null)
                        .Build();
                }

                return SubmitFetchRequest(
                    producerSequence,
                    imageRequest,
                    new RequestLevel(RequestLevel.FULL_FETCH),
                    callerContext);
            }
            catch (Exception exception)
            {
                return DataSources.ImmediateFailedDataSource<CloseableReference<IPooledByteBuffer>>(exception);
            }
        }

        /// <summary>
        /// Submits a request for prefetching to the bitmap cache.
        /// <param name="imageRequest">The request to submit.</param>
        /// <param name="callerContext">The caller context for image request.</param>
        /// @return a IDataSource that can safely be ignored.
        /// </summary>
        public IDataSource<object> PrefetchToBitmapCache(
            ImageRequest imageRequest,
            object callerContext)
        {
            if (!_isPrefetchEnabledSupplier.Get())
            {
                return DataSources.ImmediateFailedDataSource<object>(PREFETCH_EXCEPTION);
            }
            try
            {
                IProducer<object> producerSequence =
                    _producerSequenceFactory.GetDecodedImagePrefetchProducerSequence(imageRequest);

                return SubmitPrefetchRequest(
                    producerSequence,
                    imageRequest,
                    new RequestLevel(RequestLevel.FULL_FETCH),
                    callerContext,
                    Priority.MEDIUM);
            }
            catch (Exception exception)
            {
                return DataSources.ImmediateFailedDataSource<object>(exception);
            }
        }

        /// <summary>
        /// Submits a request for prefetching to the disk cache with a default priority.
        /// <param name="imageRequest">The request to submit.</param>
        /// <param name="callerContext">The caller context for image request.</param>
        /// @return a DataSource that can safely be ignored.
        /// </summary>
        public IDataSource<object> PrefetchToDiskCache(
            ImageRequest imageRequest,
            object callerContext)
        {
            return PrefetchToDiskCache(imageRequest, callerContext, Priority.MEDIUM);
        }

        /// <summary>
        /// Submits a request for prefetching to the disk cache.
        /// <param name="imageRequest">The request to submit.</param>
        /// <param name="callerContext">The caller context for image request.</param>
        /// <param name="priority">Custom priority for the fetch.</param>
        /// @return a DataSource that can safely be ignored.
        /// </summary>
        public IDataSource<object> PrefetchToDiskCache(
            ImageRequest imageRequest,
            object callerContext,
            int priority)
        {
            if (!_isPrefetchEnabledSupplier.Get())
            {
                return DataSources.ImmediateFailedDataSource<object>(PREFETCH_EXCEPTION);
            }

            try
            {
                IProducer<object> producerSequence =
                    _producerSequenceFactory.GetEncodedImagePrefetchProducerSequence(imageRequest);

                return SubmitPrefetchRequest(
                    producerSequence,
                    imageRequest,
                    new RequestLevel(RequestLevel.FULL_FETCH),
                    callerContext,
                    priority);
            }
            catch (Exception exception)
            {
                return DataSources.ImmediateFailedDataSource<object>(exception);
            }
        }

        /// <summary>
        /// Removes all images with the specified <see cref="Uri"/> from memory cache.
        /// <param name="uri">The uri of the image to evict.</param>
        /// </summary>
        public void EvictFromMemoryCache(Uri uri)
        {
            Predicate<ICacheKey> predicate = PredicateForUri(uri);
            _bitmapMemoryCache.RemoveAll(predicate);
            _encodedMemoryCache.RemoveAll(predicate);
        }

        /// <summary>
        /// <para />If you have supplied your own cache key factory when configuring the 
        /// pipeline, this method may not work correctly. It will only work if the custom 
        /// factory builds the cache key entirely from the URI. If that is not the case, 
        /// use EvictFromDiskCache(ImageRequest).
        /// <param name="uri">The uri of the image to evict.</param>
        /// </summary>
        public void EvictFromDiskCache(Uri uri)
        {
            EvictFromDiskCache(ImageRequest.FromUri(uri));
        }

        /// <summary>
        /// Removes all images with the specified <see cref="Uri"/> from disk cache.
        ///
        /// <param name="imageRequest">The imageRequest for the image to evict from disk cache.</param>
        /// </summary>
        public void EvictFromDiskCache(ImageRequest imageRequest)
        {
            ICacheKey cacheKey = _cacheKeyFactory.GetEncodedCacheKey(imageRequest, null);
            _mainBufferedDiskCache.Remove(cacheKey);
            _smallImageBufferedDiskCache.Remove(cacheKey);
        }

        /// <summary>
        /// <para />If you have supplied your own cache key factory when configuring the pipeline, 
        /// this method may not work correctly. It will only work if the custom factory builds the 
        /// cache key entirely from the URI. If that is not the case, use EvictFromMemoryCache(Uri)
        /// and EvictFromDiskCache(ImageRequest) separately.
        /// <param name="uri">The uri of the image to evict.</param>
        /// </summary>
        public void EvictFromCache(Uri uri)
        {
            EvictFromMemoryCache(uri);
            EvictFromDiskCache(uri);
        }

        /// <summary>
        /// Clear the memory caches.
        /// </summary>
        public void ClearMemoryCaches()
        {
            Predicate<ICacheKey> allPredicate = (key => true);
            _bitmapMemoryCache.RemoveAll(allPredicate);
            _encodedMemoryCache.RemoveAll(allPredicate);
        }

        /// <summary>
        /// Clear disk caches.
        /// </summary>
        public void ClearDiskCaches()
        {
            _mainBufferedDiskCache.ClearAll();
            _smallImageBufferedDiskCache.ClearAll();
        }

        /// <summary>
        /// Clear all the caches (memory and disk).
        /// </summary>
        public void ClearCaches()
        {
            ClearMemoryCaches();
            ClearDiskCaches();
        }

        /// <summary>
        /// Returns whether the image is stored in the bitmap memory cache.
        ///
        /// <param name="uri">The uri for the image to be looked up.</param>
        /// @return true if the image was found in the bitmap memory cache, false otherwise.
        /// </summary>
        public bool IsInBitmapMemoryCache(Uri uri)
        {
            if (uri == null)
            {
                return false;
            }

            Predicate<ICacheKey> bitmapCachePredicate = PredicateForUri(uri);
            return _bitmapMemoryCache.Contains(bitmapCachePredicate);
        }

        /// <summary>
        /// @return The Bitmap MemoryCache.
        /// </summary>
        public IMemoryCache<ICacheKey, CloseableImage> GetBitmapMemoryCache()
        {
            return _bitmapMemoryCache;
        }

        /// <summary>
        /// Returns whether the image is stored in the bitmap memory cache.
        ///
        /// <param name="imageRequest">The imageRequest for the image to be looked up.</param>
        /// @return true if the image was found in the bitmap memory cache, false otherwise.
        /// </summary>
        public bool IsInBitmapMemoryCache(ImageRequest imageRequest)
        {
            if (imageRequest == null)
            {
                return false;
            }

            ICacheKey cacheKey = _cacheKeyFactory.GetBitmapCacheKey(imageRequest, null);
            CloseableReference<CloseableImage> reference = _bitmapMemoryCache.Get(cacheKey);

            try
            {
                return CloseableReference<CloseableImage>.IsValid(reference);
            }
            finally
            {
                CloseableReference<CloseableImage>.CloseSafely(reference);
            }
        }

        /// <summary>
        /// Returns whether the image is stored in the disk cache.
        /// Performs disk cache check synchronously. It is not recommended to use this
        /// unless you know what exactly you are doing. Disk cache check is a costly operation,
        /// the call will block the caller thread until the cache check is completed.
        ///
        /// <param name="uri">The uri for the image to be looked up.</param>
        /// @return true if the image was found in the disk cache, false otherwise.
        /// </summary>
        public bool IsInDiskCacheSync(Uri uri)
        {
            return IsInDiskCacheSync(uri, CacheChoice.SMALL) ||
                IsInDiskCacheSync(uri, CacheChoice.DEFAULT);
        }

        /// <summary>
        /// Returns whether the image is stored in the disk cache.
        /// Performs disk cache check synchronously. It is not recommended to use this
        /// unless you know what exactly you are doing. Disk cache check is a costly operation,
        /// the call will block the caller thread until the cache check is completed.
        ///
        /// <param name="uri">The uri for the image to be looked up.</param>
        /// <param name="cacheChoice">The cacheChoice for the cache to be looked up.</param>
        /// @return true if the image was found in the disk cache, false otherwise.
        /// </summary>
        public bool IsInDiskCacheSync(Uri uri, CacheChoice cacheChoice)
        {
            ImageRequest imageRequest = ImageRequestBuilder
                .NewBuilderWithSource(uri)
                .SetCacheChoice(cacheChoice)
                .Build();

            return IsInDiskCacheSync(imageRequest);
        }

        /// <summary>
        /// Performs disk cache check synchronously. It is not recommended to use this
        /// unless you know what exactly you are doing. Disk cache check is a costly operation,
        /// the call will block the caller thread until the cache check is completed.
        /// <param name="imageRequest">The imageRequest for the image to be looked up.</param>
        /// @return true if the image was found in the disk cache, false otherwise.
        /// </summary>
        public bool IsInDiskCacheSync(ImageRequest imageRequest)
        {
            ICacheKey cacheKey = _cacheKeyFactory.GetEncodedCacheKey(imageRequest, null);
            CacheChoice cacheChoice = imageRequest.CacheChoice;

            switch (cacheChoice)
            {
                case CacheChoice.DEFAULT:
                    return _mainBufferedDiskCache.DiskCheckSync(cacheKey);

                case CacheChoice.SMALL:
                    return _smallImageBufferedDiskCache.DiskCheckSync(cacheKey);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns whether the image is stored in the disk cache.
        ///
        /// <para />If you have supplied your own cache key factory when configuring the pipeline, 
        /// this method may not work correctly. It will only work if the custom factory builds the 
        /// cache key entirely from the URI. If that is not the case, use IsInDiskCache(ImageRequest).
        ///
        /// <param name="uri">The uri for the image to be looked up.</param>
        /// @return true if the image was found in the disk cache, false otherwise.
        /// </summary>
        public IDataSource<bool> IsInDiskCache(Uri uri)
        {
            return IsInDiskCache(ImageRequest.FromUri(uri));
        }

        /// <summary>
        /// Returns whether the image is stored in the disk cache.
        ///
        /// <param name="imageRequest">The imageRequest for the image to be looked up.</param>
        /// @return true if the image was found in the disk cache, false otherwise.
        /// </summary>
        public IDataSource<bool> IsInDiskCache(ImageRequest imageRequest)
        {
            ICacheKey cacheKey = _cacheKeyFactory.GetEncodedCacheKey(imageRequest, null);
            SimpleDataSource<bool> dataSource = SimpleDataSource<bool>.Create();
            _mainBufferedDiskCache.Contains(cacheKey)
                .ContinueWith(
                task =>
                {
                    if (!task.IsCanceled && !task.IsFaulted && task.Result)
                    {
                        return Task.FromResult(true);
                    }

                    return _smallImageBufferedDiskCache.Contains(cacheKey);
                },
                TaskContinuationOptions.ExecuteSynchronously)
                .Unwrap()
                .ContinueWith(
                task =>
                {
                    dataSource.SetResult(!task.IsCanceled && !task.IsFaulted && task.Result);
                },
                TaskContinuationOptions.ExecuteSynchronously);

            return dataSource;
        }

        private IDataSource<CloseableReference<T>> SubmitFetchRequest<T>(
            IProducer<CloseableReference<T>> producerSequence,
            ImageRequest imageRequest,
            RequestLevel lowestPermittedRequestLevelOnSubmit,
            object callerContext)
        {
            IRequestListener requestListener = GetRequestListenerForRequest(imageRequest);

            try
            {
                RequestLevel lowestPermittedRequestLevel = RequestLevel.GetMax(
                    imageRequest.LowestPermittedRequestLevel,
                    lowestPermittedRequestLevelOnSubmit);

                SettableProducerContext settableProducerContext = new SettableProducerContext(
                    imageRequest,
                    GenerateUniqueFutureId(),
                    requestListener,
                    callerContext,
                    lowestPermittedRequestLevel.Value,
                    /* isPrefetch */ false,
                    imageRequest.IsProgressiveRenderingEnabled ||
                        !UriUtil.IsNetworkUri(imageRequest.SourceUri),
                    imageRequest.Priority);

                return CloseableProducerToDataSourceAdapter<T>.Create(
                    producerSequence,
                    settableProducerContext,
                    requestListener);
            }
            catch (Exception exception)
            {
                return DataSources.ImmediateFailedDataSource<CloseableReference<T>>(exception);
            }
        }

        private IDataSource<object> SubmitPrefetchRequest(
            IProducer<object> producerSequence,
            ImageRequest imageRequest,
            RequestLevel lowestPermittedRequestLevelOnSubmit,
            object callerContext,
            int priority)
        {
            IRequestListener requestListener = GetRequestListenerForRequest(imageRequest);

            try
            {
                RequestLevel lowestPermittedRequestLevel = RequestLevel.GetMax(
                        imageRequest.LowestPermittedRequestLevel,
                        lowestPermittedRequestLevelOnSubmit);

                SettableProducerContext settableProducerContext = new SettableProducerContext(
                    imageRequest,
                    GenerateUniqueFutureId(),
                    requestListener,
                    callerContext,
                    lowestPermittedRequestLevel.Value,
                    /* isPrefetch */ true,
                    /* isIntermediateResultExpected */ false,
                    priority);

                return ProducerToDataSourceAdapter<object>.Create(
                    producerSequence,
                    settableProducerContext,
                    requestListener);
            }
            catch (Exception exception)
            {
                return DataSources.ImmediateFailedDataSource<object>(exception);
            }
        }

        private IRequestListener GetRequestListenerForRequest(ImageRequest imageRequest)
        {
            if (imageRequest.RequestListener == null)
            {
                return _requestListener;
            }

            return new ForwardingRequestListener(_requestListener, imageRequest.RequestListener);
        }

        private Predicate<ICacheKey> PredicateForUri(Uri uri)
        {
            return new Predicate<ICacheKey>(key => key.ContainsUri(uri));
        }

        /// <summary>
        /// Pauses the producer queue.
        /// </summary>
        public void Pause()
        {
            _threadHandoffProducerQueue.StartQueueing();
        }

        /// <summary>
        /// Resumes the producer queue.
        /// </summary>
        public void Resume()
        {
            _threadHandoffProducerQueue.StopQueuing();
        }

        /// <summary>
        /// Returns true if the producer queue is paused, false otherwise.
        /// </summary>
        /// <returns></returns>
        public bool IsPaused()
        {
            return _threadHandoffProducerQueue.IsQueueing();
        }

        /// <summary>
        /// @return The CacheKeyFactory implementation used by ImagePipeline.
        /// </summary>
        public ICacheKeyFactory GetCacheKeyFactory()
        {
            return _cacheKeyFactory;
        }
    }
}
