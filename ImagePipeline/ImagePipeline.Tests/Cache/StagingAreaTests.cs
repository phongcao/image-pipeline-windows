using Cache.Common;
using FBCore.Common.References;
using ImagePipeline.Cache;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace ImagePipeline.Tests.Cache
{
    /// <summary>
    /// Tests for <see cref="StagingArea"/> class
    /// </summary>
    [TestClass]
    public class StagingAreaTests
    {
        private static readonly byte[] BYTES = new byte[] { 1, 123, 20, 3, 6, 23, 1 };

        private StagingArea _stagingArea;
        private CloseableReference<IPooledByteBuffer> _closeableReference;
        private CloseableReference<IPooledByteBuffer> _closeableReference2;
        private EncodedImage _encodedImage;
        private EncodedImage _secondEncodedImage;
        private ICacheKey _cacheKey;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _stagingArea = StagingArea.Instance;
            _closeableReference = CloseableReference<IPooledByteBuffer>.of(new TrivialPooledByteBuffer(BYTES));
            _closeableReference2 = CloseableReference<IPooledByteBuffer>.of(new TrivialPooledByteBuffer(BYTES));
            _encodedImage = new EncodedImage(_closeableReference);
            _secondEncodedImage = new EncodedImage(_closeableReference2);
            _cacheKey = new SimpleCacheKey("http://this.is/uri");
            _stagingArea.Put(_cacheKey, _encodedImage);
        }

        /// <summary>
        /// Tests out the Contain method
        /// </summary>
        [TestMethod]
        public void TestContains()
        {
            Assert.IsTrue(_stagingArea.ContainsKey(_cacheKey));
            Assert.IsFalse(_stagingArea.ContainsKey(new SimpleCacheKey("http://this.is.not.uri")));
        }

        /// <summary>
        /// Tests out the Contain method
        /// </summary>
        [TestMethod]
        public void TestDoesntContainInvalid()
        {
            _encodedImage.Dispose();
            Assert.IsTrue(_stagingArea.ContainsKey(_cacheKey));
            Assert.IsTrue(EncodedImage.IsValid(_stagingArea.Get(_cacheKey)));
        }

        /// <summary>
        /// Tests out the Get method
        /// </summary>
        [TestMethod]
        public void TestGetValue()
        {
            Assert.AreSame(
                _closeableReference.GetUnderlyingReferenceTestOnly(),
                _stagingArea.Get(_cacheKey).GetByteBufferRef().GetUnderlyingReferenceTestOnly());
        }

        /// <summary>
        /// Tests out the Get method
        /// </summary>
        [TestMethod]
        public void TestBumpsRefCountOnGet()
        {
            _stagingArea.Get(_cacheKey);
            Assert.AreEqual(4, _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
        }

        /// <summary>
        /// Tests out the Put method
        /// </summary>
        [TestMethod]
        public void TestAnotherPut()
        {
            _stagingArea.Put(_cacheKey, _secondEncodedImage);
            Assert.AreEqual(2, _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
            Assert.AreSame(
                _closeableReference2.GetUnderlyingReferenceTestOnly(),
                _stagingArea.Get(_cacheKey).GetByteBufferRef().GetUnderlyingReferenceTestOnly());
        }

        /// <summary>
        /// Tests out the Put method
        /// </summary>
        [TestMethod]
        public void TestSamePut()
        {
            Assert.AreEqual(3, _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
            _stagingArea.Put(_cacheKey, _encodedImage);
            Assert.AreEqual(3, _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
            Assert.AreSame(
                _closeableReference.GetUnderlyingReferenceTestOnly(),
                _stagingArea.Get(_cacheKey).GetByteBufferRef().GetUnderlyingReferenceTestOnly());
        }

        /// <summary>
        /// Tests out the Put method
        /// </summary>
        [TestMethod]
        public void TestRemove()
        {
            Assert.IsTrue(_stagingArea.Remove(_cacheKey, _encodedImage));
            Assert.AreEqual(2, _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
            Assert.IsFalse(_stagingArea.Remove(_cacheKey, _encodedImage));
        }

        /// <summary>
        /// Tests out the Put method
        /// </summary>
        [TestMethod]
        public void TestRemoveWithBadRef()
        {
            Assert.IsFalse(_stagingArea.Remove(_cacheKey, _secondEncodedImage));
            Assert.IsTrue(CloseableReference<IPooledByteBuffer>.IsValid(_closeableReference));
            Assert.IsTrue(CloseableReference<IPooledByteBuffer>.IsValid(_closeableReference2));
        }

        /// <summary>
        /// Tests out the Put method
        /// </summary>
        [TestMethod]
        public void TestRemoveWithoutValueCheck()
        {
            Assert.IsTrue(_stagingArea.Remove(_cacheKey));
            Assert.AreEqual(2, _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
            Assert.IsFalse(_stagingArea.Remove(_cacheKey));
        }

        /// <summary>
        /// Tests out the Put method
        /// </summary>
        [TestMethod]
        public void TestClearAll()
        {
            _stagingArea.Put(new SimpleCacheKey("second"), _secondEncodedImage);
            _stagingArea.ClearAll();
            Assert.AreEqual(2, _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
            Assert.AreEqual(2, _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
            Assert.IsFalse(_stagingArea.Remove(_cacheKey));
        }
    }
}
