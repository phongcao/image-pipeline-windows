using FBCore.Common.Internal;
using FBCore.Common.Memory;
using System;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Configuration class for pools.
    /// </summary>
    public class PoolConfig
    {
        /// <summary>
        /// There are a lot of parameters in this class. Please follow strict alphabetical order.
        /// </summary>
        
        // Factory methods
        private static Func<Builder> _newBuilder;
        private static Func<Builder, PoolParams> _bitmapPoolParamsBuilder;
        private static Func<Builder, PoolStatsTracker> _bitmapPoolStatsTrackerBuilder;
        private static Func<Builder, PoolParams> _flexByteArrayPoolParamsBuilder;
        private static Func<Builder, IMemoryTrimmableRegistry> _memoryTrimmableRegistryBuilder;
        private static Func<Builder, PoolParams> _nativeMemoryChunkPoolParamsBuilder;
        private static Func<Builder, PoolStatsTracker> _nativeMemoryChunkPoolStatsTrackerBuilder;
        private static Func<Builder, PoolParams> _smallByteArrayPoolParamsBuilder;
        private static Func<Builder, PoolStatsTracker> _smallByteArrayPoolStatsTrackerBuilder;

        /// <summary>
        /// Gets the <see cref="BitmapPool"/> params
        /// </summary>
        public PoolParams BitmapPoolParams { get;  }

        /// <summary>
        /// Gets the <see cref="BitmapPool"/> stats tracker
        /// </summary>
        public PoolStatsTracker BitmapPoolStatsTracker { get; }

        /// <summary>
        /// Gets the <see cref="FlexByteArrayPool"/> params
        /// </summary>
        public PoolParams FlexByteArrayPoolParams { get; }

        /// <summary>
        /// Gets the <see cref="IMemoryTrimmableRegistry"/>
        /// </summary>
        public IMemoryTrimmableRegistry MemoryTrimmableRegistry { get; }

        /// <summary>
        /// Gets the <see cref="NativeMemoryChunkPool"/> params
        /// </summary>
        public PoolParams NativeMemoryChunkPoolParams { get; }

        /// <summary>
        /// Gets the <see cref="NativeMemoryChunkPool"/> stats tracker
        /// </summary>
        public PoolStatsTracker NativeMemoryChunkPoolStatsTracker { get; }

        /// <summary>
        /// Gets the <see cref="GenericByteArrayPool"/> params
        /// </summary>
        public PoolParams SmallByteArrayPoolParams { get; }

        /// <summary>
        /// Gets the <see cref="GenericByteArrayPool"/> stats tracker
        /// </summary>
        public PoolStatsTracker SmallByteArrayPoolStatsTracker { get; }

        private PoolConfig(Builder builder)
        {
            BitmapPoolParams = _bitmapPoolParamsBuilder(builder) ?? DefaultBitmapPoolParams.Get();
            BitmapPoolStatsTracker = _bitmapPoolStatsTrackerBuilder(builder) ?? NoOpPoolStatsTracker.Instance;
            FlexByteArrayPoolParams = _flexByteArrayPoolParamsBuilder(builder) ?? DefaultFlexByteArrayPoolParams.Get();
            MemoryTrimmableRegistry = _memoryTrimmableRegistryBuilder(builder) ?? NoOpMemoryTrimmableRegistry.Instance;
            NativeMemoryChunkPoolParams = _nativeMemoryChunkPoolParamsBuilder(builder) ?? DefaultNativeMemoryChunkPoolParams.Get();
            NativeMemoryChunkPoolStatsTracker = _nativeMemoryChunkPoolStatsTrackerBuilder(builder) ?? NoOpPoolStatsTracker.Instance;
            SmallByteArrayPoolParams = _smallByteArrayPoolParamsBuilder(builder) ?? DefaultByteArrayPoolParams.Get();
            SmallByteArrayPoolStatsTracker = _smallByteArrayPoolStatsTrackerBuilder(builder) ?? NoOpPoolStatsTracker.Instance;
        }

        /// <summary>
        /// Builder class factory method
        /// </summary>
        /// <returns></returns>
        public static Builder NewBuilder()
        {
            return _newBuilder();
        }

        /// <summary>
        /// Builder class for PoolConfig
        /// </summary>
        public class Builder
        {
            private PoolParams _bitmapPoolParams;
            private PoolStatsTracker _bitmapPoolStatsTracker;
            private PoolParams _flexByteArrayPoolParams;
            private IMemoryTrimmableRegistry _memoryTrimmableRegistry;
            private PoolParams _nativeMemoryChunkPoolParams;
            private PoolStatsTracker _nativeMemoryChunkPoolStatsTracker;
            private PoolParams _smallByteArrayPoolParams;
            private PoolStatsTracker _smallByteArrayPoolStatsTracker;
            
            static Builder()
            {
                _newBuilder = () => new Builder();
                _bitmapPoolParamsBuilder = b => b._bitmapPoolParams;
                _bitmapPoolStatsTrackerBuilder = b => b._bitmapPoolStatsTracker;
                _flexByteArrayPoolParamsBuilder = b => b._flexByteArrayPoolParams;
                _memoryTrimmableRegistryBuilder = b => b._memoryTrimmableRegistry;
                _nativeMemoryChunkPoolParamsBuilder = b => b._nativeMemoryChunkPoolParams;
                _nativeMemoryChunkPoolStatsTrackerBuilder = b => b._nativeMemoryChunkPoolStatsTracker;
                _smallByteArrayPoolParamsBuilder = b => b._smallByteArrayPoolParams;
                _smallByteArrayPoolStatsTrackerBuilder = b => b._smallByteArrayPoolStatsTracker;
            }

            private Builder()
            {
            }

            /// <summary>
            /// Sets the <see cref="BitmapPool"/> params
            /// </summary>
            /// <param name="bitmapPoolParams"></param>
            /// <returns></returns>
            public Builder SetBitmapPoolParams(PoolParams bitmapPoolParams)
            {
                _bitmapPoolParams = Preconditions.CheckNotNull(bitmapPoolParams);
                return this;
            }

            /// <summary>
            /// Sets the <see cref="BitmapPool"/> stats tracker
            /// </summary>
            /// <param name="bitmapPoolStatsTracker"></param>
            /// <returns></returns>
            public Builder SetBitmapPoolStatsTracker(
                PoolStatsTracker bitmapPoolStatsTracker)
            {
                _bitmapPoolStatsTracker = Preconditions.CheckNotNull(bitmapPoolStatsTracker);
                return this;
            }

            /// <summary>
            /// Sets the <see cref="FlexByteArrayPool"/> params
            /// </summary>
            /// <param name="flexByteArrayPoolParams"></param>
            /// <returns></returns>
            public Builder SetFlexByteArrayPoolParams(PoolParams flexByteArrayPoolParams)
            {
                _flexByteArrayPoolParams = flexByteArrayPoolParams;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="IMemoryTrimmableRegistry"/>
            /// </summary>
            /// <param name="memoryTrimmableRegistry"></param>
            /// <returns></returns>
            public Builder SetMemoryTrimmableRegistry(IMemoryTrimmableRegistry memoryTrimmableRegistry)
            {
                _memoryTrimmableRegistry = memoryTrimmableRegistry;
                return this;
            }

            /// <summary>
            /// Sets the <see cref="NativeMemoryChunkPool"/> params
            /// </summary>
            /// <param name="nativeMemoryChunkPoolParams"></param>
            /// <returns></returns>
            public Builder SetNativeMemoryChunkPoolParams(PoolParams nativeMemoryChunkPoolParams)
            {
                _nativeMemoryChunkPoolParams = Preconditions.CheckNotNull(nativeMemoryChunkPoolParams);
                return this;
            }

            /// <summary>
            /// Sets the <see cref="NativeMemoryChunkPool"/> stats tracker
            /// </summary>
            /// <param name="nativeMemoryChunkPoolStatsTracker"></param>
            /// <returns></returns>
            public Builder SetNativeMemoryChunkPoolStatsTracker(
                PoolStatsTracker nativeMemoryChunkPoolStatsTracker)
            {
                _nativeMemoryChunkPoolStatsTracker =
                    Preconditions.CheckNotNull(nativeMemoryChunkPoolStatsTracker);
                return this;
            }

            /// <summary>
            /// Sets the small <see cref="GenericByteArrayPool"/> params
            /// </summary>
            /// <param name="commonByteArrayPoolParams"></param>
            /// <returns></returns>
            public Builder SetSmallByteArrayPoolParams(PoolParams commonByteArrayPoolParams)
            {
                _smallByteArrayPoolParams = Preconditions.CheckNotNull(commonByteArrayPoolParams);
                return this;
            }

            /// <summary>
            /// Sets the small <see cref="GenericByteArrayPool"/> stats tracker
            /// </summary>
            /// <param name="smallByteArrayPoolStatsTracker"></param>
            /// <returns></returns>
            public Builder SetSmallByteArrayPoolStatsTracker(
                PoolStatsTracker smallByteArrayPoolStatsTracker)
            {
                _smallByteArrayPoolStatsTracker =
                    Preconditions.CheckNotNull(smallByteArrayPoolStatsTracker);
                return this;
            }

            /// <summary>
            /// Builds the pool config
            /// </summary>
            /// <returns></returns>
            public PoolConfig Build()
            {
                return new PoolConfig(this);
            }
        }
    }
}
