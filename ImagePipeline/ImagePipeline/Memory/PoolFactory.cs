using FBCore.Common.Internal;
using ImagePipelineBase.ImagePipeline.Memory;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Factory class for pools.
    /// </summary>
    public class PoolFactory
    {
        private readonly PoolConfig _config;
        private BitmapPool _bitmapPool;
        private FlexByteArrayPool _flexByteArrayPool;
        private NativeMemoryChunkPool _nativeMemoryChunkPool;
        private IPooledByteBufferFactory _pooledByteBufferFactory;
        private PooledByteStreams _pooledByteStreams;
        private SharedByteArray _sharedByteArray;
        private IByteArrayPool _smallByteArrayPool;

        /// <summary>
        /// Instantiates the <see cref="PoolFactory"/>.
        /// </summary>
        /// <param name="config"></param>
        public PoolFactory(PoolConfig config)
        {
            _config = Preconditions.CheckNotNull(config);
        }

        /// <summary>
        /// Creates the <see cref="BitmapPool"/> using config
        /// </summary>
        public BitmapPool BitmapPool
        {
            get
            {
                if (_bitmapPool == null)
                {
                    _bitmapPool = new BitmapPool(
                        _config.MemoryTrimmableRegistry,
                        _config.BitmapPoolParams,
                        _config.BitmapPoolStatsTracker);
                }

                return _bitmapPool;
            }
        }

        /// <summary>
        /// Creates the <see cref="FlexByteArrayPool"/> using config
        /// </summary>
        public FlexByteArrayPool FlexByteArrayPool
        {
            get
            {
                if (_flexByteArrayPool == null)
                {
                    _flexByteArrayPool = new FlexByteArrayPool(
                        _config.MemoryTrimmableRegistry,
                        _config.FlexByteArrayPoolParams);
                }

                return _flexByteArrayPool;
            }
        }

        /// <summary>
        /// Gets the maximum threads
        /// </summary>
        public int FlexByteArrayPoolMaxNumThreads
        {
            get
            {
                return _config.FlexByteArrayPoolParams.MaxNumThreads;
            }
        }

        /// <summary>
        /// Creates the <see cref="NativeMemoryChunkPool"/> using config
        /// </summary>
        public NativeMemoryChunkPool NativeMemoryChunkPool
        {
            get
            {
                if (_nativeMemoryChunkPool == null)
                {
                    _nativeMemoryChunkPool = new NativeMemoryChunkPool(
                        _config.MemoryTrimmableRegistry,
                        _config.NativeMemoryChunkPoolParams,
                        _config.NativeMemoryChunkPoolStatsTracker);
                }

                return _nativeMemoryChunkPool;
            }
        }

        /// <summary>
        /// Creates the <see cref="PooledByteBufferFactory"/>
        /// </summary>
        public IPooledByteBufferFactory PooledByteBufferFactory
        {
            get
            {
                if (_pooledByteBufferFactory == null)
                {
                    _pooledByteBufferFactory = new NativePooledByteBufferFactory(
                        NativeMemoryChunkPool,
                        PooledByteStreams);
                }

                return _pooledByteBufferFactory;
            }
        }

        /// <summary>
        /// Creates the <see cref="PooledByteStreams"/>
        /// </summary>
        public PooledByteStreams PooledByteStreams
        {
            get
            {
                if (_pooledByteStreams == null)
                {
                    _pooledByteStreams = new PooledByteStreams(SmallByteArrayPool);
                }

                return _pooledByteStreams;
            }
        }

        /// <summary>
        /// Creates the <see cref="SharedByteArray"/> using config
        /// </summary>
        public SharedByteArray SharedByteArray
        {
            get
            {
                if (_sharedByteArray == null)
                {
                    _sharedByteArray = new SharedByteArray(
                        _config.MemoryTrimmableRegistry,
                        _config.FlexByteArrayPoolParams);
                }

                return _sharedByteArray;
            }
        }

        /// <summary>
        /// Creates the <see cref="SmallByteArrayPool"/> using config
        /// </summary>
        public IByteArrayPool SmallByteArrayPool
        {
            get
            {
                if (_smallByteArrayPool == null)
                {
                    _smallByteArrayPool = new GenericByteArrayPool(
                        _config.MemoryTrimmableRegistry,
                        _config.SmallByteArrayPoolParams,
                        _config.SmallByteArrayPoolStatsTracker);
                }

                return _smallByteArrayPool;
            }
        }
    }
}
