using Cache.Common;
using Cache.Disk;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.IO;

namespace ImagePipelineBase.Tests.Cache.Disk
{
    /// <summary>
    /// Tests for <see cref="SettableCacheEvent"/>
    /// </summary>
    [TestClass]
    public class SettableCacheEventTests
    {
        /// <summary>
        /// Tests out recyle and clear all
        /// </summary>
        [TestMethod]
        public void TestRecycleClearsAllFields()
        {
            SettableCacheEvent cacheEvent = SettableCacheEvent.Obtain();
            cacheEvent.SetCacheKey(default(ICacheKey));
            cacheEvent.SetCacheLimit(21);
            cacheEvent.SetCacheSize(12332445);
            cacheEvent.SetEvictionReason(EvictionReason.CACHE_MANAGER_TRIMMED);
            cacheEvent.SetException(new IOException());
            cacheEvent.SetItemSize(1435);
            cacheEvent.SetResourceId("sddqrtyjf");

            cacheEvent.Recycle();

            Assert.IsNull(cacheEvent.CacheKey);
            Assert.AreEqual(cacheEvent.CacheLimit, 0);
            Assert.AreEqual(cacheEvent.CacheSize, 0);
            Assert.AreEqual(cacheEvent.EvictionReason, EvictionReason.NONE);
            Assert.IsNull(cacheEvent.Exception);
            Assert.AreEqual(cacheEvent.ItemSize, 0);
            Assert.IsNull(cacheEvent.ResourceId);
        }

        /// <summary>
        /// Tests out recyle and no re-use
        /// </summary>
        [TestMethod]
        public void TestSecondObtainGetsNewEventIfNoRecycling()
        {
            SettableCacheEvent firstEvent = SettableCacheEvent.Obtain();
            SettableCacheEvent secondEvent = SettableCacheEvent.Obtain();

            Assert.AreNotEqual(secondEvent.GetHashCode(), firstEvent.GetHashCode());
        }

        /// <summary>
        /// Tests out recyle and re-use
        /// </summary>
        [TestMethod]
        public void TestSecondObtainAfterRecyclingGetsRecycledEvent()
        {
            SettableCacheEvent firstEvent = SettableCacheEvent.Obtain();
            firstEvent.Recycle();
            SettableCacheEvent secondEvent = SettableCacheEvent.Obtain();

            Assert.AreEqual(secondEvent.GetHashCode(), firstEvent.GetHashCode());
        }
    }
}
