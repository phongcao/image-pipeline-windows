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
            BitmapPoolParams = builder._bitmapPoolParams ?? DefaultBitmapPoolParams.Get();
            BitmapPoolStatsTracker = builder._bitmapPoolStatsTracker ?? NoOpPoolStatsTracker.Instance;
            FlexByteArrayPoolParams = builder._flexByteArrayPoolParams ?? DefaultFlexByteArrayPoolParams.Get();
            MemoryTrimmableRegistry = builder._memoryTrimmableRegistry ?? NoOpMemoryTrimmableRegistry.Instance;
            NativeMemoryChunkPoolParams = builder._nativeMemoryChunkPoolParams ?? DefaultNativeMemoryChunkPoolParams.Get();
            NativeMemoryChunkPoolStatsTracker = builder._nativeMemoryChunkPoolStatsTracker ?? NoOpPoolStatsTracker.Instance;
            SmallByteArrayPoolParams = builder._smallByteArrayPoolParams ?? DefaultByteArrayPoolParams.Get();
            SmallByteArrayPoolStatsTracker = builder._smallByteArrayPoolStatsTracker ?? NoOpPoolStatsTracker.Instance;
        }

        /// <summary>
        /// Builder class factory method
        /// </summary>
        /// <returns></returns>
        public static Builder NewBuilder()
        {
            return new Builder();
        }

        /// <summary>
        /// Builder class for PoolConfig
        /// </summary>
        public class Builder
        {
            internal PoolParams _bitmapPoolParams;
            internal PoolStatsTracker _bitmapPoolStatsTracker;
            internal PoolParams _flexByteArrayPoolParams;
            internal IMemoryTrimmableRegistry _memoryTrimmableRegistry;
            internal PoolParams _nativeMemoryChunkPoolParams;
            internal PoolStatsTracker _nativeMemoryChunkPoolStatsTracker;
            internal PoolParams _smallByteArrayPoolParams;
            internal PoolStatsTracker _smallByteArrayPoolStatsTracker;

            internal Builder()
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
