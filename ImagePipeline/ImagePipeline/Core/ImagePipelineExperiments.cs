namespace ImagePipeline.Core
{
    /// <summary>
    /// Encapsulates additional elements of the <see cref="ImagePipelineConfig"/> which are 
    /// currently in an experimental state.
    ///
    /// <para />These options may often change or disappear altogether and it is not recommended 
    /// to change their values from their defaults.
    /// </summary>
    public class ImagePipelineExperiments
    {
        internal readonly int _forceSmallCacheThresholdBytes;
        internal readonly bool _webpSupportEnabled;
        internal readonly int _throttlingMaxSimultaneousRequests;
        internal readonly bool _externalCreatedBitmapLogEnabled;

        private ImagePipelineExperiments(Builder builder, ImagePipelineConfig.Builder configBuilder)
        {
            _forceSmallCacheThresholdBytes = builder.ForceSmallCacheThresholdBytes;
            _webpSupportEnabled = builder.IsWebpSupportEnabled;
            _throttlingMaxSimultaneousRequests = builder.ThrottlingMaxSimultaneousRequests;
            _externalCreatedBitmapLogEnabled = builder.IsExternalCreatedBitmapLogEnabled;
        }

        /// <summary>
        /// Returns true if the external bitmap log is enabled, otherwise false.
        /// </summary>
        public bool IsExternalCreatedBitmapLogEnabled
        {
            get
            {
                return _externalCreatedBitmapLogEnabled;
            }
        }

        /// <summary>
        /// Gets the force small cache threshold bytes.
        /// </summary>
        /// <returns></returns>
        public int ForceSmallCacheThresholdBytes
        {
            get
            {
                return _forceSmallCacheThresholdBytes;
            }
        }

        /// <summary>
        /// Returns true if webp support is enabled, otherwise false.
        /// </summary>
        public bool IsWebpSupportEnabled
        {
            get
            {
                return _webpSupportEnabled;
            }
        }

        /// <summary>
        /// Gets the throttling max simultaneous requests.
        /// </summary>
        public int ThrottlingMaxSimultaneousRequests
        {
            get
            {
                return _throttlingMaxSimultaneousRequests;
            }
        }

        /// <summary>
        /// Creates the builder for ImagePipelineExperiments.
        /// </summary>
        public static Builder NewBuilder(
            ImagePipelineConfig.Builder configBuilder)
        {
            return new Builder(configBuilder);
        }

        /// <summary>
        /// Builder class for ImagePipelineExperiments
        /// </summary>
        public class Builder
        {
            private const int DEFAULT_MAX_SIMULTANEOUS_FILE_FETCH_AND_RESIZE = 5;

            internal ImagePipelineConfig.Builder ConfigBuilder { get; private set; }
            internal int ForceSmallCacheThresholdBytes { get; private set; }
            internal bool IsWebpSupportEnabled { get; private set; }
            internal bool IsExternalCreatedBitmapLogEnabled { get; private set; }
            internal int ThrottlingMaxSimultaneousRequests { get; private set; } = 
                DEFAULT_MAX_SIMULTANEOUS_FILE_FETCH_AND_RESIZE;

            /// <summary>
            /// Instantiates the ImagePipelineExperiments builder.
            /// </summary>
            /// <param name="configBuilder"></param>
            public Builder(ImagePipelineConfig.Builder configBuilder)
            {
                ConfigBuilder = configBuilder;
            }

            /// <summary>
            /// Enables the external bitmap log.
            /// </summary>
            public ImagePipelineConfig.Builder SetExternalCreatedBitmapLogEnabled(
                bool externalCreatedBitmapLogEnabled)
            {
                IsExternalCreatedBitmapLogEnabled = externalCreatedBitmapLogEnabled;
                return ConfigBuilder;
            }

            /// <summary>
            /// If this value is nonnegative, then all network-downloaded images below this 
            /// size will be written to the small image cache.
            ///
            /// <para />This will require the image pipeline to do up to two disk reads, instead 
            /// of one, before going out to network. Use only if this pattern makes sense for your 
            /// application.
            /// </summary>
            public ImagePipelineConfig.Builder SetForceSmallCacheThresholdBytes(
                int forceSmallCacheThresholdBytes)
            {
                ForceSmallCacheThresholdBytes = forceSmallCacheThresholdBytes;
                return ConfigBuilder;
            }

            /// <summary>
            /// Enables webp support.
            /// </summary>
            public ImagePipelineConfig.Builder SetWebpSupportEnabled(bool webpSupportEnabled)
            {
                IsWebpSupportEnabled = webpSupportEnabled;
                return ConfigBuilder;
            }

            /// <summary>
            /// Using this method is possible to change the max number of threads for loading 
            /// and sizing local images.
            /// <param name="throttlingMaxSimultaneousRequests">Max number of thread.</param>
            /// @return The Builder itself for chaining.
            /// </summary>
            public ImagePipelineConfig.Builder SetThrottlingMaxSimultaneousRequests(
                int throttlingMaxSimultaneousRequests)
            {
                ThrottlingMaxSimultaneousRequests = throttlingMaxSimultaneousRequests;
                return ConfigBuilder;
            }

            /// <summary>
            /// Builds the ImagePipelineExperiments.
            /// </summary>
            public ImagePipelineExperiments Build()
            {
                return new ImagePipelineExperiments(this, ConfigBuilder);
            }
        }
    }
}
