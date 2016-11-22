using FBCore.Common.References;
using ImagePipeline.Bitmaps;
using ImagePipeline.Image;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using Windows.Graphics.Imaging;

namespace ImagePipelineBase.Tests.ImagePipeline.Image
{
    /// <summary>
    /// Basic tests for closeable static bitmap
    /// </summary>
    [TestClass]
    public class CloseableStaticBitmapTests
    {
        private SoftwareBitmap _bitmap;
        private CloseableStaticBitmap _closeableStaticBitmap;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _bitmap = new SoftwareBitmap(BitmapPixelFormat.Rgba8, 1, 1);
            IResourceReleaser<SoftwareBitmap> resourceReleaser = SimpleBitmapReleaser.Instance;
            _closeableStaticBitmap = new CloseableStaticBitmap(
                _bitmap, resourceReleaser, ImmutableQualityInfo.FULL_QUALITY, 0);
        }

        /// <summary>
        /// Tests out the Dispose method
        /// </summary>
        [TestMethod]
        public void TestClose()
        {
            _closeableStaticBitmap.Dispose();
            Assert.IsTrue(_closeableStaticBitmap.IsClosed);
            bool disposed = false;

            // Check if object has been disposed
            try
            {
                disposed = (_bitmap.PixelWidth == 0);
            }
            catch (ObjectDisposedException)
            {
                disposed = true;
            }

            Assert.IsTrue(disposed);
        }

        /// <summary>
        /// Tests out the ConvertToBitmapReference method
        /// </summary>
        [TestMethod]
        public void TestConvert()
        {
            CloseableReference<SoftwareBitmap> reference = 
                _closeableStaticBitmap.ConvertToBitmapReference();
            Assert.AreSame(reference.Get(), _bitmap);
            Assert.IsTrue(_closeableStaticBitmap.IsClosed);
        }

        /// <summary>
        /// Tests out the ConvertToBitmapReference method after dispose
        /// </summary>
        [TestMethod]
        public void TestCannotConvertIfClosed()
        {
            _closeableStaticBitmap.Dispose();
            try
            {
                _closeableStaticBitmap.ConvertToBitmapReference();
                Assert.Fail();
            }
            catch (ArgumentNullException)
            {
                // This is expected
            }
        }
    }
}
