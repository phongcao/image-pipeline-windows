using BinaryResource;
using Cache.Common;
using Cache.Disk;
using FBCore.Common.References;
using ImagePipeline.Cache;
using ImagePipeline.Core;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ImagePipeline.Tests.Cache
{
    /// <summary>
    /// Tests for <see cref="BufferedDiskCache"/>
    /// </summary>
    [TestClass]
    public class BufferedDiskCacheTests
    {
        private IFileCache _fileCache;
        private IPooledByteBufferFactory _byteBufferFactory;
        private PooledByteStreams _pooledByteStreams;
        private StagingArea _stagingArea;
        private IImageCacheStatsTracker _imageCacheStatsTracker;
        private IPooledByteBuffer _pooledByteBuffer;
        private Stream _inputStream;
        private IBinaryResource _binaryResource;

        private MultiCacheKey _cacheKey;
        private CancellationTokenSource _token;
        private BufferedDiskCache _bufferedDiskCache;
        private CloseableReference<IPooledByteBuffer> _closeableReference;
        private EncodedImage _encodedImage;
        private IExecutorService _readPriorityExecutor;
        private IExecutorService _writePriorityExecutor;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            IFileCacheFactory fileCacheFactory = new DiskStorageCacheFactory(new DynamicDefaultDiskStorageFactory());
            _fileCache = fileCacheFactory.Get(DiskCacheConfig.NewBuilder().Build());

            _closeableReference = CloseableReference<IPooledByteBuffer>.of(_pooledByteBuffer);
            _encodedImage = new EncodedImage(_closeableReference);
            IList<ICacheKey> keys = new List<ICacheKey>();
            keys.Add(new SimpleCacheKey("http://test.uri"));
            keys.Add(new SimpleCacheKey("http://tyrone.uri"));
            keys.Add(new SimpleCacheKey("http://ian.uri"));
            _cacheKey = new MultiCacheKey(keys);
        }

        [TestMethod]
        public void Test()
        {

        }
    }
}
