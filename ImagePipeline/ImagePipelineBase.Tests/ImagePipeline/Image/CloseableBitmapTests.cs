using FBCore.Common.References;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using Windows.Graphics.Imaging;

namespace ImagePipelineBase.Tests.ImagePipeline.Image
{
    /// <summary>
    /// Basic tests for closeable bitmap
    /// </summary>
    [TestClass]
    public sealed class CloseableBitmapTests : IDisposable
    {
        private SoftwareBitmap _bitmap;
        private CloseableStaticBitmap _closeableStaticBitmap;
        private int _releaseCallCount;
        private IResourceReleaser<SoftwareBitmap> _resourceReleaser;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _bitmap = new SoftwareBitmap(BitmapPixelFormat.Rgba8, 50, 50);
            _releaseCallCount = 0;
            _resourceReleaser = new ResourceReleaserImpl<SoftwareBitmap>(
                b =>
                {
                    b.Dispose();
                    ++_releaseCallCount;
                });

            _closeableStaticBitmap = new CloseableStaticBitmap(
                _bitmap, _resourceReleaser, ImmutableQualityInfo.FULL_QUALITY, 0);
        }

        /// <summary>
        /// Test cleanup.
        /// </summary>
        public void Dispose()
        {
            _bitmap.Dispose();
            _closeableStaticBitmap.Dispose();
        }

        /// <summary>
        /// Tests out basic operations
        /// </summary>
        [TestMethod]
        public void TestBasic()
        {
            Assert.IsFalse(_closeableStaticBitmap.IsClosed);
            Assert.AreSame(_bitmap, _closeableStaticBitmap.UnderlyingBitmap);

            // Close it now
            _closeableStaticBitmap.Dispose();
            Assert.IsTrue(_closeableStaticBitmap.IsClosed);
            Assert.IsNull(_closeableStaticBitmap.UnderlyingBitmap);
            Assert.AreEqual(1, _releaseCallCount);

            // Close it again
            _closeableStaticBitmap.Dispose();
            Assert.IsTrue(_closeableStaticBitmap.IsClosed);
            Assert.IsNull(_closeableStaticBitmap.UnderlyingBitmap);
        }

        /// <summary>
        /// Tests out finalization
        /// </summary>
        [TestMethod]
        public void TestFinalize()
        {
            _closeableStaticBitmap.Dispose();
            Assert.IsTrue(_closeableStaticBitmap.IsClosed);
            Assert.IsNull(_closeableStaticBitmap.UnderlyingBitmap);
            Assert.AreEqual(1, _releaseCallCount);
        }
    }
}
