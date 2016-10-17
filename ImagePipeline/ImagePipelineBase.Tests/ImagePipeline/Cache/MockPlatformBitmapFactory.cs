using FBCore.Common.References;
using ImagePipeline.Bitmaps;
using ImagePipeline.Memory;
using System.Threading;
using Windows.Graphics.Imaging;

namespace ImagePipelineBase.Tests.ImagePipeline.Cache
{
    /// <summary>
    /// Mock PlatformBitmapFactory
    /// </summary>
    class MockPlatformBitmapFactory : PlatformBitmapFactory
    {
        private static readonly IResourceReleaser<SoftwareBitmap> BITMAP_RESOURCE_RELEASER = 
            new ResourceReleaserHelper<SoftwareBitmap>(b => b.Dispose());
        private int _addBitmapReferenceCallCount = 0;
        private SoftwareBitmap _bitmap;

        /// <summary>
        /// Instantiates the <see cref="MockPlatformBitmapFactory"/>.
        /// </summary>
        public MockPlatformBitmapFactory()
        {
            _bitmapCreationObserver = null;
        }

        /// <summary>
        /// Creates a bitmap of the specified width and height. This is intended for ImagePipeline's
        /// internal use only.
        ///
        /// <param name="width">the width of the bitmap</param>
        /// <param name="height">the height of the bitmap</param>
        /// <param name="bitmapConfig">the Bitmap.Config used to create the Bitmap</param>
        /// @return a reference to the bitmap
        /// @throws TooManyBitmapsException if the pool is full
        /// @throws java.lang.OutOfMemoryError if the Bitmap cannot be allocated
        /// </summary>
        public override CloseableReference<SoftwareBitmap> CreateBitmapInternal(
            int width,
            int height,
            BitmapPixelFormat bitmapConfig)
        {
            _bitmap = new SoftwareBitmap(bitmapConfig, width, height);
            return CloseableReference<SoftwareBitmap>.of(_bitmap, BITMAP_RESOURCE_RELEASER);
        }

        /// <summary>
        /// Returns the bitmap creation observer
        /// </summary>
        public IBitmapCreationObserver BitmapCreationObserver
        {
            get
            {
                return _bitmapCreationObserver;
            }
        }

        /// <summary>
        /// Adds bitmap reference
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="callerContext"></param>
        public override void AddBitmapReference(
            SoftwareBitmap bitmap,
            object callerContext)
        {
            Interlocked.Increment(ref _addBitmapReferenceCallCount);
            base.AddBitmapReference(bitmap, callerContext);
        }

        /// <summary>
        /// Returns how many times the AddBitmapReference has been invoked
        /// </summary>
        public int AddBitmapReferenceCallCount
        {
            get
            {
                return Volatile.Read(ref _addBitmapReferenceCallCount);
            }
        }

        /// <summary>
        /// The bitmap reference
        /// </summary>
        public SoftwareBitmap Bitmap
        {
            get
            {
                return _bitmap;
            }
        }
    }
}
