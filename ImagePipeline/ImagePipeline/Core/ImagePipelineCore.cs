using Cache.Common;
using FBCore.Common.Internal;
using FBCore.Common.References;
using FBCore.Common.Util;
using FBCore.Concurrency;
using FBCore.DataSource;
using ImagePipeline.Cache;
using ImagePipeline.Common;
using ImagePipeline.Datasource;
using ImagePipeline.Image;
using ImagePipeline.Listener;
using ImagePipeline.Memory;
using ImagePipeline.Platform;
using ImagePipeline.Producers;
using ImagePipeline.Request;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace ImagePipeline.Core
{
    /// <summary>
    /// The entry point for the image pipeline.
    /// </summary>
    public class ImagePipelineCore
    {
        private const int MAX_DATA_SOURCE_SUBSCRIBERS = 10;

        private readonly ProducerSequenceFactory _producerSequenceFactory;
        private readonly IRequestListener _requestListener;
        private readonly ISupplier<bool> _isPrefetchEnabledSupplier;
        private readonly IMemoryCache<ICacheKey, CloseableImage> _bitmapMemoryCache;
        private readonly IMemoryCache<ICacheKey, IPooledByteBuffer> _encodedMemoryCache;
        private readonly BufferedDiskCache _mainBufferedDiskCache;
        private readonly BufferedDiskCache _smallImageBufferedDiskCache;
        private readonly ICacheKeyFactory _cacheKeyFactory;
        private readonly ThreadHandoffProducerQueue _threadHandoffProducerQueue;
        private readonly FlexByteArrayPool _flexByteArrayPool;
        private readonly IExecutorService _handleResultExecutor;
        private long _idCounter;

        /// <summary>
        /// Instantiates the <see cref="ImagePipelineCore"/>.
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
        /// <param name="flexByteArrayPool">The memory pool use for BitmapImage conversion.</param>
        public ImagePipelineCore(
            ProducerSequenceFactory producerSequenceFactory,
            HashSet<IRequestListener> requestListeners,
            ISupplier<bool> isPrefetchEnabledSupplier,
            IMemoryCache<ICacheKey, CloseableImage> bitmapMemoryCache,
            IMemoryCache<ICacheKey, IPooledByteBuffer> encodedMemoryCache,
            BufferedDiskCache mainBufferedDiskCache,
            BufferedDiskCache smallImageBufferedDiskCache,
            ICacheKeyFactory cacheKeyFactory,
            ThreadHandoffProducerQueue threadHandoffProducerQueue,
            FlexByteArrayPool flexByteArrayPool)
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
            _flexByteArrayPool = flexByteArrayPool;
            _handleResultExecutor = Executors.NewFixedThreadPool(MAX_DATA_SOURCE_SUBSCRIBERS);
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
        /// Submits a request for bitmap cache lookup.
        ///
        /// <param name="imageRequest">The request to submit.</param>
        /// <param name="token">The cancellation token.</param>
        /// @return a Task{WriteableBitmap} representing the image.
        /// </summary>
        public Task<WriteableBitmap> FetchImageFromBitmapCache(
            ImageRequest imageRequest,
            CancellationToken token = default(CancellationToken))
        {
            var taskCompletionSource = new TaskCompletionSource<WriteableBitmap>();
            var dataSource = FetchDecodedImage(
                imageRequest,
                null,
                new RequestLevel(RequestLevel.BITMAP_MEMORY_CACHE));

            var dataSubscriber = new BaseBitmapDataSubscriberImpl(
                async bitmap =>
                {
                    if (bitmap != null)
                    {
                        await DispatcherHelpers.RunOnDispatcherAsync(() =>
                        {
                            try
                            {
                                var writeableBitmap = new WriteableBitmap(bitmap.PixelWidth, bitmap.PixelHeight);
                                bitmap.CopyToBuffer(writeableBitmap.PixelBuffer);
                                taskCompletionSource.SetResult(writeableBitmap);
                            }
                            catch (Exception e)
                            {
                                taskCompletionSource.SetException(e);
                            }
                        })
                        .ConfigureAwait(false);
                    }
                    else
                    {
                        taskCompletionSource.SetResult(null);
                    }
                },
                response =>
                {
                    taskCompletionSource.SetException(response.GetFailureCause());
                });

            dataSource.Subscribe(dataSubscriber, _handleResultExecutor);
            token.Register(() =>
            {
                dataSource.Close();
                taskCompletionSource.TrySetCanceled();
            });

            return taskCompletionSource.Task;
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
                return DataSources.ImmediateFailedDataSource<CloseableReference<IPooledByteBuffer>>(
                    exception);
            }
        }

        /// <summary>
        /// Fetches the encoded BitmapImage.
        /// <param name="uri">The image uri.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The encoded BitmapImage.</returns>
        /// @throws IOException if the image uri can't be found.
        /// </summary>
        public Task<BitmapImage> FetchEncodedBitmapImage(
            Uri uri, 
            CancellationToken token = default(CancellationToken))
        {
            var taskCompletionSource = new TaskCompletionSource<BitmapImage>();
            var dataSource = FetchEncodedImage(ImageRequest.FromUri(uri), null);
            var dataSubscriber = new BaseDataSubscriberImpl<CloseableReference<IPooledByteBuffer>>(
                async response =>
                {
                    CloseableReference<IPooledByteBuffer> reference = response.GetResult();
                    if (reference != null)
                    {
                        //----------------------------------------------------------------------
                        // Phong Cao: InMemoryRandomAccessStream can't write anything < 16KB.
                        // http://stackoverflow.com/questions/25928408/inmemoryrandomaccessstream-incorrect-behavior
                        //----------------------------------------------------------------------
                        IPooledByteBuffer inputStream = reference.Get();
                        int supportedSize = Math.Max(16 * ByteConstants.KB, inputStream.Size);

                        // Allocate temp buffer for stream convert
                        byte[] bytesArray = default(byte[]);
                        CloseableReference<byte[]> bytesArrayRef = default(CloseableReference<byte[]>);

                        try
                        {
                            bytesArrayRef = _flexByteArrayPool.Get(supportedSize);
                            bytesArray = bytesArrayRef.Get();
                        }
                        catch (Exception)
                        {
                            // Allocates the byte array since the pool couldn't provide one
                            bytesArray = new byte[supportedSize];
                        }

                        try
                        {
                            inputStream.Read(0, bytesArray, 0, inputStream.Size);
                            await DispatcherHelpers.RunOnDispatcherAsync(async () =>
                            {
                                using (var outStream = new InMemoryRandomAccessStream())
                                using (var writeStream = outStream.AsStreamForWrite())
                                {
                                    await writeStream.WriteAsync(bytesArray, 0, supportedSize).ConfigureAwait(false);
                                    outStream.Seek(0);
                                    BitmapImage bitmapImage = new BitmapImage();
                                    await bitmapImage.SetSourceAsync(outStream).AsTask().ConfigureAwait(false);
                                    taskCompletionSource.SetResult(bitmapImage);
                                }
                            })
                            .ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            taskCompletionSource.SetException(e);
                        }
                        finally
                        {
                            CloseableReference<IPooledByteBuffer>.CloseSafely(reference);
                            CloseableReference<byte[]>.CloseSafely(bytesArrayRef);
                        }
                    }
                    else
                    {
                        taskCompletionSource.SetResult(null);
                    }
                },
                response =>
                {
                    taskCompletionSource.SetException(response.GetFailureCause());
                });

            dataSource.Subscribe(dataSubscriber, _handleResultExecutor);
            token.Register(() =>
            {
                dataSource.Close();
                taskCompletionSource.TrySetCanceled();
            });

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Fetches the decoded SoftwareBitmapSource.
        /// <param name="imageRequest">The image request.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The decoded SoftwareBitmapSource.</returns>
        /// @throws IOException if the image request isn't valid.
        /// </summary>
        public Task<WriteableBitmap> FetchDecodedBitmapImage(
            ImageRequest imageRequest,
            CancellationToken token = default(CancellationToken))
        {
            var taskCompletionSource = new TaskCompletionSource<WriteableBitmap>();
            var dataSource = FetchDecodedImage(imageRequest, null);
            var dataSubscriber = new BaseBitmapDataSubscriberImpl(
                async bitmap =>
                {
                    if (bitmap != null)
                    {
                        await DispatcherHelpers.RunOnDispatcherAsync(() =>
                        {
                            try
                            {
                                var writeableBitmap = new WriteableBitmap(bitmap.PixelWidth, bitmap.PixelHeight);
                                bitmap.CopyToBuffer(writeableBitmap.PixelBuffer);
                                taskCompletionSource.SetResult(writeableBitmap);
                            }
                            catch (Exception e)
                            {
                                taskCompletionSource.SetException(e);
                            }
                        })
                        .ConfigureAwait(false);
                    }
                    else
                    {
                        taskCompletionSource.SetResult(null);
                    }
                },
                response =>
                {
                    taskCompletionSource.SetException(response.GetFailureCause());
                });

            dataSource.Subscribe(dataSubscriber, _handleResultExecutor);
            token.Register(() =>
            {
                dataSource.Close();
                taskCompletionSource.TrySetCanceled();
            });

            return taskCompletionSource.Task;
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
                return DataSources.ImmediateFailedDataSource<object>(
                    new OperationCanceledException("Prefetching is not enabled"));
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
        /// Submits a request for prefetching to the bitmap cache.
        /// <param name="uri">The image uri.</param>
        /// <param name="token">The cancellation token.</param>
        /// @return a DataSource that can safely be ignored.
        /// </summary>
        public Task PrefetchToBitmapCache(
            Uri uri, 
            CancellationToken token = default(CancellationToken))
        {
            var taskCompletionSource = new TaskCompletionSource<object>();
            var dataSource = PrefetchToBitmapCache(ImageRequest.FromUri(uri), null);
            var dataSubscriber = new BaseDataSubscriberImpl<object>(
                response =>
                {
                    taskCompletionSource.SetResult(null);
                    return Task.CompletedTask;
                },
                response =>
                {
                    Exception error = response.GetFailureCause();
                    taskCompletionSource.SetException(error);
                });

            dataSource.Subscribe(dataSubscriber, _handleResultExecutor);
            token.Register(() =>
            {
                dataSource.Close();
                taskCompletionSource.TrySetCanceled();
            });

            return taskCompletionSource.Task;
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
                return DataSources.ImmediateFailedDataSource<object>(
                    new OperationCanceledException("Prefetching is not enabled"));
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
        /// Submits a request for prefetching to the disk cache.
        /// <param name="uri">The image uri.</param>
        /// <param name="token">The cancellation token.</param>
        /// @return a DataSource that can safely be ignored.
        /// </summary>
        public Task PrefetchToDiskCache(
            Uri uri, 
            CancellationToken token = default(CancellationToken))
        {
            var taskCompletionSource = new TaskCompletionSource<object>();
            var dataSource = PrefetchToDiskCache(ImageRequest.FromUri(uri), null);
            var dataSubscriber = new BaseDataSubscriberImpl<object>(
                response =>
                {
                    taskCompletionSource.SetResult(null);
                    return Task.CompletedTask;
                },
                response =>
                {
                    Exception error = response.GetFailureCause();
                    taskCompletionSource.SetException(error);
                });

            dataSource.Subscribe(dataSubscriber, _handleResultExecutor);
            token.Register(() =>
            {
                dataSource.Close();
                taskCompletionSource.TrySetCanceled();
            });

            return taskCompletionSource.Task;
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
        public Task EvictFromDiskCache(Uri uri)
        {
            return EvictFromDiskCache(ImageRequest.FromUri(uri));
        }

        /// <summary>
        /// Removes all images with the specified <see cref="Uri"/> from disk cache.
        ///
        /// <param name="imageRequest">The imageRequest for the image to evict from disk cache.</param>
        /// </summary>
        public async Task EvictFromDiskCache(ImageRequest imageRequest)
        {
            ICacheKey cacheKey = _cacheKeyFactory.GetEncodedCacheKey(imageRequest, null);
            await _mainBufferedDiskCache.Remove(cacheKey).ConfigureAwait(false);
            await _smallImageBufferedDiskCache.Remove(cacheKey).ConfigureAwait(false);
        }

        /// <summary>
        /// <para />If you have supplied your own cache key factory when configuring the pipeline, 
        /// this method may not work correctly. It will only work if the custom factory builds the 
        /// cache key entirely from the URI. If that is not the case, use EvictFromMemoryCache(Uri)
        /// and EvictFromDiskCache(ImageRequest) separately.
        /// <param name="uri">The uri of the image to evict.</param>
        /// </summary>
        public async Task EvictFromCache(Uri uri)
        {
            EvictFromMemoryCache(uri);
            await EvictFromDiskCache(uri).ConfigureAwait(false);
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
        public async Task ClearDiskCachesAsync()
        {
            await _mainBufferedDiskCache.ClearAll().ConfigureAwait(false);
            await _smallImageBufferedDiskCache.ClearAll().ConfigureAwait(false);
        }

        /// <summary>
        /// Clear all the caches (memory and disk).
        /// </summary>
        public async Task ClearCachesAsync()
        {
            ClearMemoryCaches();
            await ClearDiskCachesAsync().ConfigureAwait(false);
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

        /// <summary>
        /// Returns whether the image is stored in the disk cache.
        ///
        /// <param name="imageRequest">The imageRequest for the image to be looked up.</param>
        /// @return true if the image was found in the disk cache, false otherwise.
        /// </summary>
        public async Task<bool> IsInDiskCacheAsync(ImageRequest imageRequest)
        {
            ICacheKey cacheKey = _cacheKeyFactory.GetEncodedCacheKey(imageRequest, null);
            bool found = await _mainBufferedDiskCache.Contains(cacheKey).ConfigureAwait(false);
            if (!found)
            {
                return await _smallImageBufferedDiskCache.Contains(cacheKey).ConfigureAwait(false);
            }

            return found;
        }

        /// <summary>
        /// Returns whether the image is stored in the disk cache.
        ///
        /// <param name="uri">The uri for the image to be looked up.</param>
        /// @return true if the image was found in the disk cache, false otherwise.
        /// </summary>
        public Task<bool> IsInDiskCacheAsync(Uri uri)
        {
            return IsInDiskCacheAsync(ImageRequest.FromUri(uri));
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
