using ImagePipeline.Memory;
using ImagePipeline.Testing;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.Collections.Generic;

namespace ImagePipeline.Tests.Memory
{
    /// <summary>
    /// Tests for NativeMemoryChunkPool 
    /// </summary>
    [TestClass]
    public class NativeMemoryChunkPoolTests
    {
        private NativeMemoryChunkPool _pool;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            Dictionary<int, int> bucketSizes = new Dictionary<int, int>();
            bucketSizes.Add(32, 2);
            bucketSizes.Add(64, 1);
            bucketSizes.Add(128, 1);
            _pool = new FakeNativeMemoryChunkPool(new PoolParams(128, bucketSizes));
        }

        /// <summary>
        /// Tests out the Alloc method
        /// </summary>
        [TestMethod]
        public void TestAlloc()
        {
            NativeMemoryChunk c = _pool.Alloc(1);
            Assert.IsNotNull(c);
            Assert.AreEqual(1, c.Size);
            Assert.AreEqual(1, _pool.Alloc(1).Size);
            Assert.AreEqual(33, _pool.Alloc(33).Size);
            Assert.AreEqual(32, _pool.Alloc(32).Size);
        }

        /// <summary>
        /// Tests out the Free method
        /// </summary>
        [TestMethod]
        public void TestFree()
        {
            NativeMemoryChunk c = _pool.Alloc(1);
            Assert.IsFalse(c.Closed);
            _pool.Free(c);
            Assert.IsTrue(c.Closed);
            _pool.Free(c);
            Assert.IsTrue(c.Closed);
        }

        /// <summary>
        /// Tests out the GetBucketedSize method
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
        /// Tests out the GetBucketedSizeForValue method
        /// </summary>
        [TestMethod]
        public void TestGetBucketedSizeForValue()
        {
            Assert.AreEqual(32, _pool.GetBucketedSizeForValue(new FakeNativeMemoryChunk(32)));
            Assert.AreEqual(64, _pool.GetBucketedSizeForValue(new FakeNativeMemoryChunk(64)));
            Assert.AreEqual(128, _pool.GetBucketedSizeForValue(new FakeNativeMemoryChunk(128)));

            // Test with non-bucket values
            Assert.AreEqual(1, _pool.GetBucketedSizeForValue(new FakeNativeMemoryChunk(1)));
            Assert.AreEqual(129, _pool.GetBucketedSizeForValue(new FakeNativeMemoryChunk(129)));
            Assert.AreEqual(31, _pool.GetBucketedSizeForValue(new FakeNativeMemoryChunk(31)));
        }

        /// <summary>
        /// Tests out the GetSizeInBytes method
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

        /// <summary>
        /// Tests out the IsReusable method
        /// </summary>
        [TestMethod]
        public void TestIsReusable()
        {
            NativeMemoryChunk chunk = _pool.Get(1);
            Assert.IsTrue(_pool.IsReusable(chunk));
            chunk.Dispose();
            Assert.IsFalse(_pool.IsReusable(chunk));
        }
    }
}
