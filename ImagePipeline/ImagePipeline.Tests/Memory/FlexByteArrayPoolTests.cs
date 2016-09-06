using FBCore.Common.Memory;
using FBCore.Common.References;
using ImagePipeline.Memory;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.Collections.Generic;

namespace ImagePipeline.Tests.Memory
{
    /**
     * Tests for {@link FlexByteArrayPool}
     */
    [TestClass]
    public class FlexByteArrayPoolTests
    {
        private const int MIN_BUFFER_SIZE = 4;
        private const int MAX_BUFFER_SIZE = 16;
        private FlexByteArrayPool _pool;
        private SoftRefByteArrayPool _delegatePool;

        [TestInitialize]
        public void Initialize()
        {
            Dictionary<int, int> buckets = new Dictionary<int, int>();
            for (int i = MIN_BUFFER_SIZE; i <= MAX_BUFFER_SIZE; i *= 2)
            {
                buckets.Add(i, 3);
            }

            _pool = new FlexByteArrayPool(
                new MockMemoryTrimmableRegistry(),
                new PoolParams(
                    int.MaxValue,
                    int.MaxValue,
                    buckets,
                    MIN_BUFFER_SIZE,
                    MAX_BUFFER_SIZE,
                    1));

            _delegatePool = _pool._delegatePool;
        }

        [TestMethod]
        public void TestBasic()
        {
            Assert.AreEqual(MIN_BUFFER_SIZE, _pool.GetMinBufferSize());
            Assert.AreEqual(MAX_BUFFER_SIZE, _delegatePool._poolParams.MaxBucketSize);
            Assert.AreEqual(0, _delegatePool.FreeCounter.NumBytes);
        }

        [TestMethod]
        public void TestGet()
        {
            CloseableReference<byte[]> arrayRef = _pool.Get(1);
            Assert.AreEqual(0, _delegatePool.FreeCounter.NumBytes);
            Assert.AreEqual(MIN_BUFFER_SIZE, arrayRef.Get().Length);
        }

        [TestMethod]
        public void TestGetTooBigArray()
        {
            Assert.AreEqual(2 * MAX_BUFFER_SIZE, _pool.Get(2 * MAX_BUFFER_SIZE).Get().Length);
        }

        [TestMethod]
        public void TestRelease()
        {
            _pool.Get(MIN_BUFFER_SIZE).Dispose();
            Assert.AreEqual(MIN_BUFFER_SIZE, _delegatePool.FreeCounter.NumBytes);
        }

        [TestMethod]
        public void TestGet_Realloc()
        {
            CloseableReference<byte[]> arrayRef = _pool.Get(1);
            byte[] smallArray = arrayRef.Get();
            arrayRef.Dispose();

            arrayRef = _pool.Get(7);
            Assert.AreEqual(8, arrayRef.Get().Length);
            Assert.AreNotSame(smallArray, arrayRef.Get());
        }

        [TestMethod]
        public void TestTrim()
        {
            _pool.Get(7).Dispose();
            Assert.AreEqual(1, _delegatePool.GetBucket(8).GetFreeListSize());

            // Now trim, and verify again
            _delegatePool.Trim(MemoryTrimType.OnCloseToDalvikHeapLimit);
            Assert.AreEqual(0, _delegatePool.GetBucket(8).GetFreeListSize());
        }

        [TestMethod]
        public void TestTrimUnsuccessful()
        {
            CloseableReference<byte[]> arrayRef = _pool.Get(7);
            _delegatePool.Trim(MemoryTrimType.OnCloseToDalvikHeapLimit);
            Assert.IsNotNull(arrayRef.Get());
        }

        [TestMethod]
        public void TestGetBucketedSize()
        {
            Assert.AreEqual(MIN_BUFFER_SIZE, _delegatePool.GetBucketedSize(1));
            Assert.AreEqual(MIN_BUFFER_SIZE, _delegatePool.GetBucketedSize(2));
            Assert.AreEqual(MIN_BUFFER_SIZE, _delegatePool.GetBucketedSize(3));
            Assert.AreEqual(MIN_BUFFER_SIZE, _delegatePool.GetBucketedSize(4));
            Assert.AreEqual(8, _delegatePool.GetBucketedSize(5));
            Assert.AreEqual(8, _delegatePool.GetBucketedSize(6));
            Assert.AreEqual(8, _delegatePool.GetBucketedSize(7));
            Assert.AreEqual(8, _delegatePool.GetBucketedSize(8));
            Assert.AreEqual(16, _delegatePool.GetBucketedSize(9));
        }
    }
}
