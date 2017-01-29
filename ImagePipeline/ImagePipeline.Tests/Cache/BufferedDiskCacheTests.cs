using Cache.Common;
using Cache.Disk;
using FBCore.Common.Internal;
using FBCore.Common.References;
using FBCore.Concurrency;
using ImagePipeline.Cache;
using ImagePipeline.Core;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace ImagePipeline.Tests.Cache
{
    /// <summary>
    /// Tests for <see cref="BufferedDiskCache"/>
    /// </summary>
    [TestClass]
    public class BufferedDiskCacheTests
    {
        private IFileCache _fileCache;
        private IFileCacheFactory _fileCacheFactory;
        private PoolFactory _poolFactory;
        private IPooledByteBufferFactory _byteBufferFactory;
        private PooledByteStreams _pooledByteStreams;
        private StagingArea _stagingArea;
        private IImageCacheStatsTracker _imageCacheStatsTracker;
        private IPooledByteBuffer _pooledByteBuffer;
        private MultiCacheKey _cacheKey;
        private AtomicBoolean _isCancelled;
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
            // Initializes the IFileCache
            _fileCacheFactory = new DiskStorageCacheFactory(new DynamicDefaultDiskStorageFactory());
            _fileCache = _fileCacheFactory.Get(DiskCacheConfig.NewBuilder().Build());

            // Initializes the IPooledByteBufferFactory and PooledByteStreams
            _poolFactory = new PoolFactory(PoolConfig.NewBuilder().Build());
            _byteBufferFactory = _poolFactory.PooledByteBufferFactory;
            _pooledByteStreams = _poolFactory.PooledByteStreams;

            // Initializes the IPooledByteBuffer from an image
            var file = StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/SplashScreen.scale-200.png")).GetAwaiter().GetResult();
            using (var stream = file.OpenReadAsync().GetAwaiter().GetResult())
            {        
                _pooledByteBuffer = _byteBufferFactory.NewByteBuffer(
                    ByteStreams.ToByteArray(stream.AsStream()));
            }

            _closeableReference = CloseableReference<IPooledByteBuffer>.of(_pooledByteBuffer);
            _encodedImage = new EncodedImage(_closeableReference);
            _stagingArea = StagingArea.Instance;
            _imageCacheStatsTracker = NoOpImageCacheStatsTracker.Instance;

            // Initializes the cache keys
            IList<ICacheKey> keys = new List<ICacheKey>();
            keys.Add(new SimpleCacheKey("http://test.uri"));
            keys.Add(new SimpleCacheKey("http://tyrone.uri"));
            keys.Add(new SimpleCacheKey("http://ian.uri"));
            _cacheKey = new MultiCacheKey(keys);

            // Initializes the executors
            _isCancelled = new AtomicBoolean(false);
            _readPriorityExecutor = Executors.NewFixedThreadPool(1);
            _writePriorityExecutor = Executors.NewFixedThreadPool(1);

            // Initializes the disk cache
            _bufferedDiskCache = new BufferedDiskCache(
                _fileCache,
                _byteBufferFactory,
                _pooledByteStreams,
                _readPriorityExecutor,
                _writePriorityExecutor,
                _imageCacheStatsTracker);
        }

        /// <summary>
        /// Tests adding key to file cache
        /// </summary>
        [TestMethod]
        public async Task TestHasKeySyncFromFileCache()
        {
            await _bufferedDiskCache.Put(_cacheKey, _encodedImage);
            Assert.IsTrue(_bufferedDiskCache.ContainsSync(_cacheKey));
            Assert.IsFalse(_stagingArea.ContainsKey(_cacheKey));
        }

        /// <summary>
        /// Tests adding key to staging area
        /// </summary>
        [TestMethod]
        public async Task TestHasKeySyncFromStagingArea()
        {
            Task addImage = _bufferedDiskCache.Put(_cacheKey, _encodedImage);
            Assert.IsTrue(_bufferedDiskCache.ContainsSync(_cacheKey));
            Assert.IsTrue(_stagingArea.ContainsKey(_cacheKey));
            Assert.IsFalse(addImage.IsCompleted);
            await addImage;
        }

        /// <summary>
        /// Tests adding key to disk storage
        /// </summary>
        [TestMethod]
        public async Task TestDoesntAlwaysHaveKeySync()
        {
            await _bufferedDiskCache.Put(_cacheKey, _encodedImage);
            _fileCache = _fileCacheFactory.Get(DiskCacheConfig.NewBuilder().Build());
            _stagingArea.ClearAll();
            _bufferedDiskCache = new BufferedDiskCache(
                _fileCache,
                _byteBufferFactory,
                _pooledByteStreams,
                _readPriorityExecutor,
                _writePriorityExecutor,
                _imageCacheStatsTracker);
            Assert.IsFalse(_bufferedDiskCache.ContainsSync(_cacheKey));
            Assert.IsTrue(await _bufferedDiskCache.Contains(_cacheKey));
        }

        /// <summary>
        /// Tests adding key to disk storage
        /// </summary>
        [TestMethod]
        public async Task TestSyncDiskCacheCheck()
        {
            await _bufferedDiskCache.Put(_cacheKey, _encodedImage);
            _fileCache = _fileCacheFactory.Get(DiskCacheConfig.NewBuilder().Build());
            _stagingArea.ClearAll();
            _bufferedDiskCache = new BufferedDiskCache(
                _fileCache,
                _byteBufferFactory,
                _pooledByteStreams,
                _readPriorityExecutor,
                _writePriorityExecutor,
                _imageCacheStatsTracker);
            Assert.IsTrue(_bufferedDiskCache.DiskCheckSync(_cacheKey));
        }

        /// <summary>
        /// Tests querying disk cache
        /// </summary>
        [TestMethod]
        public async Task TestQueriesDiskCache()
        {
            await _bufferedDiskCache.Put(_cacheKey, _encodedImage);
            EncodedImage image = await _bufferedDiskCache.Get(_cacheKey, _isCancelled);
            Assert.IsTrue(2 == image.GetByteBufferRef().GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
            byte[] buf1 = new byte[_pooledByteBuffer.Size];
            _pooledByteBuffer.Read(0, buf1, 0, _pooledByteBuffer.Size);
            byte[] buf2 = new byte[image.GetByteBufferRef().Get().Size];
            image.GetByteBufferRef().Get().Read(0, buf2, 0, image.GetByteBufferRef().Get().Size);
            CollectionAssert.AreEqual(buf1, buf2);
        }

        /// <summary>
        /// Tests cancellation
        /// </summary>
        [TestMethod]
        public async Task TestCacheGetCancellation()
        {
            await _bufferedDiskCache.Put(_cacheKey, _encodedImage);

            try
            {
                _isCancelled.Value = true;
                await _bufferedDiskCache.Get(_cacheKey, _isCancelled);
                Assert.Fail();
            }
            catch (Exception)
            {
                // This is expected
            }
        }

        /// <summary>
        /// Tests write to disk cache
        /// </summary>
        [TestMethod]
        public async Task TestWritesToDiskCache()
        {
            await _bufferedDiskCache.Put(_cacheKey, _encodedImage);

            // Ref count should be equal to 2 ('owned' by the _closeableReference and other 'owned' by
            // _encodedImage)
            Assert.IsTrue(2 == _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
        }

        /// <summary>
        /// Tests cache miss
        /// </summary>
        [TestMethod]
        public async Task TestCacheMiss()
        {
            ICacheKey cacheKey = new SimpleCacheKey("http://hello.uri");
            EncodedImage image = await _bufferedDiskCache.Get(cacheKey, _isCancelled);
            Assert.IsNull(image);
        }

        /// <summary>
        /// Tests out the references management
        /// </summary>
        [TestMethod]
        public async Task TestManagesReference()
        {
            await _bufferedDiskCache.Put(_cacheKey, _encodedImage);
            Assert.IsTrue(2 == _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
        }

        /// <summary>
        /// Tests pin to staging area
        /// </summary>
        [TestMethod]
        public async Task TestPins()
        {
            Task putTask = _bufferedDiskCache.Put(_cacheKey, _encodedImage);
            Assert.IsTrue(_stagingArea.ContainsKey(_cacheKey));
            await putTask;
        }

        /// <summary>
        /// Tests get from staging area
        /// </summary>
        [TestMethod]
        public async Task TestFromStagingArea()
        {
            Task putTask = _bufferedDiskCache.Put(_cacheKey, _encodedImage);         
            EncodedImage image = _stagingArea.Get(_cacheKey);
            Assert.AreSame(
                _closeableReference.GetUnderlyingReferenceTestOnly(),
                image.GetByteBufferRef().GetUnderlyingReferenceTestOnly());
            await putTask;
        }

        /// <summary>
        /// Tests get from staging area
        /// </summary>
        [TestMethod]
        public async Task TestFromStagingAreaLater()
        {
            await _bufferedDiskCache.Put(_cacheKey, _encodedImage);
            EncodedImage image = await _bufferedDiskCache.Get(_cacheKey, _isCancelled);
            Assert.IsTrue(_encodedImage.Size == image.Size);

            // Ref count should be equal to 3 (One for _closeableReference, one that is cloned when
            // _encodedImage is created and a third one that is cloned when the method GetByteBufferRef is
            // called in EncodedImage).
            Assert.IsTrue(3 == _encodedImage.GetByteBufferRef().GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
        }

        /// <summary>
        /// Tests unpin from staging area
        /// </summary>
        [TestMethod]
        public async Task TestUnpins()
        {
            await _bufferedDiskCache.Put(_cacheKey, _encodedImage);
            Assert.IsFalse(_stagingArea.ContainsKey(_cacheKey));
        }

        /// <summary>
        /// Tests contains from staging area
        /// </summary>
        [TestMethod]
        public async Task TestContainsFromStagingAreaLater()
        {
            Task putTask = _bufferedDiskCache.Put(_cacheKey, _encodedImage);
            Assert.IsTrue(_bufferedDiskCache.Contains(_cacheKey).GetAwaiter().GetResult());
            Assert.IsTrue(_stagingArea.ContainsKey(_cacheKey));
            await putTask;
        }

        /// <summary>
        /// Tests remove from staging area
        /// </summary>
        [TestMethod]
        public async Task TestRemoveFromStagingArea()
        {
            await _bufferedDiskCache.Put(_cacheKey, _encodedImage);
            await _bufferedDiskCache.Remove(_cacheKey);
            Assert.IsTrue(0 != _stagingArea._removeCallsTestOnly);
        }

        /// <summary>
        /// Tests clear from staging area
        /// </summary>
        [TestMethod]
        public async Task TestClearFromStagingArea()
        {
            await _bufferedDiskCache.Put(_cacheKey, _encodedImage);
            await _bufferedDiskCache.ClearAll();
            Assert.IsTrue(0 != _stagingArea._clearAllCallsTestOnly);
        }
    }
}
