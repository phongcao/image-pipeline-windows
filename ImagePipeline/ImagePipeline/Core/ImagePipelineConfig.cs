using Cache.Disk;
using FBCore.Common.Internal;
using FBCore.Common.Memory;
using ImagePipeline.AnimatedFactory;
using ImagePipeline.Bitmaps;
using ImagePipeline.Cache;
using ImagePipeline.Decoder;
using ImagePipeline.Listener;
using ImagePipeline.Memory;
using ImagePipeline.Producers;
using System.Collections.Generic;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Core
{
    /// <summary>
    /// Master configuration class for the image pipeline library.
    ///
    /// To use:
    /// <code>
    ///   ImagePipelineConfig config = ImagePipelineConfig.NewBuilder()
    ///       .SetXXX(xxx)
    ///       .SetYYY(yyy)
    ///       .Build();
    ///   ImagePipelineFactory factory = new ImagePipelineFactory(config);
    ///   ImagePipeline pipeline = factory.GetImagePipeline();
    /// </code>
    ///
    /// <para />This should only be done once per process.
    /// </summary>
    public class ImagePipelineConfig
    {
        // If a member here is marked @Nullable, it must be constructed by ImagePipelineFactory
        // on demand if needed.

        // There are a lot of parameters in this class. Please follow strict alphabetical order.
        private readonly IAnimatedImageFactory _animatedImageFactory;
        private readonly BitmapPixelFormat _bitmapConfig;
        private readonly ISupplier<MemoryCacheParams> _bitmapMemoryCacheParamsSupplier;
        private readonly ICacheKeyFactory _cacheKeyFactory;
        private readonly bool _downsampleEnabled;
        private readonly bool _decodeMemoryFileEnabled;
        private readonly IFileCacheFactory _fileCacheFactory;
        private readonly ISupplier<MemoryCacheParams> _encodedMemoryCacheParamsSupplier;
        private readonly IExecutorSupplier _executorSupplier;
        private readonly IImageCacheStatsTracker _imageCacheStatsTracker;
        private readonly ImageDecoder _imageDecoder;
        private readonly ISupplier<bool> _isPrefetchEnabledSupplier;
        private readonly DiskCacheConfig _mainDiskCacheConfig;
        private readonly IMemoryTrimmableRegistry _memoryTrimmableRegistry;
        private readonly INetworkFetcher<FetchState> _networkFetcher;
        private readonly PlatformBitmapFactory _platformBitmapFactory;
        private readonly PoolFactory _poolFactory;
        private readonly IProgressiveJpegConfig _progressiveJpegConfig;
        private readonly HashSet<IRequestListener> _requestListeners;
        private readonly bool _resizeAndRotateEnabledForNetwork;
        private readonly DiskCacheConfig _smallImageDiskCacheConfig;
        private readonly ImagePipelineExperiments _imagePipelineExperiments;

        private ImagePipelineConfig(Builder builder)
        {
            _animatedImageFactory = builder.AnimatedImageFactory;
            _bitmapMemoryCacheParamsSupplier = builder.BitmapMemoryCacheParamsSupplier ??
                    new DefaultBitmapMemoryCacheParamsSupplier();

            _bitmapConfig = builder.BitmapConfig == default(BitmapPixelFormat) ?
                    BitmapPixelFormat.Bgra8 : builder.BitmapConfig;

            _cacheKeyFactory = builder.CacheKeyFactory ?? DefaultCacheKeyFactory.Instance;

            _decodeMemoryFileEnabled = builder.IsDecodeMemoryFileEnabled;
            _fileCacheFactory = builder.FileCacheFactory ??
                new DiskStorageCacheFactory(new DynamicDefaultDiskStorageFactory());

            _downsampleEnabled = builder.IsDownsampleEnabled;
            _encodedMemoryCacheParamsSupplier = builder.EncodedMemoryCacheParamsSupplier ??
                    new DefaultEncodedMemoryCacheParamsSupplier();

            _imageCacheStatsTracker = builder.ImageCacheStatsTracker ??
                    NoOpImageCacheStatsTracker.Instance;

            _imageDecoder = builder.ImageDecoder;
            _isPrefetchEnabledSupplier = builder.IsPrefetchEnabledSupplier ??
                new SupplierImpl<bool>(
                    () =>
                    {
                        return true;
                    });

            _mainDiskCacheConfig = builder.MainDiskCacheConfig ??
                GetDefaultMainDiskCacheConfig();

            _memoryTrimmableRegistry = builder.MemoryTrimmableRegistry ??
                NoOpMemoryTrimmableRegistry.Instance;

            _networkFetcher = builder.NetworkFetcher ?? new HttpUrlConnectionNetworkFetcher();
            _platformBitmapFactory = builder.PlatformBitmapFactory;
            _poolFactory = builder.PoolFactory ?? new PoolFactory(PoolConfig.NewBuilder().Build());
            _progressiveJpegConfig = builder.ProgressiveJpegConfig == default(IProgressiveJpegConfig) ?
                new SimpleProgressiveJpegConfig() : builder.ProgressiveJpegConfig;

            _requestListeners = builder.RequestListeners ?? new HashSet<IRequestListener>();
            _resizeAndRotateEnabledForNetwork = builder.ResizeAndRotateEnabledForNetwork;
            _smallImageDiskCacheConfig = builder.SmallImageDiskCacheConfig ?? _mainDiskCacheConfig;

            // Below this comment can't be built in alphabetical order, because of dependencies
            int numCpuBoundThreads = _poolFactory.FlexByteArrayPoolMaxNumThreads;
            _executorSupplier = builder.ExecutorSupplier ?? 
                new DefaultExecutorSupplier(numCpuBoundThreads);

            _imagePipelineExperiments = builder.Experiment.Build();
        }

        /// <summary>
        /// Gets the animated image factory.
        /// </summary>
        public IAnimatedImageFactory AnimatedImageFactory
        {
            get
            {
                return _animatedImageFactory;
            }
        }

        /// <summary>
        /// Gets the bitmap configuration.
        /// </summary>
        public BitmapPixelFormat BitmapConfig
        {
            get
            {
                return _bitmapConfig;
            }
        }

        /// <summary>
        /// Gets the bitmap memory cache params supplier.
        /// </summary>
        public ISupplier<MemoryCacheParams> BitmapMemoryCacheParamsSupplier
        {
            get
            {
                return _bitmapMemoryCacheParamsSupplier;
            }
        }

        /// <summary>
        /// Gets the cache key factory.
        /// </summary>
        public ICacheKeyFactory CacheKeyFactory
        {
            get
            {
                return _cacheKeyFactory;
            }
        }

        /// <summary>
        /// @deprecated Use GetExperiments() and ImagePipelineExperiments.IsDecodeFileDescriptorEnabled().
        /// </summary>
        public bool IsDecodeFileDescriptorEnabled
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if decode memory file is enabled, otherwise false.
        /// </summary>
        public bool IsDecodeMemoryFileEnabled
        {
            get
            {
                return _decodeMemoryFileEnabled;
            }
        }

        /// <summary>
        /// Gets the file cache factory.
        /// </summary>
        public IFileCacheFactory FileCacheFactory
        {
            get
            {
                return _fileCacheFactory;
            }
        }

        /// <summary>
        /// Returns true if downsample is enabled, otherwise false.
        /// </summary>
        public bool IsDownsampleEnabled
        {
            get
            {
                return _downsampleEnabled;
            }
        }

        /// <summary>
        /// @deprecated Use GetExperiments and ImagePipelineExperiments.IsWebpSupportEnabled().
        /// </summary>
        public bool IsWebpSupportEnabled
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the encoded memory cache params supplier.
        /// </summary>
        public ISupplier<MemoryCacheParams> EncodedMemoryCacheParamsSupplier
        {
            get
            {
                return _encodedMemoryCacheParamsSupplier;
            }
        }

        /// <summary>
        /// Gets the executor supplier.
        /// </summary>
        public IExecutorSupplier ExecutorSupplier
        {
            get
            {
                return _executorSupplier;
            }
        }

        /// <summary>
        /// @deprecated Use GetExperiments and ImagePipelineExperiments.GetForceSmallCacheThresholdBytes().
        /// </summary>
        public int ForceSmallCacheThresholdBytes
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the image cache stats tracker.
        /// </summary>
        public IImageCacheStatsTracker ImageCacheStatsTracker
        {
            get
            {
                return _imageCacheStatsTracker;
            }
        }

        /// <summary>
        /// Gets the image decoder.
        /// </summary>
        public ImageDecoder ImageDecoder
        {
            get
            {
                return _imageDecoder;
            }
        }

        /// <summary>
        /// Gets the IsPrefetchEnabledSupplier.
        /// </summary>
        public ISupplier<bool> IsPrefetchEnabledSupplier
        {
            get
            {
                return _isPrefetchEnabledSupplier;
            }
        }

        /// <summary>
        /// Gets the main disk cache configuration.
        /// </summary>
        public DiskCacheConfig MainDiskCacheConfig
        {
            get
            {
                return _mainDiskCacheConfig;
            }
        }

        /// <summary>
        /// Gets the memory trimmable registry.
        /// </summary>
        public IMemoryTrimmableRegistry MemoryTrimmableRegistry
        {
            get
            {
                return _memoryTrimmableRegistry;
            }
        }

        /// <summary>
        /// Gets the network fletcher.
        /// </summary>
        public INetworkFetcher<FetchState> NetworkFetcher
        {
            get
            {
                return _networkFetcher;
            }
        }

        /// <summary>
        /// Gets the platform bitmap factory.
        /// </summary>
        public PlatformBitmapFactory PlatformBitmapFactory
        {
            get
            {
                return _platformBitmapFactory;
            }
        }

        /// <summary>
        /// Gets the pool factory.
        /// </summary>
        public PoolFactory PoolFactory
        {
            get
            {
                return _poolFactory;
            }
        }

        /// <summary>
        /// Gets the progressive jpeg configuration.
        /// </summary>
        public IProgressiveJpegConfig ProgressiveJpegConfig
        {
            get
            {
                return _progressiveJpegConfig;
            }
        }

        /// <summary>
        /// Gets the request listeners.
        /// </summary>
        public HashSet<IRequestListener> RequestListeners
        {
            get
            {
                // TODO: returns the readonly set
                return _requestListeners;
            }
        }

        /// <summary>
        /// Returns true if resize and rotate for network is enabled, otherwise false.
        /// </summary>
        public bool IsResizeAndRotateEnabledForNetwork
        {
            get
            {
                return _resizeAndRotateEnabledForNetwork;
            }
        }

        /// <summary>
        /// Gets the small image disk cache configuration.
        /// </summary>
        public DiskCacheConfig SmallImageDiskCacheConfig
        {
            get
            {
                return _smallImageDiskCacheConfig;
            }
        }

        /// <summary>
        /// Gets the imagepipeline experiments.
        /// </summary>
        public ImagePipelineExperiments Experiments
        {
            get
            {
                return _imagePipelineExperiments;
            }
        }

        private static DiskCacheConfig GetDefaultMainDiskCacheConfig()
        {
            return DiskCacheConfig.NewBuilder().Build();
        }

        /// <summary>
        /// Instantiates imagepipelineconfig builder.
        /// </summary>
        /// <returns></returns>
        public static Builder NewBuilder()
        {
            return new Builder();
        }

        /// <summary>
        /// Builder class for ImagePipelineConfig
        /// </summary>
        public class Builder
        {
            internal IAnimatedImageFactory AnimatedImageFactory { get; private set; }
            internal BitmapPixelFormat BitmapConfig { get; private set; }
            internal ISupplier<MemoryCacheParams> BitmapMemoryCacheParamsSupplier { get; private set; }
            internal ICacheKeyFactory CacheKeyFactory { get; private set; }
            internal bool IsDownsampleEnabled { get; private set; }
            internal bool IsDecodeMemoryFileEnabled { get; private set; }
            internal ISupplier<MemoryCacheParams> EncodedMemoryCacheParamsSupplier { get; private set; }
            internal IExecutorSupplier ExecutorSupplier { get; private set; }
            internal IImageCacheStatsTracker ImageCacheStatsTracker { get; private set; }
            internal ImageDecoder ImageDecoder { get; private set; }
            internal ISupplier<bool> IsPrefetchEnabledSupplier { get; private set; }
            internal DiskCacheConfig MainDiskCacheConfig { get; private set; }
            internal IMemoryTrimmableRegistry MemoryTrimmableRegistry { get; private set; }
            internal INetworkFetcher<FetchState> NetworkFetcher { get; private set; }
            internal PlatformBitmapFactory PlatformBitmapFactory { get; private set; }
            internal PoolFactory PoolFactory { get; private set; }
            internal IProgressiveJpegConfig ProgressiveJpegConfig { get; private set; }
            internal HashSet<IRequestListener> RequestListeners { get; private set; }
            internal bool ResizeAndRotateEnabledForNetwork { get; private set; } = true;
            internal DiskCacheConfig SmallImageDiskCacheConfig { get; private set; }
            internal IFileCacheFactory FileCacheFactory { get; private set; }

            private readonly ImagePipelineExperiments.Builder _experimentsBuilder;

            internal Builder()
            {
                _experimentsBuilder = new ImagePipelineExperiments.Builder(this);
            }

            /// <summary>
            /// Sets the animated image factory.
            /// </summary>
            public Builder SetAnimatedImageFactory(IAnimatedImageFactory animatedImageFactory)
            {
                AnimatedImageFactory = animatedImageFactory;
                return this;
            }

            /// <summary>
            /// Sets the bitmap pixel format.
            /// </summary>
            public Builder SetBitmapsConfig(BitmapPixelFormat config)
            {
                BitmapConfig = config;
                return this;
            }

            /// <summary>
            /// Sets the bitmap memory cache params supplier.
            /// </summary>
            public Builder SetBitmapMemoryCacheParamsSupplier(
                ISupplier<MemoryCacheParams> bitmapMemoryCacheParamsSupplier)
            {
                BitmapMemoryCacheParamsSupplier =
                    Preconditions.CheckNotNull(bitmapMemoryCacheParamsSupplier);

                return this;
            }

            /// <summary>
            /// Sets the cache key factory.
            /// </summary>
            public Builder SetCacheKeyFactory(ICacheKeyFactory cacheKeyFactory)
            {
                CacheKeyFactory = cacheKeyFactory;
                return this;
            }

            /// <summary>
            /// Enables decode memory file.
            /// </summary>
            public Builder SetDecodeMemoryFileEnabled(bool decodeMemoryFileEnabled)
            {
                IsDecodeMemoryFileEnabled = decodeMemoryFileEnabled;
                return this;
            }

            /// <summary>
            /// Sets the file cache factory.
            /// </summary>
            public Builder SetFileCacheFactory(IFileCacheFactory fileCacheFactory)
            {
                FileCacheFactory = fileCacheFactory;
                return this;
            }

            /// <summary>
            /// @deprecated use <see cref="SetFileCacheFactory"/> instead
            /// </summary>
            public Builder SetDiskStorageFactory(IDiskStorageFactory diskStorageFactory)
            {
                SetFileCacheFactory(new DiskStorageCacheFactory(diskStorageFactory));
                return this;
            }

            /// <summary>
            /// Enables downsample.
            /// </summary>
            /// <param name="downsampleEnabled"></param>
            /// <returns></returns>
            public Builder SetDownsampleEnabled(bool downsampleEnabled)
            {
                IsDownsampleEnabled = downsampleEnabled;
                return this;
            }

            /// <summary>
            /// Sets the encoded memory cache params supplier
            /// </summary>
            public Builder SetEncodedMemoryCacheParamsSupplier(
                ISupplier<MemoryCacheParams> encodedMemoryCacheParamsSupplier)
            {
                EncodedMemoryCacheParamsSupplier =
                    Preconditions.CheckNotNull(encodedMemoryCacheParamsSupplier);

                return this;
            }

            /// <summary>
            /// Sets the executor supplier.
            /// </summary>
            public Builder SetExecutorSupplier(IExecutorSupplier executorSupplier)
            {
                ExecutorSupplier = executorSupplier;
                return this;
            }

            /// <summary>
            /// Sets the image cache stats tracker.
            /// </summary>
            public Builder SetImageCacheStatsTracker(IImageCacheStatsTracker imageCacheStatsTracker)
            {
                ImageCacheStatsTracker = imageCacheStatsTracker;
                return this;
            }

            /// <summary>
            /// Sets the image decoder.
            /// </summary>
            public Builder SetImageDecoder(ImageDecoder imageDecoder)
            {
                ImageDecoder = imageDecoder;
                return this;
            }

            /// <summary>
            /// Sets the IsPrefetchEnabledSupplier.
            /// </summary>
            public Builder SetIsPrefetchEnabledSupplier(ISupplier<bool> isPrefetchEnabledSupplier)
            {
                IsPrefetchEnabledSupplier = isPrefetchEnabledSupplier;
                return this;
            }

            /// <summary>
            /// Sets the main disk cache configuration.
            /// </summary>
            public Builder SetMainDiskCacheConfig(DiskCacheConfig mainDiskCacheConfig)
            {
                MainDiskCacheConfig = mainDiskCacheConfig;
                return this;
            }

            /// <summary>
            /// Sets the memory trimmable registry.
            /// </summary>
            public Builder SetMemoryTrimmableRegistry(IMemoryTrimmableRegistry memoryTrimmableRegistry)
            {
                MemoryTrimmableRegistry = memoryTrimmableRegistry;
                return this;
            }

            /// <summary>
            /// Sets the network fletcher.
            /// </summary>
            public Builder SetNetworkFetcher(INetworkFetcher<FetchState> networkFetcher)
            {
                NetworkFetcher = networkFetcher;
                return this;
            }

            /// <summary>
            /// Sets the platform bitmap factory.
            /// </summary>
            public Builder SetPlatformBitmapFactory(PlatformBitmapFactory platformBitmapFactory)
            {
                PlatformBitmapFactory = platformBitmapFactory;
                return this;
            }

            /// <summary>
            /// Sets the pool factory.
            /// </summary>
            public Builder SetPoolFactory(PoolFactory poolFactory)
            {
                PoolFactory = poolFactory;
                return this;
            }

            /// <summary>
            /// Sets the progressive jpeg configuration.
            /// </summary>
            public Builder SetProgressiveJpegConfig(IProgressiveJpegConfig progressiveJpegConfig)
            {
                ProgressiveJpegConfig = progressiveJpegConfig;
                return this;
            }

            /// <summary>
            /// Sets the request listeners.
            /// </summary>
            public Builder SetRequestListeners(HashSet<IRequestListener> requestListeners)
            {
                RequestListeners = requestListeners;
                return this;
            }

            /// <summary>
            /// Enables resize and rotate for network.
            /// </summary>
            public Builder SetResizeAndRotateEnabledForNetwork(bool resizeAndRotateEnabledForNetwork)
            {
                ResizeAndRotateEnabledForNetwork = resizeAndRotateEnabledForNetwork;
                return this;
            }

            /// <summary>
            /// Sets the small image disk cache configuration.
            /// </summary>
            public Builder SetSmallImageDiskCacheConfig(DiskCacheConfig smallImageDiskCacheConfig)
            {
                SmallImageDiskCacheConfig = smallImageDiskCacheConfig;
                return this;
            }

            /// <summary>
            /// Gets the imagepipeline experiment builder.
            /// </summary>
            public ImagePipelineExperiments.Builder Experiment
            {
                get
                {
                    return _experimentsBuilder;
                }
            }

            /// <summary>
            /// Builds the imagepipeline configuration.
            /// </summary>
            public ImagePipelineConfig Build()
            {
                return new ImagePipelineConfig(this);
            }
        }
    }
}
