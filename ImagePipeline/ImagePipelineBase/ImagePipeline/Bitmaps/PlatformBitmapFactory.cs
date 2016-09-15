using FBCore.Common.References;
using Windows.Graphics.Imaging;

namespace ImagePipelineBase.ImagePipeline.Bitmaps
{
    /// <summary>
    /// Bitmap factory optimized for the platform.
    /// </summary>
    public abstract class PlatformBitmapFactory
    {
        /// <summary>
        /// Creates a bitmap of the specified width and height.
        ///
        /// <param name="width">the width of the bitmap</param>
        /// <param name="height">the height of the bitmap</param>
        /// <param name="bitmapConfig">the Bitmap.Config used to create the Bitmap</param>
        /// @return a reference to the bitmap
        /// @throws TooManyBitmapsException if the pool is full
        /// @throws java.lang.OutOfMemoryError if the Bitmap cannot be allocated
        /// </summary>
        public CloseableReference<SoftwareBitmap> CreateBitmap(
            int width,
            int height,
            BitmapPixelFormat bitmapConfig)
        {
            return CreateBitmap(width, height, bitmapConfig, null);
        }

        /// <summary>
        /// Creates a bitmap of the specified width and height.
        /// The bitmap will be created with the default ARGB_8888 configuration
        ///
        /// <param name="width">the width of the bitmap</param>
        /// <param name="height">the height of the bitmap</param>
        /// @return a reference to the bitmap
        /// @throws TooManyBitmapsException if the pool is full
        /// @throws java.lang.OutOfMemoryError if the Bitmap cannot be allocated
        /// </summary>
        public CloseableReference<SoftwareBitmap> CreateBitmap(int width, int height)
        {
            return CreateBitmap(width, height, BitmapPixelFormat.Rgba8);
        }

        /// <summary>
        /// Creates a bitmap of the specified width and height.
        ///
        /// <param name="width">the width of the bitmap</param>
        /// <param name="height">the height of the bitmap</param>
        /// <param name="bitmapConfig">the Bitmap.Config used to create the Bitmap</param>
        /// <param name="callerContext">the Tag to track who create the Bitmap</param>
        /// @return a reference to the bitmap
        /// @throws TooManyBitmapsException if the pool is full
        /// @throws java.lang.OutOfMemoryError if the Bitmap cannot be allocated
        /// </summary>
        public CloseableReference<SoftwareBitmap> CreateBitmap(
            int width,
            int height,
            BitmapPixelFormat bitmapConfig,
            object callerContext)
        {
            CloseableReference<SoftwareBitmap> reference = CreateBitmapInternal(width, height, bitmapConfig);
            AddBitmapReference(reference.Get(), callerContext);
            return reference;
        }

        /// <summary>
        /// Creates a bitmap of the specified width and height.
        /// The bitmap will be created with the default ARGB_8888 configuration
        ///
        /// <param name="width">the width of the bitmap</param>
        /// <param name="height">the height of the bitmap</param>
        /// <param name="callerContext">the Tag to track who create the Bitmap</param>
        /// @return a reference to the bitmap
        /// @throws TooManyBitmapsException if the pool is full
        /// @throws java.lang.OutOfMemoryError if the Bitmap cannot be allocated
        /// </summary>
        public CloseableReference<SoftwareBitmap> CreateBitmap(
            int width,
            int height,
            object callerContext)
        {
            return CreateBitmap(width, height, BitmapPixelFormat.Rgba8, callerContext);
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
        public abstract CloseableReference<SoftwareBitmap> CreateBitmapInternal(
            int width,
            int height,
            BitmapPixelFormat bitmapConfig);

        /// <summary>
        /// Bitmap creation observer
        /// </summary>
        protected static IBitmapCreationObserver _bitmapCreationObserver;

        /// <summary>
        /// Sets the creation observer
        /// </summary>
        /// <param name="bitmapCreationObserver"></param>
        public void SetCreationListener(IBitmapCreationObserver bitmapCreationObserver)
        {
            if (_bitmapCreationObserver == null)
            {
                _bitmapCreationObserver = bitmapCreationObserver;
            }
        }

        /// <summary>
        /// Adds bitmap reference
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="callerContext"></param>
        public virtual void AddBitmapReference(
            SoftwareBitmap bitmap,
            object callerContext)
        {
            if (_bitmapCreationObserver != null)
            {
                _bitmapCreationObserver.OnBitmapCreated(bitmap, callerContext);
            }
        }
    }
}
