using BinaryResource;
using Cache.Common;
using Cache.Disk;
using FBCore.Common.Disk;
using FBCore.Common.Internal;
using FBCore.Common.Time;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace ImagePipelineBase.Tests.Cache.Disk
{
    /// <summary>
    /// Test for <see cref="DiskStorageCache"/>
    /// </summary>
    [TestClass]
    public class DiskStorageCacheTests
    {
        private const string CACHE_TYPE = "media_test";

        private const int TESTCACHE_VERSION_START_OF_VERSIONING = 1;
        private const int TESTCACHE_CURRENT_VERSION = TESTCACHE_VERSION_START_OF_VERSIONING;
        private const int TESTCACHE_NEXT_VERSION = TESTCACHE_CURRENT_VERSION + 1;

        /// <summary>
        /// The threshold (in bytes) for the size of file cache
        /// </summary>
        private const long FILE_CACHE_MAX_SIZE_HIGH_LIMIT = 200;
        private const long FILE_CACHE_MAX_SIZE_LOW_LIMIT = 200;

        private DirectoryInfo _cacheDirectory;
        private IDiskStorage _storage;
        private DiskStorageCache _cache;
        private IDiskTrimmableRegistry _diskTrimmableRegistry;
        private ICacheEventListener _cacheEventListener;
        private MockSystemClock _clock;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _clock = MockSystemClock.Get();
            _clock.SetDateTime(DateTime.Now);
            NoOpDiskTrimmableRegistry.Instance.ResetCounter();
            _diskTrimmableRegistry = NoOpDiskTrimmableRegistry.Instance;
            _cacheEventListener = new DuplicatingCacheEventListener();

            // We know the directory will be this
            _cacheDirectory = new DirectoryInfo(
                Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, CACHE_TYPE));
            _cacheDirectory.Create();
            if (!_cacheDirectory.Exists)
            {
                throw new Exception(
                    string.Format(
                        "Cannot create cache dir: {0}: directory {1}",
                        ApplicationData.Current.LocalCacheFolder.Path,
                        _cacheDirectory.Exists ? "already exists" : "does not exist"));
            }

            _storage = CreateDiskStorage(TESTCACHE_VERSION_START_OF_VERSIONING);
            _cache = CreateDiskCache(_storage, false);
            Assert.IsTrue(((NoOpDiskTrimmableRegistry)_diskTrimmableRegistry).RegisterDiskTrimmableCount == 1);
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            _cache.ClearAll();
        }

        private IDiskStorage CreateDiskStorage(int version)
        {
            return new DiskStorageWithReadFailures(
                version,
                Suppliers.of<FileSystemInfo>(
                    new DirectoryInfo(ApplicationData.Current.LocalCacheFolder.Path)),
                CACHE_TYPE,
                NoOpCacheErrorLogger.Instance,
                _clock);
        }

        private DiskStorageCache CreateDiskCache(
            IDiskStorage diskStorage,
            bool indexPopulateAtStartupEnabled)
        {
            Params diskStorageCacheParams = new Params(
                0,
                FILE_CACHE_MAX_SIZE_LOW_LIMIT,
                FILE_CACHE_MAX_SIZE_HIGH_LIMIT);

            return new DiskStorageCache(
                diskStorage,
                new DefaultEntryEvictionComparatorSupplier(),
                diskStorageCacheParams,
                _cacheEventListener,
                NoOpCacheErrorLogger.Instance,
                _diskTrimmableRegistry,
                indexPopulateAtStartupEnabled,
                _clock);
        }

        /// <summary>
        /// Tests cache event listener
        /// </summary>
        [TestMethod]
        public void TestCacheEventListener()
        {
            // 1. Add first cache file
            ICacheKey key1 = new SimpleCacheKey("foo");
            int value1Size = 101;
            byte[] value1 = new byte[value1Size];
            value1[80] = 99; // 'c', just so it's not all zeros for the equality test below.
            IBinaryResource resource1 = _cache.Insert(key1, WriterCallbacks.From(value1));

            VerifyListenerOnWriteAttempt(key1);
            string resourceId1 = VerifyListenerOnWriteSuccessAndGetResourceId(key1, value1Size);

            IBinaryResource resource1Again = _cache.GetResource(key1);
            Assert.AreEqual(((FileBinaryResource)resource1).File.FullName, 
                ((FileBinaryResource)resource1Again).File.FullName);
            VerifyListenerOnHit(key1, resourceId1);

            IBinaryResource resource1Again2 = _cache.GetResource(key1);
            Assert.AreEqual(((FileBinaryResource)resource1).File.FullName,
                ((FileBinaryResource)resource1Again2).File.FullName);
            VerifyListenerOnHit(key1, resourceId1);

            SimpleCacheKey missingKey = new SimpleCacheKey("nonexistent_key");
            IBinaryResource res2 = _cache.GetResource(missingKey);
            Assert.IsNull(res2);
            VerifyListenerOnMiss(missingKey);

            DuplicatingCacheEventListener listener = (DuplicatingCacheEventListener)_cacheEventListener;
            listener.Clear();
            _cache.ClearAll();
            Assert.IsTrue(listener.GetEvents("OnCleared").Count != 0);
            Assert.IsTrue(listener.GetEvents("OnHit").Count == 0);
            Assert.IsTrue(listener.GetEvents("OnMiss").Count == 0);
            Assert.IsTrue(listener.GetEvents("OnWriteAttempt").Count == 0);
            Assert.IsTrue(listener.GetEvents("OnWriteSuccess").Count == 0);
            Assert.IsTrue(listener.GetEvents("OnReadException").Count == 0);
            Assert.IsTrue(listener.GetEvents("OnWriteException").Count == 0);
            Assert.IsTrue(listener.GetEvents("OnEviction").Count == 0);
        }

        /// <summary>
        /// Tests size based file eviction of cache files. Also tests that unexpected
        /// files (which are not in the format expected by the cache) do not count
        /// towards the cache size, and are also evicted during both evictions (LRU and Old).
        ///
        /// @throws Exception
        /// </summary>
        [TestMethod]
        public void TestCacheFile()
        {
            if (!_cacheDirectory.Exists)
            {
                throw new Exception("Cannot create cache dir");
            }
                       
            // Write non-cache, non-lru file in the cache directory
            FileInfo unexpected1 = new FileInfo(
                Path.Combine(_cacheDirectory.FullName, "unexpected1"));
            using (FileStream unexpected1Fs = unexpected1.OpenWrite())
            {
                unexpected1Fs.Write(new byte[110], 0, 110);
            }

            // Touch the non-cache, non-lru file, and assert that it succeeds.
            _clock.SetDateTime(DateTime.Now.AddHours(1));

            try
            {
                unexpected1.LastWriteTime = _clock.Now;
            }
            catch (Exception)
            {
                Assert.Fail();
            }

            // 1. Add first cache file
            ICacheKey key1 = new SimpleCacheKey("foo");
            byte[] value1 = new byte[101];
            value1[80] = 99; // 'c', just so it's not all zeros for the equality test below.
            _cache.Insert(key1, WriterCallbacks.From(value1));

            // Verify resource
            CollectionAssert.AreEqual(value1, GetContents(GetResource(key1)));

            // 1. Touch the LRU file, and assert that it succeeds.
            _clock.SetDateTime(DateTime.Now.AddHours(2));
            Assert.IsTrue(_cache.Probe(key1));

            // The cache size should be the size of the first file only
            // The unexpected files should not count towards size
            Assert.IsTrue(_cache.Size == 101);

            // Write another non-cache, non-lru file in the cache directory
            FileInfo unexpected2 = new FileInfo(
                Path.Combine(_cacheDirectory.FullName, "unexpected2"));
            using (FileStream unexpected2Fs = unexpected2.OpenWrite())
            {
                unexpected2Fs.Write(new byte[120], 0, 120);
            }

            // Touch the non-cache, non-lru file, and assert that it succeeds.
            _clock.SetDateTime(DateTime.Now.AddHours(3));

            try
            {
                unexpected2.LastWriteTime = _clock.Now;
            }
            catch (Exception)
            {
                Assert.Fail();
            }

            // 2. Add second cache file
            ICacheKey key2 = new SimpleCacheKey("bar");
            byte[] value2 = new byte[102];
            value2[80] = 100; // 'd', just so it's not all zeros for the equality test below.
            _cache.Insert(key2, WriterCallbacks.From(value2));

            // 2. Touch the LRU file, and assert that it succeeds.
            _clock.SetDateTime(DateTime.Now.AddHours(4));
            Assert.IsTrue(_cache.Probe(key2));

            // The cache size should be the size of the first + second cache files
            // The unexpected files should not count towards size
            Assert.IsTrue(_cache.Size == 203);

            // At this point, the filecache size has exceeded
            // FILE_CACHE_MAX_SIZE_HIGH_LIMIT. However, eviction will be triggered
            // only when the next value will be inserted (to be more particular,
            // before the next value is inserted).

            // 3. Add third cache file
            ICacheKey key3 = new SimpleCacheKey("foobar");
            byte[] value3 = new byte[103];
            value3[80] = 101; // 'e', just so it's not all zeros for the equality test below.
            _cache.Insert(key3, WriterCallbacks.From(value3));

            // At this point, the first file should have been evicted. Only the
            // files associated with the second and third entries should be in cache.

            // 1. Verify that the first cache, lru files are deleted
            Assert.IsNull(GetResource(key1));

            // Verify the first unexpected file is deleted, but that eviction stops
            // before the second unexpected file
            Assert.IsFalse(unexpected1.Exists);
            Assert.IsFalse(unexpected2.Exists);

            // 2. Verify the second cache, lru files exist
            CollectionAssert.AreEqual(value2, GetContents(GetResource(key2)));

            // 3. Verify that cache, lru files for third entry still exists
            CollectionAssert.AreEqual(value3, GetContents(GetResource(key3)));

            // The cache size should be the size of the second + third files
            Assert.IsTrue(_cache.Size == 205, $"Expected cache size of 205 but is { _cache.Size }");

            // Write another non-cache, non-lru file in the cache directory
            FileInfo unexpected3 = new FileInfo(
                Path.Combine(_cacheDirectory.FullName, "unexpected3"));
            using (FileStream unexpected3Fs = unexpected3.OpenWrite())
            {
                unexpected3Fs.Write(new byte[120], 0, 120);
            }

            Assert.IsTrue(unexpected3.Exists);

            // After a clear, cache file size should be uninitialized (-1)
            _cache.ClearAll();
            Assert.AreEqual(-1, _cache.Size);
            unexpected3.Refresh();
            Assert.IsFalse(unexpected3.Exists);
            Assert.IsNull(GetResource(key2));
            Assert.IsNull(GetResource(key3));
        }

        /// <summary>
        /// Tests multi cache keys
        /// </summary>
        [TestMethod]
        public void TestWithMultiCacheKeys()
        {
            ICacheKey insertKey1 = new SimpleCacheKey("foo");
            byte[] value1 = new byte[101];
            value1[50] = 97; // 'a', just so it's not all zeros for the equality test below.
            _cache.Insert(insertKey1, WriterCallbacks.From(value1));

            List<ICacheKey> keys1 = new List<ICacheKey>(2);
            keys1.Add(new SimpleCacheKey("bar"));
            keys1.Add(new SimpleCacheKey("foo"));
            MultiCacheKey matchingMultiKey = new MultiCacheKey(keys1);
            CollectionAssert.AreEqual(value1, GetContents(_cache.GetResource(matchingMultiKey)));

            List<ICacheKey> keys2 = new List<ICacheKey>(2);
            keys2.Add(new SimpleCacheKey("one"));
            keys2.Add(new SimpleCacheKey("two"));
            MultiCacheKey insertKey2 = new MultiCacheKey(keys2);
            byte[] value2 = new byte[101];
            value1[50] = 98; // 'b', just so it's not all zeros for the equality test below.
            _cache.Insert(insertKey2, WriterCallbacks.From(value2));

            ICacheKey matchingSimpleKey = new SimpleCacheKey("one");
            CollectionAssert.AreEqual(value2, GetContents(_cache.GetResource(matchingSimpleKey)));
        }

        /// <summary>
        /// Tests cache file with IOException
        /// </summary>
        [TestMethod]
        public void TestCacheFileWithIOException()
        {
            ICacheKey key1 = new SimpleCacheKey("aaa");

            // Before inserting, make sure files not exist.
            IBinaryResource resource1 = GetResource(key1);
            Assert.IsNull(resource1);

            // 1. Should not create cache files if IOException happens in the middle.
            IOException writeException = new IOException();
            try
            {
                _cache.Insert(key1, new WriterCallbackImpl(os =>
                {
                    throw writeException;
                }));

                Assert.Fail();
            }
            catch (IOException)
            {
                Assert.IsNull(GetResource(key1));
            }

            VerifyListenerOnWriteAttempt(key1);
            VerifyListenerOnWriteException(key1, writeException);

            // 2. Test a read failure from DiskStorage
            ICacheKey key2 = new SimpleCacheKey("bbb");
            int value2Size = 42;
            byte[] value2 = new byte[value2Size];
            value2[25] = 98; // 'b'
            _cache.Insert(key2, WriterCallbacks.From(value2));

            VerifyListenerOnWriteAttempt(key2);
            string resourceId2 = VerifyListenerOnWriteSuccessAndGetResourceId(key2, value2Size);

            ((DiskStorageWithReadFailures)_storage).SetPoisonResourceId(resourceId2);

            Assert.IsNull(_cache.GetResource(key2));
            VerifyListenerOnReadException(key2, DiskStorageWithReadFailures.POISON_EXCEPTION);

            Assert.IsFalse(_cache.Probe(key2));
            VerifyListenerOnReadException(key2, DiskStorageWithReadFailures.POISON_EXCEPTION);

            DuplicatingCacheEventListener listener = (DuplicatingCacheEventListener)_cacheEventListener;
            Assert.IsTrue(listener.GetEvents("OnCleared").Count == 0);
            Assert.IsTrue(listener.GetEvents("OnHit").Count == 0);
            Assert.IsTrue(listener.GetEvents("OnMiss").Count == 0);
            Assert.IsTrue(listener.GetEvents("OnEviction").Count == 0);
        }

        /// <summary>
        /// Tests clean the old cache
        /// </summary>
        [TestMethod]
        public void TestCleanOldCache()
        {
            TimeSpan cacheExpiration = TimeSpan.FromDays(5);
            ICacheKey key1 = new SimpleCacheKey("aaa");
            int value1Size = 41;
            byte[] value1 = new byte[value1Size];
            value1[25] = 97; // 'a'
            _cache.Insert(key1, WriterCallbacks.From(value1));

            string resourceId1 = VerifyListenerOnWriteSuccessAndGetResourceId(key1, value1Size);

            ICacheKey key2 = new SimpleCacheKey("bbb");
            int value2Size = 42;
            byte[] value2 = new byte[value2Size];
            value2[25] = 98; // 'b'
            _cache.Insert(key2, WriterCallbacks.From(value2));

            string resourceId2 = VerifyListenerOnWriteSuccessAndGetResourceId(key2, value2Size);

            // Increment clock by default expiration time + 1 day
            _clock.SetDateTime(DateTime.Now.Add(cacheExpiration + TimeSpan.FromDays(1)));

            ICacheKey key3 = new SimpleCacheKey("ccc");
            int value3Size = 43;
            byte[] value3 = new byte[value3Size];
            value3[25] = 99; // 'c'
            _cache.Insert(key3, WriterCallbacks.From(value3));
            TimeSpan valueAge3 = TimeSpan.FromHours(1);
            _clock.SetDateTime(DateTime.Now.Add(cacheExpiration + TimeSpan.FromDays(1) + valueAge3));

            long oldestEntry = _cache.ClearOldEntries((long)cacheExpiration.TotalMilliseconds);
            Assert.IsTrue(valueAge3.TotalSeconds == oldestEntry / 1000);

            CollectionAssert.AreEqual(value3, GetContents(GetResource(key3)));
            Assert.IsNull(GetResource(key1));
            Assert.IsNull(GetResource(key2));

            string[] resourceIds = new string[] { resourceId1, resourceId2 };
            long[] itemSizes = new long[] { value1Size, value2Size };
            long cacheSizeBeforeEviction = value1Size + value2Size + value3Size;
            VerifyListenerOnEviction(
                resourceIds,
                itemSizes,
                EvictionReason.CONTENT_STALE,
                cacheSizeBeforeEviction);
        }

        /// <summary>
        /// Tests clean all old entries in the cache
        /// </summary>
        [TestMethod]
        public void TestCleanOldCacheNoEntriesRemaining()
        {
            TimeSpan cacheExpiration = TimeSpan.FromDays(5);
            ICacheKey key1 = new SimpleCacheKey("aaa");
            byte[] value1 = new byte[41];
            _cache.Insert(key1, WriterCallbacks.From(value1));

            ICacheKey key2 = new SimpleCacheKey("bbb");
            byte[] value2 = new byte[42];
            _cache.Insert(key2, WriterCallbacks.From(value2));

            // Increment clock by default expiration time + 1 day
            _clock.SetDateTime(DateTime.Now.Add(cacheExpiration + TimeSpan.FromDays(1)));

            long oldestEntry = _cache.ClearOldEntries((long)cacheExpiration.TotalMilliseconds);
            Assert.IsTrue(0 == oldestEntry);
        }

        /// <summary>
        /// Test to make sure that the same item stored with two different versions
        /// of the cache will be stored with two different file names.
        ///
        /// @throws UnsupportedEncodingException
        /// </summary>
        [TestMethod]
        public void TestVersioning()
        {
            // Define data that will be written to cache
            ICacheKey key = new SimpleCacheKey("version_test");
            byte[] value = new byte[32];
            value[0] = 118; // 'v'

            // Set up cache with version == 1
            IDiskStorage storage1 = CreateDiskStorage(TESTCACHE_CURRENT_VERSION);
            DiskStorageCache cache1 = CreateDiskCache(storage1, false);

            // Write test data to cache 1
            cache1.Insert(key, WriterCallbacks.From(value));

            // Get cached file
            IBinaryResource resource1 = GetResource(storage1, key);
            Assert.IsNotNull(resource1);

            // Set up cache with version == 2
            IDiskStorage storageSupplier2 = CreateDiskStorage(TESTCACHE_NEXT_VERSION);
            DiskStorageCache cache2 = CreateDiskCache(storageSupplier2, false);

            // Write test data to cache 2
            cache2.Insert(key, WriterCallbacks.From(value));

            // Get cached file
            IBinaryResource resource2 = GetResource(storageSupplier2, key);
            Assert.IsNotNull(resource2);

            // Make sure filenames of the two file are different
            Assert.IsFalse(resource2.Equals(resource1));
        }

        /// <summary>
        /// Tests storage enabled
        /// </summary>
        [TestMethod]
        public void TestIsEnabled()
        {
            IDiskStorage storage = CreateDiskStorage(TESTCACHE_CURRENT_VERSION);
            DiskStorageCache cache = CreateDiskCache(storage, false);
            Assert.IsTrue(cache.IsEnabled);
        }

        /// <summary>
        /// Verify that multiple threads can write to the cache at the same time.
        /// </summary>
        [TestMethod]
        public void TestConcurrency()
        {
            IList<Task> tasks = new List<Task>();

            using (Barrier barrier = new Barrier(3))
            {
                WriterCallbackImpl writerCallback = new WriterCallbackImpl((os) =>
                {
                    try
                    {
                        // Both threads will need to hit this barrier. If writing is serialized,
                        // the second thread will never reach here as the first will hold
                        // the write lock forever.
                        barrier.SignalAndWait();
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                });

                ICacheKey key1 = new SimpleCacheKey("concurrent1");
                ICacheKey key2 = new SimpleCacheKey("concurrent2");
                Task t1 = RunInsertionInSeparateThread(key1, writerCallback);
                tasks.Add(t1);
                Task t2 = RunInsertionInSeparateThread(key2, writerCallback);
                tasks.Add(t2);
                barrier.SignalAndWait();             
            }

            Task.WaitAll(tasks.ToArray());
        }

        private Task RunInsertionInSeparateThread(ICacheKey key, IWriterCallback callback)
        {
            return Task.Run(() =>
            {
                try
                {
                    _cache.Insert(key, callback);
                }
                catch (IOException)
                {
                    Assert.Fail();
                }
            });
        }

        /// <summary>
        /// Tests insertion
        /// </summary>
        [TestMethod]
        public void TestInsertionInIndex()
        {
            ICacheKey key = PutOneThingInCache();
            Assert.IsTrue(_cache.HasKeySync(key));
            Assert.IsTrue(_cache.HasKey(key));
        }

        /// <summary>
        /// Tests non-existing key
        /// </summary>
        [TestMethod]
        public void TestDoesntHaveKey()
        {
            ICacheKey key = new SimpleCacheKey("foo");
            Assert.IsFalse(_cache.HasKeySync(key));
            Assert.IsFalse(_cache.HasKey(key));
        }

        /// <summary>
        /// Tests existing key + no populate at startup + not awaiting for index
        /// </summary>
        [TestMethod]
        public void TestHasKeyWithoutPopulateAtStartupWithoutAwaitingIndex()
        {
            ICacheKey key = PutOneThingInCache();

            // A new cache object in the same directory. Equivalent to a process restart.
            // Index may not yet updated.
            DiskStorageCache cache2 = CreateDiskCache(_storage, false);
            Assert.IsFalse(cache2.HasKeySync(key));
            Assert.IsTrue(cache2.HasKey(key));

            // HasKey() adds item to the index
            Assert.IsTrue(cache2.HasKeySync(key));
        }

        /// <summary>
        /// Tests existing key + no populate at startup + awaiting for index
        /// </summary>
        [TestMethod]
        public void TestHasKeyWithoutPopulateAtStartupWithAwaitingIndex()
        {
            ICacheKey key = PutOneThingInCache();

            // A new cache object in the same directory. Equivalent to a process restart.
            // Index may not yet updated.
            DiskStorageCache cache2 = CreateDiskCache(_storage, false);

            // Wait for index populated in cache before use of cache
            cache2.AwaitIndex();
            Assert.IsTrue(cache2.HasKey(key));
            Assert.IsTrue(cache2.HasKeySync(key));
        }

        /// <summary>
        /// Tests existing key + populate at startup + awaiting for index
        /// </summary>
        [TestMethod]
        public void TestHasKeyWithPopulateAtStartupWithAwaitingIndex()
        {
            ICacheKey key = PutOneThingInCache();

            // A new cache object in the same directory. Equivalent to a process restart.
            // Index should be updated.
            DiskStorageCache cache2 = CreateDiskCache(_storage, true);

            // Wait for index populated in cache before use of cache
            cache2.AwaitIndex();
            Assert.IsTrue(cache2.HasKeySync(key));
            Assert.IsTrue(cache2.HasKey(key));
        }

        /// <summary>
        /// Tests existing key + populate at startup + not awaiting for index
        /// </summary>
        [TestMethod]
        public void TestHasKeyWithPopulateAtStartupWithoutAwaitingIndex()
        {
            ICacheKey key = PutOneThingInCache();

            // A new cache object in the same directory. Equivalent to a process restart.
            // Index may not yet updated.
            DiskStorageCache cache2 = CreateDiskCache(_storage, true);
            Assert.IsTrue(cache2.HasKey(key));
            Assert.IsTrue(cache2.HasKeySync(key));
        }

        /// <summary>
        /// Tests out getting resource without awaiting for index
        /// </summary>
        [TestMethod]
        public void TestGetResourceWithoutAwaitingIndex()
        {
            ICacheKey key = PutOneThingInCache();

            // A new cache object in the same directory. Equivalent to a process restart.
            // Index may not yet updated.
            DiskStorageCache cache2 = CreateDiskCache(_storage, false);
            Assert.IsNotNull(cache2.GetResource(key));
        }

        /// <summary>
        /// Tests clear index
        /// </summary>
        [TestMethod]
        public void TestClearIndex()
        {
            ICacheKey key = PutOneThingInCache();
            _cache.ClearAll();
            Assert.IsFalse(_cache.HasKeySync(key));
            Assert.IsFalse(_cache.HasKey(key));
        }

        /// <summary>
        /// Tests remove file + clear index
        /// </summary>
        [TestMethod]
        public void TestRemoveFileClearsIndex()
        {
            ICacheKey key = PutOneThingInCache();
            _storage.ClearAll();
            Assert.IsNull(_cache.GetResource(key));
            Assert.IsFalse(_cache.HasKeySync(key));
        }

        /// <summary>
        /// Tests out size eviction and clear index
        /// </summary>
        [TestMethod]
        public void TestSizeEvictionClearsIndex()
        {
            _clock.SetDateTime(DateTime.Now.Add(TimeSpan.FromDays(1)));
            ICacheKey key1 = PutOneThingInCache();
            ICacheKey key2 = new SimpleCacheKey("bar");
            ICacheKey key3 = new SimpleCacheKey("duck");
            byte[] value2 = new byte[(int)FILE_CACHE_MAX_SIZE_HIGH_LIMIT];
            value2[80] = 99; // 'c'
            IWriterCallback callback = WriterCallbacks.From(value2);
            _clock.SetDateTime(DateTime.Now.Add(TimeSpan.FromDays(2)));
            _cache.Insert(key2, callback);

            // Now over limit. Next write will evict key1
            _clock.SetDateTime(DateTime.Now.Add(TimeSpan.FromDays(3)));
            _cache.Insert(key3, callback);
            Assert.IsFalse(_cache.HasKeySync(key1));
            Assert.IsFalse(_cache.HasKey(key1));
            Assert.IsTrue(_cache.HasKeySync(key3));
            Assert.IsTrue(_cache.HasKey(key3));
        }

        /// <summary>
        /// Tests out time eviction and clear index
        /// </summary>
        [TestMethod]
        public void TestTimeEvictionClearsIndex()
        {
            ICacheKey key = PutOneThingInCache();
            _clock.SetDateTime(DateTime.Now.Add(TimeSpan.FromDays(5)));
            _cache.ClearOldEntries((long)TimeSpan.FromDays(4).TotalMilliseconds);
            Assert.IsFalse(_cache.HasKeySync(key));
            Assert.IsFalse(_cache.HasKey(key));
        }

        private ICacheKey PutOneThingInCache()
        {
            ICacheKey key = new SimpleCacheKey("foo");
            byte[] value1 = new byte[101];
            value1[80] = 99; // 'c'
            _cache.Insert(key, WriterCallbacks.From(value1));
            return key;
        }

        private void VerifyListenerOnHit(ICacheKey key, string resourceId)
        {
            DuplicatingCacheEventListener listener = (DuplicatingCacheEventListener)_cacheEventListener;
            Assert.IsTrue(listener.GetEvents("OnHit").Count != 0);
            SettableCacheEvent cacheEvent = (SettableCacheEvent)listener.GetLastEvent("OnHit");
            Assert.IsNotNull(cacheEvent);
            Assert.AreEqual(cacheEvent.CacheKey, key);
            Assert.AreEqual(cacheEvent.ResourceId, resourceId);
        }

        private void VerifyListenerOnMiss(ICacheKey key)
        {
            DuplicatingCacheEventListener listener = (DuplicatingCacheEventListener)_cacheEventListener;
            Assert.IsTrue(listener.GetEvents("OnMiss").Count != 0);
            SettableCacheEvent cacheEvent = (SettableCacheEvent)listener.GetLastEvent("OnMiss");
            Assert.IsNotNull(cacheEvent);
            Assert.AreEqual(cacheEvent.CacheKey, key);
        }

        private void VerifyListenerOnWriteAttempt(ICacheKey key)
        {
            DuplicatingCacheEventListener listener = (DuplicatingCacheEventListener)_cacheEventListener;
            Assert.IsTrue(listener.GetEvents("OnWriteAttempt").Count != 0);
            SettableCacheEvent cacheEvent = (SettableCacheEvent)listener.GetLastEvent("OnWriteAttempt");
            Assert.IsNotNull(cacheEvent);
            Assert.AreEqual(cacheEvent.CacheKey, key);
        }

        private string VerifyListenerOnWriteSuccessAndGetResourceId(
            ICacheKey key,
            long itemSize)
        {
            DuplicatingCacheEventListener listener = (DuplicatingCacheEventListener)_cacheEventListener;
            Assert.IsTrue(listener.GetEvents("OnWriteSuccess").Count != 0);
            SettableCacheEvent cacheEvent = (SettableCacheEvent)listener.GetLastEvent("OnWriteSuccess");
            Assert.IsNotNull(cacheEvent);
            Assert.AreEqual(cacheEvent.CacheKey, key);
            Assert.AreEqual(cacheEvent.ItemSize, itemSize);
            Assert.IsNotNull(cacheEvent.ResourceId);
            return cacheEvent.ResourceId;
        }

        private void VerifyListenerOnWriteException(ICacheKey key, IOException exception)
        {
            DuplicatingCacheEventListener listener = (DuplicatingCacheEventListener)_cacheEventListener;
            Assert.IsTrue(listener.GetEvents("OnWriteException").Count != 0);
            SettableCacheEvent cacheEvent = (SettableCacheEvent)listener.GetLastEvent("OnWriteException");
            Assert.IsNotNull(cacheEvent);
            Assert.AreEqual(cacheEvent.CacheKey, key);
            Assert.IsNotNull(cacheEvent.Exception);
        }

        private void VerifyListenerOnReadException(ICacheKey key, IOException exception)
        {
            DuplicatingCacheEventListener listener = (DuplicatingCacheEventListener)_cacheEventListener;
            Assert.IsTrue(listener.GetEvents("OnReadException").Count != 0);
            SettableCacheEvent cacheEvent = (SettableCacheEvent)listener.GetLastEvent("OnReadException");
            Assert.IsNotNull(cacheEvent);
            Assert.AreEqual(cacheEvent.CacheKey, key);
            Assert.IsNotNull(cacheEvent.Exception);
        }

        private void VerifyListenerOnEviction(
            string[] resourceIds,
            long[] itemSizes,
            EvictionReason reason,
            long cacheSizeBeforeEviction)
        {
            int numberItems = resourceIds.Length;
            DuplicatingCacheEventListener listener = (DuplicatingCacheEventListener)_cacheEventListener;
            IList<ICacheEvent> cacheEvents = listener.GetEvents("OnEviction");
            Assert.IsTrue(cacheEvents.Count == numberItems);

            bool[] found = new bool[numberItems];
            long runningCacheSize = cacheSizeBeforeEviction;

            // The eviction order is unknown so make allowances for them coming in different orders
            foreach (var cacheEvent in cacheEvents)
            {
                Assert.IsNotNull(cacheEvent);
                SettableCacheEvent settableCacheEvent = (SettableCacheEvent)cacheEvent;
                for (int i = 0; i < numberItems; i++)
                {
                    if (!found[i] && resourceIds[i].Equals(settableCacheEvent.ResourceId))
                    {
                        found[i] = true;
                        Assert.IsTrue(settableCacheEvent.ItemSize != 0);
                        Assert.IsNotNull(settableCacheEvent.EvictionReason);
                    }
                }

                runningCacheSize -= settableCacheEvent.ItemSize;
                Assert.IsTrue(settableCacheEvent.CacheSize == runningCacheSize);
            }

            // Ensure all resources were found
            for (int i = 0; i < numberItems; i++)
            {
                Assert.IsTrue(found[i], $"Expected eviction of resource { resourceIds[i] } but wasn't evicted");
            }
        }

        class DiskStorageWithReadFailures : DynamicDefaultDiskStorage
        {
            public static readonly IOException POISON_EXCEPTION = 
                new IOException("Poisoned resource requested");

            private string _poisonResourceId;

            public DiskStorageWithReadFailures(
                int version,
                ISupplier<FileSystemInfo> baseDirectoryPathSupplier,
                string baseDirectoryName, 
                ICacheErrorLogger cacheErrorLogger,
                Clock clock = null) : base(
                    version, 
                    baseDirectoryPathSupplier, 
                    baseDirectoryName, 
                    cacheErrorLogger,
                    clock)
            {
            }

            public void SetPoisonResourceId(string poisonResourceId)
            {
                _poisonResourceId = poisonResourceId;
            }

            public override IBinaryResource GetResource(string resourceId, object debugInfo)
            {
                if (resourceId.Equals(_poisonResourceId))
                {
                    throw POISON_EXCEPTION;
                }

                return Get().GetResource(resourceId, debugInfo);
            }

            public override bool Touch(string resourceId, object debugInfo)
            {
                if (resourceId.Equals(_poisonResourceId))
                {
                    throw POISON_EXCEPTION;
                }

                return base.Touch(resourceId, debugInfo);
            }
        }

        private IBinaryResource GetResource(IDiskStorage storage, ICacheKey key)
        {
            return storage.GetResource(CacheKeyUtil.GetFirstResourceId(key), key);
        }

        private IBinaryResource GetResource(ICacheKey key)
        {
            return _storage.GetResource(CacheKeyUtil.GetFirstResourceId(key), key);
        }

        private byte[] GetContents(IBinaryResource resource)
        {
            using (Stream file = resource.OpenStream())
            {
                byte[] contents = ByteStreams.ToByteArray(file);
                return contents;
            }
        }

        /// <summary>
        /// CacheEventListener implementation which copies the data from each event into a new instance to
        /// work-around the recycling of the original event and forwards the copy so that assertions can be
        /// made afterwards.
        /// </summary>
        class DuplicatingCacheEventListener : ICacheEventListener
        {
            private IDictionary<string, IList<ICacheEvent>> _cacheEvents;

            public DuplicatingCacheEventListener()
            {
                _cacheEvents = new Dictionary<string, IList<ICacheEvent>>();
                _cacheEvents.Add("OnHit", new List<ICacheEvent>());
                _cacheEvents.Add("OnMiss", new List<ICacheEvent>());
                _cacheEvents.Add("OnWriteAttempt", new List<ICacheEvent>());
                _cacheEvents.Add("OnWriteSuccess", new List<ICacheEvent>());
                _cacheEvents.Add("OnReadException", new List<ICacheEvent>());
                _cacheEvents.Add("OnWriteException", new List<ICacheEvent>());
                _cacheEvents.Add("OnEviction", new List<ICacheEvent>());
                _cacheEvents.Add("OnCleared", new List<ICacheEvent>());
            }

            public void OnHit(ICacheEvent cacheEvent)
            {
                _cacheEvents["OnHit"].Add(DuplicateEvent(cacheEvent));
            }

            public void OnMiss(ICacheEvent cacheEvent)
            {
                _cacheEvents["OnMiss"].Add(DuplicateEvent(cacheEvent));
            }

            public void OnWriteAttempt(ICacheEvent cacheEvent)
            {
                _cacheEvents["OnWriteAttempt"].Add(DuplicateEvent(cacheEvent));
            }

            public void OnWriteSuccess(ICacheEvent cacheEvent)
            {
                _cacheEvents["OnWriteSuccess"].Add(DuplicateEvent(cacheEvent));
            }

            public void OnReadException(ICacheEvent cacheEvent)
            {
                _cacheEvents["OnReadException"].Add(DuplicateEvent(cacheEvent));
            }

            public void OnWriteException(ICacheEvent cacheEvent)
            {
                _cacheEvents["OnWriteException"].Add(DuplicateEvent(cacheEvent));
            }

            public void OnEviction(ICacheEvent cacheEvent)
            {
                _cacheEvents["OnEviction"].Add(DuplicateEvent(cacheEvent));
            }

            public void OnCleared()
            {
                _cacheEvents["OnCleared"].Add(default(ICacheEvent));
            }

            public void ClearEvents(string key)
            {
                IList<ICacheEvent> events = default(IList<ICacheEvent>);
                _cacheEvents.TryGetValue(key, out events);
                if (events != null)
                {
                    events.Clear();
                }
            }

            public void Clear()
            {
                foreach (var entry in _cacheEvents)
                {
                    entry.Value.Clear();
                }
            }

            public IList<ICacheEvent> GetEvents(string key)
            {
                IList<ICacheEvent> events = default(IList<ICacheEvent>);
                _cacheEvents.TryGetValue(key, out events);
                return events;
            }

            public ICacheEvent GetLastEvent(string key)
            {
                IList<ICacheEvent> events = default(IList<ICacheEvent>);
                _cacheEvents.TryGetValue(key, out events);
                if (events != null)
                {
                    return events.LastOrDefault();
                }

                return default(ICacheEvent);
            }

            private ICacheEvent DuplicateEvent(ICacheEvent cacheEvent)
            {
                SettableCacheEvent copyEvent = SettableCacheEvent.Obtain();
                copyEvent.SetCacheKey(cacheEvent.CacheKey);
                copyEvent.SetCacheLimit(cacheEvent.CacheLimit);
                copyEvent.SetCacheSize(cacheEvent.CacheSize);
                copyEvent.SetEvictionReason(cacheEvent.EvictionReason);
                copyEvent.SetException(cacheEvent.Exception);
                copyEvent.SetItemSize(cacheEvent.ItemSize);
                copyEvent.SetResourceId(cacheEvent.ResourceId);
                return copyEvent;
            }
        }
    }
}
