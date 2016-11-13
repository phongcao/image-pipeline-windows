using FBCore.Common.Memory;
using FBCore.Common.References;
using ImagePipeline.Cache;
using ImagePipeline.Memory;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using Windows.Graphics.Imaging;

namespace ImagePipelineBase.Tests.ImagePipeline.Cache
{
    /// <summary>
    /// Tests for <see cref="CountingMemoryCache{K, V}"/>
    /// </summary>
    [TestClass]
    public class CountingMemoryCacheTests
    {
        private const int CACHE_MAX_SIZE = 1200;
        private const int CACHE_MAX_COUNT = 4;
        private const int CACHE_EVICTION_QUEUE_MAX_SIZE = 1100;
        private const int CACHE_EVICTION_QUEUE_MAX_COUNT = 3;
        private const int CACHE_ENTRY_MAX_SIZE = 1000;

        private IResourceReleaser<int> _releaser;
        private int _releaseCallCount;
        private IList<int> _releaseValues;
        private double _trimRatio;
        private ICacheTrimStrategy _cacheTrimStrategy;
        private MockSupplier<MemoryCacheParams> _paramsSupplier;
        private int _onExclusivityChangedCallCount;
        private bool? _isExclusive;
        private IEntryStateObserver<string> _entryStateObserver;
        private SoftwareBitmap _bitmap;

        private IValueDescriptor<int> _valueDescriptor;
        private MemoryCacheParams _params;
        private CountingMemoryCache<string, int> _cache;
        private MockPlatformBitmapFactory _platformBitmapFactory;
        private CloseableReference<SoftwareBitmap> _bitmapReference;

        private static readonly string KEY = "KEY";
        private static readonly string[] KEYS = 
            new string[] {"k0", "k1", "k2", "k3", "k4", "k5", "k6", "k7", "k8", "k9"};

        private static readonly IResourceReleaser<SoftwareBitmap> BITMAP_RESOURCE_RELEASER =
            new ResourceReleaserHelper<SoftwareBitmap>(b => b.Dispose());

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _releaseCallCount = 0;
            _releaseValues = new List<int>();
            _releaser = new ResourceReleaserHelper<int>(
                v =>
                {
                    ++_releaseCallCount;
                    _releaseValues.Add(v);
                });

            _onExclusivityChangedCallCount = 0;
            _isExclusive = null;
            _entryStateObserver = new EntryStateObserverHelper<string>(
                (v, b) =>
                {
                    ++_onExclusivityChangedCallCount;
                    _isExclusive = b;
                });

            _cacheTrimStrategy = new CacheTrimStrategyHelper(v => _trimRatio);
            _valueDescriptor = new ValueDescriptorHelper<int>(v => v);
            _params = new MemoryCacheParams(
                CACHE_MAX_SIZE,
                CACHE_MAX_COUNT,
                CACHE_EVICTION_QUEUE_MAX_SIZE,
                CACHE_EVICTION_QUEUE_MAX_COUNT,
                CACHE_ENTRY_MAX_SIZE);
            _paramsSupplier = new MockSupplier<MemoryCacheParams>(_params);
            _platformBitmapFactory = new MockPlatformBitmapFactory();
            _bitmap = new SoftwareBitmap(BitmapPixelFormat.Rgba8, 50, 50);
            _bitmapReference = CloseableReference<SoftwareBitmap>.of(
                _bitmap, BITMAP_RESOURCE_RELEASER);
            _cache = new CountingMemoryCache<string, int>(
                _valueDescriptor,
                _cacheTrimStrategy,
                _paramsSupplier,
                _platformBitmapFactory,
                true);
        }

        /// <summary>
        /// Tests out the SetCreationListener method
        /// </summary>
        [TestMethod]
        public void TestSetCreationListener()
        {
            Assert.IsNotNull(_platformBitmapFactory.BitmapCreationObserver);
        }

        /// <summary>
        /// Tests out the AddBitmapReference method
        /// </summary>
        [TestMethod]
        public void TestAddBitmapReference()
        {
            using (CloseableReference<SoftwareBitmap> bitmapReference = _platformBitmapFactory.CreateBitmap(50, 50))
            {
                Assert.IsNotNull(bitmapReference);
                Assert.AreEqual(1, _platformBitmapFactory.AddBitmapReferenceCallCount);
                Assert.AreEqual(bitmapReference.Get(), _platformBitmapFactory.Bitmap);
            }
        }

        /// <summary>
        /// Tests out the OnBitmapCreated method
        /// </summary>
        [TestMethod]
        public void TestOnBitmapCreated()
        {
            _platformBitmapFactory.AddBitmapReference(_bitmapReference.Get(), null);
            object callerContext = null;     
            Assert.IsTrue(_cache._otherEntries.TryGetValue(_bitmapReference.Get(), out callerContext));
        }

        /// <summary>
        ///  Tests out the Cache method
        /// </summary>
        [TestMethod]
        public void TestCache()
        {
            _cache.Cache(KEY, NewReference(100));
            AssertTotalSize(1, 100);
            AssertExclusivelyOwnedSize(0, 0);
            AssertSharedWithCount(KEY, 100, 1);
            Assert.AreEqual(0, _releaseCallCount);
        }

        /// <summary>
        /// Tests out the Dispose method of the original reference
        /// </summary>
        [TestMethod]
        public void TestClosingOriginalReference()
        {
            CloseableReference<int> originalRef = NewReference(100);
            _cache.Cache(KEY, originalRef);
            // cache should make its own copy and closing the original reference after caching
            // should not affect the cached value
            originalRef.Dispose();
            AssertTotalSize(1, 100);
            AssertExclusivelyOwnedSize(0, 0);
            AssertSharedWithCount(KEY, 100, 1);
            Assert.AreEqual(0, _releaseCallCount);
        }

        /// <summary>
        /// Tests out the Dispose method of the cache reference
        /// </summary>
        [TestMethod]
        public void TestClosingClientReference()
        {
            CloseableReference<int> cachedRef = _cache.Cache(KEY, NewReference(100));
            // cached item should get exclusively owned
            cachedRef.Dispose();
            AssertTotalSize(1, 100);
            AssertExclusivelyOwnedSize(1, 100);
            AssertExclusivelyOwned(KEY, 100);
            Assert.AreEqual(0, _releaseCallCount);
        }

        /// <summary>
        /// Tests out the OnExclusivityChanged method
        /// </summary>
        [TestMethod]
        public void TestNotExclusiveAtFirst()
        {
            _cache.Cache(KEY, NewReference(100), _entryStateObserver);
            Assert.AreEqual(0, _onExclusivityChangedCallCount);
        }

        /// <summary>
        /// Tests out the OnExclusivityChanged method
        /// </summary>
        [TestMethod]
        public void TestToggleExclusive()
        {
            CloseableReference<int> cachedRef = _cache.Cache(KEY, NewReference(100), _entryStateObserver);
            cachedRef.Dispose();
            Assert.IsTrue(_isExclusive ?? false);
            _cache.Get(KEY);
            Assert.IsFalse(_isExclusive ?? true);
        }

        /// <summary>
        /// Tests out the OnExclusivityChanged method
        /// </summary>
        [TestMethod]
        public void TestCantReuseNonExclusive()
        {
            _cache.Cache(KEY, NewReference(100), _entryStateObserver);
            Assert.IsNull(_cache.Reuse(KEY));
            Assert.AreEqual(0, _onExclusivityChangedCallCount);
        }

        /// <summary>
        /// Tests out the OnExclusivityChanged method
        /// </summary>
        [TestMethod]
        public void TestCanReuseExclusive()
        {
            CloseableReference<int> cachedRef = _cache.Cache(KEY, NewReference(100), _entryStateObserver);
            cachedRef.Dispose();
            Assert.IsTrue(_isExclusive ?? false);
            cachedRef = _cache.Reuse(KEY);
            Assert.IsNotNull(cachedRef);
            Assert.IsFalse(_isExclusive ?? true);
            cachedRef.Dispose();
            Assert.IsFalse(_isExclusive ?? true);
        }

        /// <summary>
        /// Tests out the InUseCount
        /// </summary>
        [TestMethod]
        public void TestInUseCount()
        {
            CloseableReference<int> cachedRef1 = _cache.Cache(KEY, NewReference(100));

            CloseableReference<int> cachedRef2a = _cache.Get(KEY);
            CloseableReference<int> cachedRef2b = cachedRef2a.Clone();
            AssertTotalSize(1, 100);
            AssertExclusivelyOwnedSize(0, 0);
            AssertSharedWithCount(KEY, 100, 2);

            CloseableReference<int> cachedRef3a = _cache.Get(KEY);
            CloseableReference<int> cachedRef3b = cachedRef3a.Clone();
            CloseableReference<int> cachedRef3c = cachedRef3b.Clone();
            AssertTotalSize(1, 100);
            AssertExclusivelyOwnedSize(0, 0);
            AssertSharedWithCount(KEY, 100, 3);

            cachedRef1.Dispose();
            AssertTotalSize(1, 100);
            AssertExclusivelyOwnedSize(0, 0);
            AssertSharedWithCount(KEY, 100, 2);

            // All copies of cachedRef2a need to be closed for usage count to drop
            cachedRef2a.Dispose();
            AssertTotalSize(1, 100);
            AssertExclusivelyOwnedSize(0, 0);
            AssertSharedWithCount(KEY, 100, 2);
            cachedRef2b.Dispose();
            AssertTotalSize(1, 100);
            AssertExclusivelyOwnedSize(0, 0);
            AssertSharedWithCount(KEY, 100, 1);

            // All copies of cachedRef3a need to be closed for usage count to drop
            cachedRef3c.Dispose();
            AssertTotalSize(1, 100);
            AssertExclusivelyOwnedSize(0, 0);
            AssertSharedWithCount(KEY, 100, 1);
            cachedRef3b.Dispose();
            AssertTotalSize(1, 100);
            AssertExclusivelyOwnedSize(0, 0);
            AssertSharedWithCount(KEY, 100, 1);
            cachedRef3a.Dispose();
            AssertTotalSize(1, 100);
            AssertExclusivelyOwnedSize(1, 100);
            AssertExclusivelyOwned(KEY, 100);
        }
        
        /// <summary>
        /// Tests out the Cache method with the same key
        /// </summary>
        [TestMethod]
        public void TestCachingSameKeyTwice()
        {
            CloseableReference<int> originalRef1 = NewReference(110);
            CloseableReference<int> cachedRef1 = _cache.Cache(KEY, originalRef1);
            CloseableReference<int> cachedRef2a = _cache.Get(KEY);
            CloseableReference<int> cachedRef2b = cachedRef2a.Clone();
            CloseableReference<int> cachedRef3 = _cache.Get(KEY);
            CountingMemoryCache<string, int>.Entry entry1 = _cache._cachedEntries.Get(KEY);

            CloseableReference<int> cachedRef2 = _cache.Cache(KEY, NewReference(120));
            CountingMemoryCache<string, int>.Entry entry2 = _cache._cachedEntries.Get(KEY);
            Assert.AreNotSame(entry1, entry2);
            AssertOrphanWithCount(entry1, 3);
            AssertSharedWithCount(KEY, 120, 1);

            // Release the orphaned reference only when all clients are gone
            originalRef1.Dispose();
            cachedRef2b.Dispose();
            AssertOrphanWithCount(entry1, 3);
            cachedRef2a.Dispose();
            AssertOrphanWithCount(entry1, 2);
            cachedRef1.Dispose();
            AssertOrphanWithCount(entry1, 1);
            Assert.AreEqual(0, _releaseCallCount);
            cachedRef3.Dispose();
            AssertOrphanWithCount(entry1, 0);
            Assert.AreEqual(1, _releaseCallCount);
        }

        /// <summary>
        /// Tests out the Cache method with the exceeding value
        /// </summary>
        [TestMethod]
        public void TestDoesNotCacheBigValues()
        {
            Assert.IsNull(_cache.Cache(KEY, NewReference(CACHE_ENTRY_MAX_SIZE + 1)));
        }

        /// <summary>
        /// Tests out the Cache method with the max value
        /// </summary>
        [TestMethod]
        public void TestDoesCacheNotTooBigValues()
        {
            Assert.IsNotNull(_cache.Cache(KEY, NewReference(CACHE_ENTRY_MAX_SIZE)));
        }

        /// <summary>
        /// Tests out the cache eviction
        /// </summary>
        [TestMethod]
        public void TestEviction_ByTotalSize()
        {
            // Value 4 cannot fit the cache
            CloseableReference<int> originalRef1 = NewReference(400);
            CloseableReference<int> valueRef1 = _cache.Cache(KEYS[1], originalRef1);
            originalRef1.Dispose();
            CloseableReference<int> originalRef2 = NewReference(500);
            CloseableReference<int> valueRef2 = _cache.Cache(KEYS[2], originalRef2);
            originalRef2.Dispose();
            CloseableReference<int> originalRef3 = NewReference(100);
            CloseableReference<int> valueRef3 = _cache.Cache(KEYS[3], originalRef3);
            originalRef3.Dispose();
            CloseableReference<int> originalRef4 = NewReference(700);
            CloseableReference<int> valueRef4 = _cache.Cache(KEYS[4], originalRef4);
            originalRef4.Dispose();
            AssertTotalSize(3, 1000);
            AssertExclusivelyOwnedSize(0, 0);
            AssertSharedWithCount(KEYS[1], 400, 1);
            AssertSharedWithCount(KEYS[2], 500, 1);
            AssertSharedWithCount(KEYS[3], 100, 1);
            AssertNotCached(KEYS[4], 700);
            Assert.IsNull(valueRef4);

            // Closing the clients of cached items will make them viable for eviction
            valueRef1.Dispose();
            valueRef2.Dispose();
            valueRef3.Dispose();
            AssertTotalSize(3, 1000);
            AssertExclusivelyOwnedSize(3, 1000);

            // Value 4 can now fit after evicting value1 and value2
            valueRef4 = _cache.Cache(KEYS[4], NewReference(700));
            AssertTotalSize(2, 800);
            AssertExclusivelyOwnedSize(1, 100);
            AssertNotCached(KEYS[1], 400);
            AssertNotCached(KEYS[2], 500);
            AssertExclusivelyOwned(KEYS[3], 100);
            AssertSharedWithCount(KEYS[4], 700, 1);
            Assert.IsTrue(_releaseValues.Contains(400));
            Assert.IsTrue(_releaseValues.Contains(500));
        }

        /// <summary>
        /// Tests out the cache eviction
        /// </summary>
        [TestMethod]
        public void TestEviction_ByTotalCount()
        {
            // value 5 cannot fit the cache
            CloseableReference<int> originalRef1 = NewReference(110);
            CloseableReference<int> valueRef1 = _cache.Cache(KEYS[1], originalRef1);
            originalRef1.Dispose();
            CloseableReference<int> originalRef2 = NewReference(120);
            CloseableReference<int> valueRef2 = _cache.Cache(KEYS[2], originalRef2);
            originalRef2.Dispose();
            CloseableReference<int> originalRef3 = NewReference(130);
            CloseableReference<int> valueRef3 = _cache.Cache(KEYS[3], originalRef3);
            originalRef3.Dispose();
            CloseableReference<int> originalRef4 = NewReference(140);
            CloseableReference<int> valueRef4 = _cache.Cache(KEYS[4], originalRef4);
            originalRef4.Dispose();
            CloseableReference<int> originalRef5 = NewReference(150);
            CloseableReference<int> valueRef5 = _cache.Cache(KEYS[5], originalRef5);
            originalRef5.Dispose();
            AssertTotalSize(4, 500);
            AssertExclusivelyOwnedSize(0, 0);
            AssertSharedWithCount(KEYS[1], 110, 1);
            AssertSharedWithCount(KEYS[2], 120, 1);
            AssertSharedWithCount(KEYS[3], 130, 1);
            AssertSharedWithCount(KEYS[4], 140, 1);
            AssertNotCached(KEYS[5], 150);
            Assert.IsNull(valueRef5);

            // Closing the clients of cached items will make them viable for eviction
            valueRef1.Dispose();
            valueRef2.Dispose();
            valueRef3.Dispose();
            AssertTotalSize(4, 500);
            AssertExclusivelyOwnedSize(3, 360);

            // Value 4 can now fit after evicting value1
            valueRef4 = _cache.Cache(KEYS[5], NewReference(150));
            AssertTotalSize(4, 540);
            AssertExclusivelyOwnedSize(2, 250);
            AssertNotCached(KEYS[1], 110);
            AssertExclusivelyOwned(KEYS[2], 120);
            AssertExclusivelyOwned(KEYS[3], 130);
            AssertSharedWithCount(KEYS[4], 140, 1);
            AssertSharedWithCount(KEYS[5], 150, 1);
            Assert.IsTrue(_releaseValues.Contains(110));
        }

        /// <summary>
        /// Tests out the cache eviction
        /// </summary>
        [TestMethod]
        public void TestEviction_ByEvictionQueueSize()
        {
            CloseableReference<int> originalRef1 = NewReference(200);
            CloseableReference<int> valueRef1 = _cache.Cache(KEYS[1], originalRef1);
            originalRef1.Dispose();
            valueRef1.Dispose();
            CloseableReference<int> originalRef2 = NewReference(300);
            CloseableReference<int> valueRef2 = _cache.Cache(KEYS[2], originalRef2);
            originalRef2.Dispose();
            valueRef2.Dispose();
            CloseableReference<int> originalRef3 = NewReference(700);
            CloseableReference<int> valueRef3 = _cache.Cache(KEYS[3], originalRef3);
            originalRef3.Dispose();
            AssertTotalSize(3, 1200);
            AssertExclusivelyOwnedSize(2, 500);
            AssertExclusivelyOwned(KEYS[1], 200);
            AssertExclusivelyOwned(KEYS[2], 300);
            AssertSharedWithCount(KEYS[3], 700, 1);
            Assert.AreEqual(0, _releaseCallCount);

            // Closing the client reference for item3 will cause item1 to be evicted
            valueRef3.Dispose();
            AssertTotalSize(2, 1000);
            AssertExclusivelyOwnedSize(2, 1000);
            AssertNotCached(KEYS[1], 200);
            AssertExclusivelyOwned(KEYS[2], 300);
            AssertExclusivelyOwned(KEYS[3], 700);
            Assert.IsTrue(_releaseValues.Contains(200));
        }

        /// <summary>
        /// Tests out the cache eviction
        /// </summary>
        [TestMethod]
        public void TestEviction_ByEvictionQueueCount()
        {
            CloseableReference<int> originalRef1 = NewReference(110);
            CloseableReference<int> valueRef1 = _cache.Cache(KEYS[1], originalRef1);
            originalRef1.Dispose();
            valueRef1.Dispose();
            CloseableReference<int> originalRef2 = NewReference(120);
            CloseableReference<int> valueRef2 = _cache.Cache(KEYS[2], originalRef2);
            originalRef2.Dispose();
            valueRef2.Dispose();
            CloseableReference<int> originalRef3 = NewReference(130);
            CloseableReference<int> valueRef3 = _cache.Cache(KEYS[3], originalRef3);
            originalRef3.Dispose();
            valueRef3.Dispose();
            CloseableReference<int> originalRef4 = NewReference(140);
            CloseableReference<int> valueRef4 = _cache.Cache(KEYS[4], originalRef4);
            originalRef4.Dispose();
            AssertTotalSize(4, 500);
            AssertExclusivelyOwnedSize(3, 360);
            AssertExclusivelyOwned(KEYS[1], 110);
            AssertExclusivelyOwned(KEYS[2], 120);
            AssertExclusivelyOwned(KEYS[3], 130);
            AssertSharedWithCount(KEYS[4], 140, 1);
            Assert.AreEqual(0, _releaseCallCount);

            // Closing the client reference for item4 will cause item1 to be evicted
            valueRef4.Dispose();
            AssertTotalSize(3, 390);
            AssertExclusivelyOwnedSize(3, 390);
            AssertNotCached(KEYS[1], 110);
            AssertExclusivelyOwned(KEYS[2], 120);
            AssertExclusivelyOwned(KEYS[3], 130);
            AssertExclusivelyOwned(KEYS[4], 140);
            Assert.IsTrue(_releaseValues.Contains(110));
        }

        /// <summary>
        /// Tests out the cache params update
        /// </summary>
        [TestMethod]
        public void TestUpdatesCacheParams()
        {
            CloseableReference<int> originalRef = NewReference(700);
            CloseableReference<int> cachedRef = _cache.Cache(KEYS[2], originalRef);
            originalRef.Dispose();
            cachedRef.Dispose();

            _cache.Get(KEY);
            Assert.AreEqual(1, _paramsSupplier.GetCallCount);

            _cache.Get(KEY);
            Assert.AreEqual(1, _paramsSupplier.GetCallCount);
            _cache.Get(KEY);
            Assert.AreEqual(1, _paramsSupplier.GetCallCount);

            AssertTotalSize(1, 700);
            AssertExclusivelyOwnedSize(1, 700);

            _params = new MemoryCacheParams(
                500 /* cache max size */,
                CACHE_MAX_COUNT,
                CACHE_EVICTION_QUEUE_MAX_SIZE,
                CACHE_EVICTION_QUEUE_MAX_COUNT,
                CACHE_ENTRY_MAX_SIZE);
            _paramsSupplier = new MockSupplier<MemoryCacheParams>(_params);
            _cache.ForceUpdateCacheParams(_paramsSupplier);

            _cache.Get(KEY);
            Assert.AreEqual(1, _paramsSupplier.GetCallCount);

            AssertTotalSize(0, 0);
            AssertExclusivelyOwnedSize(0, 0);
            Assert.IsTrue(_releaseValues.Contains(700));
        }

        /// <summary>
        /// Tests out the RemoveAll method
        /// </summary>
        [TestMethod]
        public void TestRemoveAllMatchingPredicate()
        {
            CloseableReference<int> originalRef1 = NewReference(110);
            CloseableReference<int> valueRef1 = _cache.Cache(KEYS[1], originalRef1);
            originalRef1.Dispose();
            valueRef1.Dispose();
            CloseableReference<int> originalRef2 = NewReference(120);
            CloseableReference<int> valueRef2 = _cache.Cache(KEYS[2], originalRef2);
            originalRef2.Dispose();
            valueRef2.Dispose();
            CloseableReference<int> originalRef3 = NewReference(130);
            CloseableReference<int> valueRef3 = _cache.Cache(KEYS[3], originalRef3);
            originalRef3.Dispose();
            CountingMemoryCache<string, int>.Entry entry3 = _cache._cachedEntries.Get(KEYS[3]);
            CloseableReference<int> originalRef4 = NewReference(150);
            CloseableReference<int> valueRef4 = _cache.Cache(KEYS[4], originalRef4);
            originalRef4.Dispose();

            int numEvictedEntries = _cache.RemoveAll(
                new Predicate<string>(key => key.Equals(KEYS[2]) || key.Equals(KEYS[3])));

            Assert.AreEqual(2, numEvictedEntries);

            AssertTotalSize(2, 260);
            AssertExclusivelyOwnedSize(1, 110);
            AssertExclusivelyOwned(KEYS[1], 110);
            AssertNotCached(KEYS[2], 120);
            AssertOrphanWithCount(entry3, 1);
            AssertSharedWithCount(KEYS[4], 150, 1);

            Assert.IsTrue(_releaseValues.Contains(120));
            Assert.IsFalse(_releaseValues.Contains(130));

            valueRef3.Dispose();
            Assert.IsTrue(_releaseValues.Contains(130));
        }

        /// <summary>
        /// Tests out the Clear method
        /// </summary>
        [TestMethod]
        public void TestClear()
        {
            CloseableReference<int> originalRef1 = NewReference(110);
            CloseableReference<int> cachedRef1 = _cache.Cache(KEYS[1], originalRef1);
            originalRef1.Dispose();
            CountingMemoryCache<string, int>.Entry entry1 = _cache._cachedEntries.Get(KEYS[1]);
            CloseableReference<int> originalRef2 = NewReference(120);
            CloseableReference<int> cachedRef2 = _cache.Cache(KEYS[2], originalRef2);
            originalRef2.Dispose();
            cachedRef2.Dispose();

            _cache.Clear();
            AssertTotalSize(0, 0);
            AssertExclusivelyOwnedSize(0, 0);
            AssertOrphanWithCount(entry1, 1);
            AssertNotCached(KEYS[2], 120);
            Assert.IsTrue(_releaseValues.Contains(120));

            cachedRef1.Dispose();
            Assert.IsTrue(_releaseValues.Contains(110));
        }

        /// <summary>
        /// Tests out the Trim method
        /// </summary>
        [TestMethod]
        public void TestTrimming()
        {
            double memoryTrimType = MemoryTrimType.OnCloseToDalvikHeapLimit;
            _params = new MemoryCacheParams(1100, 10, 1100, 10, 110);
            _paramsSupplier = new MockSupplier<MemoryCacheParams>(_params);
            _cache.ForceUpdateCacheParams(_paramsSupplier);

            // Create original references
            CloseableReference<int>[] originalRefs = new CloseableReference<int>[10];
            for (int i = 0; i < 10; i++)
            {
                originalRefs[i] = NewReference(100 + i);
            }

            // Cache items & close the original references
            CloseableReference<int>[] cachedRefs = new CloseableReference<int>[10];
            for (int i = 0; i < 10; i++)
            {
                cachedRefs[i] = _cache.Cache(KEYS[i], originalRefs[i]);
                originalRefs[i].Dispose();
            }

            // Cache should keep alive the items until evicted
            Assert.AreEqual(0, _releaseCallCount);

            // Trimming cannot evict shared entries
            _trimRatio = 1.0;
            _cache.Trim(memoryTrimType);
            AssertSharedWithCount(KEYS[0], 100, 1);
            AssertSharedWithCount(KEYS[1], 101, 1);
            AssertSharedWithCount(KEYS[2], 102, 1);
            AssertSharedWithCount(KEYS[3], 103, 1);
            AssertSharedWithCount(KEYS[4], 104, 1);
            AssertSharedWithCount(KEYS[5], 105, 1);
            AssertSharedWithCount(KEYS[6], 106, 1);
            AssertSharedWithCount(KEYS[7], 107, 1);
            AssertSharedWithCount(KEYS[8], 108, 1);
            AssertSharedWithCount(KEYS[9], 109, 1);
            AssertTotalSize(10, 1045);
            AssertExclusivelyOwnedSize(0, 0);

            // Close 7 client references
            cachedRefs[8].Dispose();
            cachedRefs[2].Dispose();
            cachedRefs[7].Dispose();
            cachedRefs[3].Dispose();
            cachedRefs[6].Dispose();
            cachedRefs[4].Dispose();
            cachedRefs[5].Dispose();
            AssertSharedWithCount(KEYS[0], 100, 1);
            AssertSharedWithCount(KEYS[1], 101, 1);
            AssertSharedWithCount(KEYS[9], 109, 1);
            AssertExclusivelyOwned(KEYS[8], 108);
            AssertExclusivelyOwned(KEYS[2], 102);
            AssertExclusivelyOwned(KEYS[7], 107);
            AssertExclusivelyOwned(KEYS[3], 103);
            AssertExclusivelyOwned(KEYS[6], 106);
            AssertExclusivelyOwned(KEYS[4], 104);
            AssertExclusivelyOwned(KEYS[5], 105);
            AssertTotalSize(10, 1045);
            AssertExclusivelyOwnedSize(7, 735);

            // Trim cache by 45%. This means that out of total of 1045 bytes cached, 574 should remain.
            // 310 bytes is used by the clients, which leaves 264 for the exclusively owned items.
            // Only the two most recent exclusively owned items fit, and they occupy 209 bytes.
            _trimRatio = 0.45;
            _cache.Trim(memoryTrimType);
            AssertSharedWithCount(KEYS[0], 100, 1);
            AssertSharedWithCount(KEYS[1], 101, 1);
            AssertSharedWithCount(KEYS[9], 109, 1);
            AssertExclusivelyOwned(KEYS[4], 104);
            AssertExclusivelyOwned(KEYS[5], 105);
            AssertNotCached(KEYS[8], 108);
            AssertNotCached(KEYS[2], 102);
            AssertNotCached(KEYS[7], 107);
            AssertNotCached(KEYS[3], 103);
            AssertNotCached(KEYS[6], 106);
            AssertTotalSize(5, 519);
            AssertExclusivelyOwnedSize(2, 209);
            Assert.IsTrue(_releaseValues.Contains(108));
            Assert.IsTrue(_releaseValues.Contains(102));
            Assert.IsTrue(_releaseValues.Contains(107));
            Assert.IsTrue(_releaseValues.Contains(103));
            Assert.IsTrue(_releaseValues.Contains(106));

            // Full trim. All exclusively owned items should be evicted.
            _trimRatio = 1.0;
            _cache.Trim(memoryTrimType);
            AssertSharedWithCount(KEYS[0], 100, 1);
            AssertSharedWithCount(KEYS[1], 101, 1);
            AssertSharedWithCount(KEYS[9], 109, 1);
            AssertNotCached(KEYS[8], 108);
            AssertNotCached(KEYS[2], 102);
            AssertNotCached(KEYS[7], 107);
            AssertNotCached(KEYS[3], 103);
            AssertNotCached(KEYS[6], 106);
            AssertNotCached(KEYS[6], 104);
            AssertNotCached(KEYS[6], 105);
            AssertTotalSize(3, 310);
            AssertExclusivelyOwnedSize(0, 0);
            Assert.IsTrue(_releaseValues.Contains(104));
            Assert.IsTrue(_releaseValues.Contains(105));
        }

        private CloseableReference<int> NewReference(int size)
        {
            return CloseableReference<int>.of(size, _releaser);
        }

        private void AssertSharedWithCount(string key, int value, int count)
        {
            Assert.IsTrue(_cache._cachedEntries.Contains(key), "key not found in the cache");
            Assert.IsFalse(_cache._exclusiveEntries.Contains(key), "key found in the exclusives");
            CountingMemoryCache<string, int>.Entry entry = _cache._cachedEntries.Get(key);
            Assert.IsNotNull(entry, "entry not found in the cache");
            Assert.AreEqual(key, entry.Key, "key mismatch");
            Assert.AreEqual(value, entry.ValueRef.Get(), "value mismatch");
            Assert.AreEqual(count, entry.ClientCount, "client count mismatch");
            Assert.IsFalse(entry.Orphan, "entry is an orphan");
        }

        private void AssertExclusivelyOwned(string key, int value)
        {
            Assert.IsTrue(_cache._cachedEntries.Contains(key), "key not found in the cache");
            Assert.IsTrue(_cache._exclusiveEntries.Contains(key), "key not found in the exclusives");
            CountingMemoryCache<string, int>.Entry entry = _cache._cachedEntries.Get(key);
            Assert.IsNotNull(entry, "entry not found in the cache");
            Assert.AreEqual(key, entry.Key, "key mismatch");
            Assert.AreEqual(value, entry.ValueRef.Get(), "value mismatch");
            Assert.AreEqual(0, entry.ClientCount, "client count greater than zero");
            Assert.IsFalse(entry.Orphan, "entry is an orphan");
        }

        private void AssertNotCached(string key, int value)
        {
            Assert.IsFalse(_cache._cachedEntries.Contains(key), "key found in the cache");
            Assert.IsFalse(_cache._exclusiveEntries.Contains(key), "key found in the exclusives");
        }

        private void AssertOrphanWithCount(CountingMemoryCache<string, int>.Entry entry, int count)
        {
            Assert.AreNotSame(entry, _cache._cachedEntries.Get(entry.Key), "entry found in the exclusives");
            Assert.AreNotSame(entry, _cache._exclusiveEntries.Get(entry.Key), "entry found in the cache");
            Assert.IsTrue(entry.Orphan, "entry is not an orphan");
            Assert.AreEqual(count, entry.ClientCount, "client count mismatch");
        }

        private void AssertTotalSize(int count, int bytes)
        {
            Assert.AreEqual(count, _cache.Count, "total cache count mismatch");
            Assert.AreEqual(bytes, _cache.SizeInBytes, "total cache size mismatch");
        }

        private void AssertExclusivelyOwnedSize(int count, int bytes)
        {
            Assert.AreEqual(count, _cache.EvictionQueueCount, "total exclusives count mismatch");
            Assert.AreEqual(bytes, _cache.EvictionQueueSizeInBytes, "total exclusives size mismatch");
        }
    }
}
