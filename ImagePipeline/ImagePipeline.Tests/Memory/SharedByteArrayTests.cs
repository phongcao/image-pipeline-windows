using FBCore.Common.Memory;
using FBCore.Common.References;
using ImagePipeline.Memory;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Diagnostics;

namespace ImagePipeline.Tests.Memory
{
    /**
     * Tests for {@link SharedByteArray}
     */
    [TestClass]
    public class SharedByteArrayTests
    {
        private SharedByteArray _array;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _array = new SharedByteArray(
                new MockMemoryTrimmableRegistry(),
                new PoolParams(
                    int.MaxValue,
                    int.MaxValue,
                    null,
                    4,
                    16,
                    1));
        }

        /// <summary>
        /// Tests basic operations
        /// </summary>
        [TestMethod]
        public void TestBasic()
        {
            Assert.AreEqual(4, _array._minByteArraySize);
            Assert.AreEqual(16, _array._maxByteArraySize);
            Assert.IsNull(_array._byteArraySoftRef.Get());
            Assert.AreEqual(1, _array._semaphore.CurrentCount);
        }

        /// <summary>
        /// Tests out the Get method
        /// </summary>
        [TestMethod]
        public void TestGet()
        {
            CloseableReference<byte[]> arrayRef = _array.Get(1);
            Assert.AreSame(_array._byteArraySoftRef.Get(), arrayRef.Get());
            Assert.AreEqual(4, arrayRef.Get().Length);
            Assert.AreEqual(0, _array._semaphore.CurrentCount);
        }

        /// <summary>
        /// Tests if the requested size is too big
        /// </summary>
        [TestMethod]
        public void TestGetTooBigArray()
        {
            try
            {
                _array.Get(32);
                Assert.Fail();
            }
            catch (ArgumentException e)
            {
                Debug.WriteLine($"{e.Message} is expected");
            }
        }

        /// <summary>
        /// Tests out the Dispose method
        /// </summary>
        [TestMethod]
        public void TestRelease()
        {
            _array.Get(4).Dispose();
            Assert.AreEqual(1, _array._semaphore.CurrentCount);
        }

        /// <summary>
        /// Tests the reallocation
        /// </summary>
        [TestMethod]
        public void TestGet_Realloc()
        {
            CloseableReference<byte[]> arrayRef = _array.Get(1);
            byte[] smallArray = arrayRef.Get();
            arrayRef.Dispose();

            arrayRef = _array.Get(7);
            Assert.AreEqual(8, arrayRef.Get().Length);
            Assert.AreSame(_array._byteArraySoftRef.Get(), arrayRef.Get());
            Assert.AreNotSame(smallArray, arrayRef.Get());
        }

        /// <summary>
        /// Tests out the Trim method
        /// </summary>
        [TestMethod]
        public void TestTrim()
        {
            _array.Get(7).Dispose();
            Assert.AreEqual(8, _array._byteArraySoftRef.Get().Length);

            // Now trim, and verify again
            _array.Trim(MemoryTrimType.OnCloseToDalvikHeapLimit);
            Assert.IsNull(_array._byteArraySoftRef.Get());
            Assert.AreEqual(1, _array._semaphore.CurrentCount);
        }

        /// <summary>
        /// Tests out the Trim method in case of failure
        /// </summary>
        [TestMethod]
        public void TestTrimUnsuccessful()
        {
            CloseableReference<byte[]> arrayRef = _array.Get(7);
            _array.Trim(MemoryTrimType.OnCloseToDalvikHeapLimit);
            Assert.AreSame(arrayRef.Get(), _array._byteArraySoftRef.Get());
            Assert.AreEqual(0, _array._semaphore.CurrentCount);
        }

        /// <summary>
        /// Tests out the GetBucketSize method
        /// </summary>
        [TestMethod]
        public void TestGetBucketedSize()
        {
            Assert.AreEqual(4, _array.GetBucketedSize(1));
            Assert.AreEqual(4, _array.GetBucketedSize(2));
            Assert.AreEqual(4, _array.GetBucketedSize(3));
            Assert.AreEqual(4, _array.GetBucketedSize(4));
            Assert.AreEqual(8, _array.GetBucketedSize(5));
            Assert.AreEqual(8, _array.GetBucketedSize(6));
            Assert.AreEqual(8, _array.GetBucketedSize(7));
            Assert.AreEqual(8, _array.GetBucketedSize(8));
            Assert.AreEqual(16, _array.GetBucketedSize(9));
        }
    }
}
