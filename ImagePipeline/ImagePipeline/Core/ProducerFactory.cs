using Cache.Common;
using FBCore.Common.References;
using ImagePipeline.Bitmaps;
using ImagePipeline.Cache;
using ImagePipeline.Decoder;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using ImagePipeline.Producers;

namespace ImagePipeline.Core
{
    /// <summary>
    /// Producer factory
    /// </summary>
    public class ProducerFactory
    {
        // Decode dependencies
        private readonly IByteArrayPool _byteArrayPool;
        private readonly ImageDecoder _imageDecoder;
        private readonly IProgressiveJpegConfig _progressiveJpegConfig;
        private readonly bool _downsampleEnabled;
        private readonly bool _resizeAndRotateEnabledForNetwork;

        // Dependencies used by multiple steps
        private readonly IExecutorSupplier _executorSupplier;
        private readonly IPooledByteBufferFactory _pooledByteBufferFactory;

        // Cache dependencies
        private readonly BufferedDiskCache _defaultBufferedDiskCache;
        private readonly BufferedDiskCache _smallImageBufferedDiskCache;
        private readonly IMemoryCache<ICacheKey, IPooledByteBuffer> _encodedMemoryCache;
        private readonly IMemoryCache<ICacheKey, CloseableImage> _bitmapMemoryCache;
        private readonly ICacheKeyFactory _cacheKeyFactory;
        private readonly int _forceSmallCacheThresholdBytes;

        // Postproc dependencies
        private readonly PlatformBitmapFactory _platformBitmapFactory;

        /// <summary>
        /// Instantiates the <see cref="ProducerFactory"/>
        /// </summary>
        /// <param name="byteArrayPool">The IByteArrayPool used by DecodeProducer.</param>
        /// <param name="imageDecoder">The image decoder.</param>
        /// <param name="progressiveJpegConfig">The progressive Jpeg configuration.</param>
        /// <param name="downsampleEnabled">Enabling downsample.</param>
        /// <param name="resizeAndRotateEnabledForNetwork">Enabling resize and rotate.</param>
        /// <param name="executorSupplier">The supplier for tasks.</param>
        /// <param name="pooledByteBufferFactory">The factory that allocates IPooledByteBuffer memory.</param>
        /// <param name="bitmapMemoryCache">The memory cache for CloseableImage.</param>
        /// <param name="encodedMemoryCache">The memory cache for IPooledByteBuffer.</param>
        /// <param name="defaultBufferedDiskCache">The default buffered disk cache.</param>
        /// <param name="smallImageBufferedDiskCache">The buffered disk cache used for small images.</param>
        /// <param name="cacheKeyFactory">The factory that creates cache keys for the pipeline.</param>
        /// <param name="platformBitmapFactory">The bitmap factory used for post process.</param>
        /// <param name="decodeFileDescriptorEnabled">Enabling the file descriptor.</param>
        /// <param name="forceSmallCacheThresholdBytes">The threshold set for using the small buffered disk cache.</param>
        public ProducerFactory(
            IByteArrayPool byteArrayPool,
            ImageDecoder imageDecoder,
            IProgressiveJpegConfig progressiveJpegConfig,
            bool downsampleEnabled,
            bool resizeAndRotateEnabledForNetwork,
            IExecutorSupplier executorSupplier,
            IPooledByteBufferFactory pooledByteBufferFactory,
            IMemoryCache<ICacheKey, CloseableImage> bitmapMemoryCache,
            IMemoryCache<ICacheKey, IPooledByteBuffer> encodedMemoryCache,
            BufferedDiskCache defaultBufferedDiskCache,
            BufferedDiskCache smallImageBufferedDiskCache,
            ICacheKeyFactory cacheKeyFactory,
            PlatformBitmapFactory platformBitmapFactory,
            bool decodeFileDescriptorEnabled,
            int forceSmallCacheThresholdBytes)
        {
            _forceSmallCacheThresholdBytes = forceSmallCacheThresholdBytes;

            _byteArrayPool = byteArrayPool;
            _imageDecoder = imageDecoder;
            _progressiveJpegConfig = progressiveJpegConfig;
            _downsampleEnabled = downsampleEnabled;
            _resizeAndRotateEnabledForNetwork = resizeAndRotateEnabledForNetwork;

            _executorSupplier = executorSupplier;
            _pooledByteBufferFactory = pooledByteBufferFactory;

            _bitmapMemoryCache = bitmapMemoryCache;
            _encodedMemoryCache = encodedMemoryCache;
            _defaultBufferedDiskCache = defaultBufferedDiskCache;
            _smallImageBufferedDiskCache = smallImageBufferedDiskCache;
            _cacheKeyFactory = cacheKeyFactory;

            _platformBitmapFactory = platformBitmapFactory;
        }

        /// <summary>
        /// Instantiates the <see cref="AddImageTransformMetaDataProducer"/>
        /// </summary>
        /// <param name="inputProducer">The input producer.</param>
        public static AddImageTransformMetaDataProducer NewAddImageTransformMetaDataProducer(
            IProducer<EncodedImage> inputProducer)
        {
            return new AddImageTransformMetaDataProducer(inputProducer);
        }

        /// <summary>
        /// Instantiates the <see cref="BitmapMemoryCacheGetProducer"/>
        /// </summary>
        /// <param name="inputProducer">The input producer.</param>
        public BitmapMemoryCacheGetProducer NewBitmapMemoryCacheGetProducer(
            IProducer<CloseableReference<CloseableImage>> inputProducer)
        {
            return new BitmapMemoryCacheGetProducer(_bitmapMemoryCache, _cacheKeyFactory, inputProducer);
        }

        /// <summary>
        /// Instantiates the <see cref="BitmapMemoryCacheKeyMultiplexProducer"/>
        /// </summary>
        /// <param name="inputProducer">The input producer.</param>
        public BitmapMemoryCacheKeyMultiplexProducer NewBitmapMemoryCacheKeyMultiplexProducer(
            IProducer<CloseableReference<CloseableImage>> inputProducer)
        {
            return new BitmapMemoryCacheKeyMultiplexProducer(_cacheKeyFactory, inputProducer);
        }

        /// <summary>
        /// Instantiates the <see cref="BitmapMemoryCacheProducer"/>
        /// </summary>
        /// <param name="inputProducer">The input producer.</param>
        public BitmapMemoryCacheProducer NewBitmapMemoryCacheProducer(
            IProducer<CloseableReference<CloseableImage>> inputProducer)
        {
            return new BitmapMemoryCacheProducer(_bitmapMemoryCache, _cacheKeyFactory, inputProducer);
        }

        /// <summary>
        /// Instantiates the <see cref="BranchOnSeparateImagesProducer"/>
        /// </summary>
        /// <param name="inputProducer1">The first input producer.</param>
        /// <param name="inputProducer2">The second input producer.</param>
        public static BranchOnSeparateImagesProducer NewBranchOnSeparateImagesProducer(
            IProducer<EncodedImage> inputProducer1,
            IProducer<EncodedImage> inputProducer2)
        {
            return new BranchOnSeparateImagesProducer(inputProducer1, inputProducer2);
        }

        /// <summary>
        /// Instantiates the <see cref="DataFetchProducer"/>
        /// </summary>
        public DataFetchProducer NewDataFetchProducer()
        {
            return new DataFetchProducer(_pooledByteBufferFactory);
        }

        /// <summary>
        /// Instantiates the <see cref="DecodeProducer"/>
        /// </summary>
        /// <param name="inputProducer">The input producer.</param>
        public DecodeProducer NewDecodeProducer(IProducer<EncodedImage> inputProducer)
        {
            return new DecodeProducer(
                _byteArrayPool,
                _executorSupplier.ForDecode,
                _imageDecoder,
                _progressiveJpegConfig,
                _downsampleEnabled,
                _resizeAndRotateEnabledForNetwork,
                inputProducer);
        }

        /// <summary>
        /// Instantiates the <see cref="DiskCacheProducer"/>
        /// </summary>
        /// <param name="inputProducer">The input producer.</param>
        public DiskCacheProducer NewDiskCacheProducer(
            IProducer<EncodedImage> inputProducer)
        {
            return new DiskCacheProducer(
                _defaultBufferedDiskCache,
                _smallImageBufferedDiskCache,
                _cacheKeyFactory,
                inputProducer,
                _forceSmallCacheThresholdBytes);
        }

        /// <summary>
        /// Instantiates the <see cref="EncodedCacheKeyMultiplexProducer"/>
        /// </summary>
        /// <param name="inputProducer">The input producer.</param>
        public EncodedCacheKeyMultiplexProducer NewEncodedCacheKeyMultiplexProducer(
            IProducer<EncodedImage> inputProducer)
        {
            return new EncodedCacheKeyMultiplexProducer(
                _cacheKeyFactory,
                inputProducer);
        }

        /// <summary>
        /// Instantiates the <see cref="EncodedMemoryCacheProducer"/>
        /// </summary>
        /// <param name="inputProducer">The input producer.</param>
        public EncodedMemoryCacheProducer NewEncodedMemoryCacheProducer(
            IProducer<EncodedImage> inputProducer)
        {
            return new EncodedMemoryCacheProducer(
                _encodedMemoryCache,
                _cacheKeyFactory,
                inputProducer);
        }

        /// <summary>
        /// Instantiates the <see cref="LocalAssetFetchProducer"/>
        /// </summary>
        public LocalAssetFetchProducer NewLocalAssetFetchProducer()
        {
            return new LocalAssetFetchProducer(
                _executorSupplier.ForLocalStorageRead,
                _pooledByteBufferFactory);
        }

        /// <summary>
        /// Instantiates the <see cref="LocalContentUriFetchProducer"/>
        /// </summary>
        public LocalContentUriFetchProducer NewLocalContentUriFetchProducer()
        {
            return new LocalContentUriFetchProducer(
                _executorSupplier.ForLocalStorageRead,
                _pooledByteBufferFactory);
        }

        /// <summary>
        /// Instantiates the <see cref="LocalContentUriThumbnailFetchProducer"/>
        /// </summary>
        public LocalContentUriThumbnailFetchProducer NewLocalContentUriThumbnailFetchProducer()
        {
            return new LocalContentUriThumbnailFetchProducer(
                _executorSupplier.ForLocalStorageRead,
                _pooledByteBufferFactory);
        }

        /// <summary>
        /// Instantiates the <see cref="LocalExifThumbnailProducer"/>
        /// </summary>
        public LocalExifThumbnailProducer NewLocalExifThumbnailProducer()
        {
            return new LocalExifThumbnailProducer(
                _executorSupplier.ForLocalStorageRead,
                _pooledByteBufferFactory);
        }

        /// <summary>
        /// Instantiates the <see cref="ThumbnailBranchProducer"/>
        /// </summary>
        public ThumbnailBranchProducer NewThumbnailBranchProducer(
            IThumbnailProducer<EncodedImage>[] thumbnailProducers)
        {
            return new ThumbnailBranchProducer(thumbnailProducers);
        }

        /// <summary>
        /// Instantiates the <see cref="LocalFileFetchProducer"/>
        /// </summary>
        public LocalFileFetchProducer NewLocalFileFetchProducer()
        {
            return new LocalFileFetchProducer(
                _executorSupplier.ForLocalStorageRead,
                _pooledByteBufferFactory);
        }

        /// <summary>
        /// Instantiates the <see cref="LocalResourceFetchProducer"/>
        /// </summary>
        public LocalResourceFetchProducer NewLocalResourceFetchProducer()
        {
            return new LocalResourceFetchProducer(
                _executorSupplier.ForLocalStorageRead,
                _pooledByteBufferFactory);
        }

        /// <summary>
        /// Instantiates the <see cref="LocalVideoThumbnailProducer"/>
        /// </summary>
        public LocalVideoThumbnailProducer NewLocalVideoThumbnailProducer()
        {
            return new LocalVideoThumbnailProducer(_executorSupplier.ForLocalStorageRead);
        }

        /// <summary>
        /// Instantiates the <see cref="NetworkFetchProducer"/>
        /// </summary>
        public NetworkFetchProducer NewNetworkFetchProducer(INetworkFetcher<FetchState> networkFetcher)
        {
            return new NetworkFetchProducer(
                _pooledByteBufferFactory,
                _byteArrayPool,
                networkFetcher);
        }

        /// <summary>
        /// Instantiates the <see cref="NullProducer{T}"/>
        /// </summary>
        public static NullProducer<T> NewNullProducer<T>()
        {
            return new NullProducer<T>();
        }

        /// <summary>
        /// Instantiates the <see cref="PostprocessedBitmapMemoryCacheProducer"/>
        /// </summary>
        /// <param name="inputProducer">The input producer.</param>
        public PostprocessedBitmapMemoryCacheProducer NewPostprocessorBitmapMemoryCacheProducer(
            IProducer<CloseableReference<CloseableImage>> inputProducer)
        {
            return new PostprocessedBitmapMemoryCacheProducer(
                _bitmapMemoryCache, _cacheKeyFactory, inputProducer);
        }

        /// <summary>
        /// Instantiates the <see cref="PostprocessorProducer"/>
        /// </summary>
        /// <param name="inputProducer">The input producer.</param>
        public PostprocessorProducer NewPostprocessorProducer(
            IProducer<CloseableReference<CloseableImage>> inputProducer)
        {
            return new PostprocessorProducer(
                inputProducer, _platformBitmapFactory, _executorSupplier.ForBackgroundTasks);
        }

        /// <summary>
        /// Instantiates the <see cref="ResizeAndRotateProducer"/>
        /// </summary>
        /// <param name="inputProducer">The input producer.</param>
        public ResizeAndRotateProducer NewResizeAndRotateProducer(IProducer<EncodedImage> inputProducer)
        {
            return new ResizeAndRotateProducer(
                _executorSupplier.ForBackgroundTasks,
                _pooledByteBufferFactory,
                inputProducer);
        }

        /// <summary>
        /// Instantiates the <see cref="SwallowResultProducer{T}"/>
        /// </summary>
        /// <param name="inputProducer">The input producer.</param>
        public static SwallowResultProducer<T> NewSwallowResultProducer<T>(IProducer<T> inputProducer)
        {
            return new SwallowResultProducer<T>(inputProducer);
        }

        /// <summary>
        /// Instantiates the <see cref="ThreadHandoffProducer{T}"/>
        /// </summary>
        /// <param name="inputProducer">The input producer.</param>
        /// <param name="inputThreadHandoffProducerQueue">The thread handoff producer queue.</param>
        public ThreadHandoffProducer<T> NewBackgroundThreadHandoffProducer<T>(
            IProducer<T> inputProducer, 
            ThreadHandoffProducerQueue inputThreadHandoffProducerQueue)
        {
            return new ThreadHandoffProducer<T>(
                inputProducer,
                inputThreadHandoffProducerQueue);
        }

        /// <summary>
        /// Instantiates the <see cref="ThrottlingProducer{T}"/>
        /// </summary>
        /// <param name="maxSimultaneousRequests">The max simultaneous requests.</param>
        /// <param name="inputProducer">The input producer.</param>
        public ThrottlingProducer<T> NewThrottlingProducer<T>(
            int maxSimultaneousRequests,
            IProducer<T> inputProducer)
        {
            return new ThrottlingProducer<T>(
                maxSimultaneousRequests,
                _executorSupplier.ForLightweightBackgroundTasks,
                inputProducer);
        }

        /// <summary>
        /// Instantiates the <see cref="WebpTranscodeProducer"/>
        /// </summary>
        /// <param name="inputProducer">The input producer.</param>
        public WebpTranscodeProducer NewWebpTranscodeProducer(
            IProducer<EncodedImage> inputProducer)
        {
            return new WebpTranscodeProducer(
                _executorSupplier.ForBackgroundTasks,
                _pooledByteBufferFactory,
                inputProducer);
        }
    }
}
