using ImagePipeline.Memory;
using ImagePipeline.Testing;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.Collections.Generic;

namespace ImagePipeline.Tests.Memory
{
    /// <summary>
    /// Basic tests for GenericByteArrayPool
    /// </summary>
    [TestClass]
    public class GenericByteArrayPoolTests
    {
        private GenericByteArrayPool _pool;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            Dictionary<int, int> bucketSizes = new Dictionary<int, int>();
            bucketSizes[32] = 2;
            bucketSizes[64] = 1;
            bucketSizes[128] = 1;
            _pool = new GenericByteArrayPool(
                new MockMemoryTrimmableRegistry(),
                new PoolParams(128, bucketSizes),
                new MockPoolStatsTracker());
        }

        /// <summary>
        /// Test out the Alloc method
        /// </summary>
        [TestMethod]
        public void TestAlloc()
        {
            Assert.AreEqual(1, _pool.Alloc(1).Length);
            Assert.AreEqual(33, _pool.Alloc(33).Length);
            Assert.AreEqual(32, _pool.Alloc(32).Length);
        }

        /// <summary>
        /// Test out the Free method
        /// </summary>
        [TestMethod]
        public void TestFree()
        {
        }

        /// <summary>
        /// Test out the GetBucketedSize method
        /// </summary>
        [TestMethod]
        public void TestGetBucketedSize()
        {
            Assert.AreEqual(32, _pool.GetBucketedSize(1));
            Assert.AreEqual(32, _pool.GetBucketedSize(32));
            Assert.AreEqual(64, _pool.GetBucketedSize(33));
            Assert.AreEqual(64, _pool.GetBucketedSize(64));
            Assert.AreEqual(128, _pool.GetBucketedSize(69));

            // Value larger than max bucket
            Assert.AreEqual(129, _pool.GetBucketedSize(129));

            int[] invalidSizes = new int[] { -1, 0 };
            foreach (int size in invalidSizes)
            {
                try
                {
                    _pool.GetBucketedSize(size);
                    Assert.Fail();
                }
                catch (InvalidSizeException)
                {
                    // This is expected
                }
            }
        }

        /// <summary>
        /// Test out the GetBucketedSizeForValue method
        /// </summary>
        [TestMethod]
        public void TestGetBucketedSizeForValue()
        {
            Assert.AreEqual(32, _pool.GetBucketedSizeForValue(new byte[32]));
            Assert.AreEqual(64, _pool.GetBucketedSizeForValue(new byte[64]));
            Assert.AreEqual(128, _pool.GetBucketedSizeForValue(new byte[128]));

            // Test with non-bucket values
            Assert.AreEqual(1, _pool.GetBucketedSizeForValue(new byte[1]));
            Assert.AreEqual(129, _pool.GetBucketedSizeForValue(new byte[129]));
            Assert.AreEqual(31, _pool.GetBucketedSizeForValue(new byte[31]));
        }

        /// <summary>
        /// Test out the GetSizeInBytes method
        /// </summary>
        [TestMethod]
        public void TestGetSizeInBytes()
        {
            Assert.AreEqual(1, _pool.GetSizeInBytes(1));
            Assert.AreEqual(32, _pool.GetSizeInBytes(32));
            Assert.AreEqual(33, _pool.GetSizeInBytes(33));
            Assert.AreEqual(64, _pool.GetSizeInBytes(64));
            Assert.AreEqual(69, _pool.GetSizeInBytes(69));
        }
    }
}
