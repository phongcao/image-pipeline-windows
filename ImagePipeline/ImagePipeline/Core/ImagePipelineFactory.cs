using Cache.Common;
using Cache.Disk;
using FBCore.Common.Internal;
using ImagePipeline.AnimatedFactory;
using ImagePipeline.Bitmaps;
using ImagePipeline.Cache;
using ImagePipeline.Decoder;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using ImagePipeline.Platform;
using ImagePipeline.Producers;
using System;

namespace ImagePipeline.Core
{
    /// <summary>
    /// Factory class for the image pipeline.
    ///
    /// <para />This class constructs the pipeline and its dependencies from other libraries.
    ///
    /// <para />As the pipeline object can be quite expensive to create, it is strongly
    /// recommended that applications create just one instance of this class
    /// and of the pipeline.
    /// </summary>
    public class ImagePipelineFactory
    {
        private static ImagePipelineFactory _instance;

        private readonly ThreadHandoffProducerQueue _threadHandoffProducerQueue;
        private readonly ImagePipelineConfig _config;
        private CountingMemoryCache<ICacheKey, CloseableImage> _bitmapCountingMemoryCache;
        private IMemoryCache<ICacheKey, CloseableImage> _bitmapMemoryCache;
        private CountingMemoryCache<ICacheKey, IPooledByteBuffer> _encodedCountingMemoryCache;
        private IMemoryCache<ICacheKey, IPooledByteBuffer> _encodedMemoryCache;
        private BufferedDiskCache _mainBufferedDiskCache;
        private IFileCache _mainFileCache;
        private ImageDecoder _imageDecoder;
        private ImagePipelineCore _imagePipeline;
        private ProducerFactory _producerFactory;
        private ProducerSequenceFactory _producerSequenceFactory;
        private BufferedDiskCache _smallImageBufferedDiskCache;
        private IFileCache _smallImageFileCache;

        private PlatformBitmapFactory _platformBitmapFactory;
        private IPlatformDecoder _platformDecoder;

        private IAnimatedFactory _animatedFactory;

        /// <summary>
        /// Gets the instance of <see cref="ImagePipelineFactory"/>.
        /// </summary>
        /// <returns></returns>
        public static ImagePipelineFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    Initialize();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Initializes <see cref="ImagePipelineFactory"/> with default config.
        /// </summary>

        private static void Initialize()
        {
            Initialize(ImagePipelineConfig.NewBuilder().Build());
        }

        /// <summary>
        /// Initializes <see cref="ImagePipelineFactory"/> with the specified config. 
        /// </summary>
        /// <param name="imagePipelineConfig">The imagepipeline configuration.</param>
        public static void Initialize(ImagePipelineConfig imagePipelineConfig)
        {
            _instance = new ImagePipelineFactory(imagePipelineConfig);
        }

        /// <summary>
        /// Shuts <see cref="ImagePipelineFactory"/> down.
        /// </summary>

        public static void ShutDown()
        {
            if (_instance != null)
            {
                _instance.GetBitmapMemoryCache().RemoveAll(new Predicate<ICacheKey>(_ => true));
                _instance.GetEncodedMemoryCache().RemoveAll(new Predicate<ICacheKey>(_ => true));
                _instance = null;
            }
        }

        /// <summary>
        /// Creates a new <see cref="DiskStorageCache"/> from the given <see cref="DiskCacheConfig"/>
        ///
        /// @deprecated use <see cref="DiskStorageCacheFactory.BuildDiskStorageCache"/>
        /// </summary>
        public static DiskStorageCache BuildDiskStorageCache(
            DiskCacheConfig diskCacheConfig,
            IDiskStorage diskStorage)
        {
            return DiskStorageCacheFactory.BuildDiskStorageCache(diskCacheConfig, diskStorage);
        }

        /// <summary>
        /// Provide the implementation of the PlatformBitmapFactory for the current platform
        /// using the provided PoolFactory.
        ///
        /// <param name="poolFactory">The PoolFactory.</param>
        /// <param name="platformDecoder">The PlatformDecoder.</param>
        /// @return The PlatformBitmapFactory implementation.
        /// </summary>
        public static PlatformBitmapFactory BuildPlatformBitmapFactory(
            PoolFactory poolFactory,
            IPlatformDecoder platformDecoder)
        {
            return new WinRTBitmapFactory();
        }

        /// <summary>
        /// Provide the implementation of the PlatformDecoder for the current platform using the
        /// provided PoolFactory.
        ///
        /// <param name="poolFactory">The PoolFactory.</param>
        /// <param name="webpSupportEnabled">Webp support.</param>
        /// @return The PlatformDecoder implementation.
        /// </summary>
        public static IPlatformDecoder BuildPlatformDecoder(
            PoolFactory poolFactory, bool webpSupportEnabled)
        {
            return new WinRTDecoder(poolFactory.FlexByteArrayPoolMaxNumThreads);
        }

        /// <summary>
        /// Instantiates the <see cref="ImagePipelineFactory"/>.
        /// </summary>
        /// <param name="config"></param>
        public ImagePipelineFactory(ImagePipelineConfig config)
        {
            _config = Preconditions.CheckNotNull(config);
            _threadHandoffProducerQueue = new ThreadHandoffProducerQueue(
                config.ExecutorSupplier.ForLightweightBackgroundTasks);
        }

        /// <summary>
        /// Gets the animated factory.
        /// </summary>
        public IAnimatedFactory GetAnimatedFactory()
        {
            if (_animatedFactory == null)
            {
                _animatedFactory = AnimatedFactoryProvider.GetAnimatedFactory(
                    GetPlatformBitmapFactory(),
                    _config.ExecutorSupplier);
            }

            return _animatedFactory;
        }

        /// <summary>
        /// Gets the bitmap counting memory cache.
        /// </summary>
        public CountingMemoryCache<ICacheKey, CloseableImage> GetBitmapCountingMemoryCache()
        {
            if (_bitmapCountingMemoryCache == null)
            {
                _bitmapCountingMemoryCache =
                    BitmapCountingMemoryCacheFactory.Get(
                        _config.BitmapMemoryCacheParamsSupplier,
                        _config.MemoryTrimmableRegistry,
                        GetPlatformBitmapFactory(),
                        _config.Experiments.IsExternalCreatedBitmapLogEnabled);
            }

            return _bitmapCountingMemoryCache;
        }

        /// <summary>
        /// Gets the bitmap memory cache.
        /// </summary>
        public IMemoryCache<ICacheKey, CloseableImage> GetBitmapMemoryCache()
        {
            if (_bitmapMemoryCache == null)
            {
                _bitmapMemoryCache = BitmapMemoryCacheFactory.Get(
                    GetBitmapCountingMemoryCache(), _config.ImageCacheStatsTracker);
            }

            return _bitmapMemoryCache;
        }

        /// <summary>
        /// Gets the encoded counting memory cache.
        /// </summary>
        public CountingMemoryCache<ICacheKey, IPooledByteBuffer> GetEncodedCountingMemoryCache()
        {
            if (_encodedCountingMemoryCache == null)
            {
                _encodedCountingMemoryCache = EncodedCountingMemoryCacheFactory.Get(
                    _config.EncodedMemoryCacheParamsSupplier,
                    _config.MemoryTrimmableRegistry,
                    GetPlatformBitmapFactory());
            }

            return _encodedCountingMemoryCache;
        }

        /// <summary>
        /// Gets the encoded memory cache.
        /// </summary>
        public IMemoryCache<ICacheKey, IPooledByteBuffer> GetEncodedMemoryCache()
        {
            if (_encodedMemoryCache == null)
            {
                _encodedMemoryCache = EncodedMemoryCacheFactory.Get(
                    GetEncodedCountingMemoryCache(),
                    _config.ImageCacheStatsTracker);
            }

            return _encodedMemoryCache;
        }

        /// <summary>
        /// Gets the platform bitmap factory.
        /// </summary>
        public PlatformBitmapFactory GetPlatformBitmapFactory()
        {
            if (_platformBitmapFactory == null)
            {
                _platformBitmapFactory = BuildPlatformBitmapFactory(
                    _config.PoolFactory,
                    GetPlatformDecoder());
            }

            return _platformBitmapFactory;
        }

        /// <summary>
        /// Gets the platform decoder.
        /// </summary>
        public IPlatformDecoder GetPlatformDecoder()
        {
            if (_platformDecoder == null)
            {
                _platformDecoder = BuildPlatformDecoder(
                    _config.PoolFactory,
                    _config.Experiments.IsWebpSupportEnabled);
            }

            return _platformDecoder;
        }

        /// <summary>
        /// @deprecated Use <see cref="GetMainFileCache"/>.
        /// </summary>
        public IFileCache GetMainDiskStorageCache()
        {
            return GetMainFileCache();
        }

        /// <summary>
        /// Gets the main file cache.
        /// </summary>
        public IFileCache GetMainFileCache()
        {
            if (_mainFileCache == null)
            {
                DiskCacheConfig diskCacheConfig = _config.MainDiskCacheConfig;
                _mainFileCache = _config.FileCacheFactory.Get(diskCacheConfig);
            }

            return _mainFileCache;
        }

        /// <summary>
        /// Gets the imagepipeline.
        /// </summary>
        /// <returns></returns>
        public ImagePipelineCore GetImagePipeline()
        {
            if (_imagePipeline == null)
            {
                _imagePipeline =
                    new ImagePipelineCore(
                        GetProducerSequenceFactory(),
                        _config.RequestListeners,
                        _config.IsPrefetchEnabledSupplier,
                        GetBitmapMemoryCache(),
                        GetEncodedMemoryCache(),
                        GetMainBufferedDiskCache(),
                        GetSmallImageBufferedDiskCache(),
                        _config.CacheKeyFactory,
                        _threadHandoffProducerQueue);
            }

            return _imagePipeline;
        }

        private ImageDecoder GetImageDecoder()
        {
            if (_imageDecoder == null)
            {
                if (_config.ImageDecoder != null)
                {
                    _imageDecoder = _config.ImageDecoder;
                }
                else
                {
                    IAnimatedFactory animatedFactory = GetAnimatedFactory();
                    IAnimatedImageFactory animatedImageFactory;
                    if (animatedFactory != null)
                    {
                        animatedImageFactory = GetAnimatedFactory().GetAnimatedImageFactory();
                    }
                    else
                    {
                        animatedImageFactory = null;
                    }

                    _imageDecoder = new ImageDecoder(
                        animatedImageFactory,
                        GetPlatformDecoder(),
                        _config.BitmapConfig);
                }
            }

            return _imageDecoder;
        }

        private BufferedDiskCache GetMainBufferedDiskCache()
        {
            if (_mainBufferedDiskCache == null)
            {
                _mainBufferedDiskCache =
                    new BufferedDiskCache(
                        GetMainFileCache(),
                        _config.PoolFactory.PooledByteBufferFactory,
                        _config.PoolFactory.PooledByteStreams,
                        _config.ExecutorSupplier.ForLocalStorageRead,
                        _config.ExecutorSupplier.ForLocalStorageWrite,
                        _config.ImageCacheStatsTracker);
            }

            return _mainBufferedDiskCache;
        }

        private ProducerFactory GetProducerFactory()
        {
            if (_producerFactory == null)
            {
                _producerFactory =
                    new ProducerFactory(
                        _config.PoolFactory.SmallByteArrayPool,
                        GetImageDecoder(),
                        _config.ProgressiveJpegConfig,
                        _config.IsDownsampleEnabled,
                        _config.IsResizeAndRotateEnabledForNetwork,
                        _config.ExecutorSupplier,
                        _config.PoolFactory.PooledByteBufferFactory,
                        GetBitmapMemoryCache(),
                        GetEncodedMemoryCache(),
                        GetMainBufferedDiskCache(),
                        GetSmallImageBufferedDiskCache(),
                        _config.CacheKeyFactory,
                        GetPlatformBitmapFactory(),
                        _config.PoolFactory.FlexByteArrayPool,
                        _config.Experiments.ForceSmallCacheThresholdBytes);
            }

            return _producerFactory;
        }

        private ProducerSequenceFactory GetProducerSequenceFactory()
        {
            if (_producerSequenceFactory == null)
            {
                _producerSequenceFactory =
                    new ProducerSequenceFactory(
                        GetProducerFactory(),
                        _config.NetworkFetcher,
                        _config.IsResizeAndRotateEnabledForNetwork,
                        _config.IsDownsampleEnabled,
                        _config.Experiments.IsWebpSupportEnabled,
                        _threadHandoffProducerQueue,
                        _config.Experiments.ThrottlingMaxSimultaneousRequests);
            }

            return _producerSequenceFactory;
        }

        /// <summary>
        /// @deprecated Use <see cref="GetSmallImageFileCache"/>.
        /// </summary>
        /// <returns></returns>
        public IFileCache GetSmallImageDiskStorageCache()
        {
            return GetSmallImageFileCache();
        }

        /// <summary>
        /// Gets the small image file cache.
        /// </summary>
        /// <returns></returns>
        public IFileCache GetSmallImageFileCache()
        {
            if (_smallImageFileCache == null)
            {
                DiskCacheConfig diskCacheConfig = _config.SmallImageDiskCacheConfig;
                _smallImageFileCache = _config.FileCacheFactory.Get(diskCacheConfig);
            }

            return _smallImageFileCache;
        }

        private BufferedDiskCache GetSmallImageBufferedDiskCache()
        {
            if (_smallImageBufferedDiskCache == null)
            {
                _smallImageBufferedDiskCache =
                    new BufferedDiskCache(
                        GetSmallImageFileCache(),
                        _config.PoolFactory.PooledByteBufferFactory,
                        _config.PoolFactory.PooledByteStreams,
                        _config.ExecutorSupplier.ForLocalStorageRead,
                        _config.ExecutorSupplier.ForLocalStorageWrite,
                        _config.ImageCacheStatsTracker);
            }

            return _smallImageBufferedDiskCache;
        }
    }
}
