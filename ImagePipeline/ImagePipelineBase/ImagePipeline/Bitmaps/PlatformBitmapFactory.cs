using FBCore.Common.References;
using System;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Bitmaps
{
    /// <summary>
    /// Bitmap factory optimized for the platform.
    /// </summary>
    public abstract class PlatformBitmapFactory
    {
        /// <summary>
        /// Creates a bitmap of the specified width and height.
        /// </summary>
        /// <param name="width">The width of the bitmap.</param>
        /// <param name="height">The height of the bitmap.</param>
        /// <param name="bitmapConfig">
        /// The bitmap config used to create the bitmap.
        /// </param>
        /// <returns>A reference to the bitmap.</returns>
        /// <exception cref="OutOfMemoryException">
        /// If the bitmap cannot be allocated.
        /// </exception>
        public CloseableReference<SoftwareBitmap> CreateBitmap(
            int width,
            int height,
            BitmapPixelFormat bitmapConfig)
        {
            return CreateBitmap(width, height, bitmapConfig, null);
        }

        /// <summary>
        /// Creates a bitmap of the specified width and height.
        /// The bitmap will be created with the default Bgra8 configuration.
        /// </summary>
        /// <param name="width">The width of the bitmap.</param>
        /// <param name="height">The height of the bitmap.</param>
        /// <returns>A reference to the bitmap.</returns>
        /// <exception cref="OutOfMemoryException">
        /// If the Bitmap cannot be allocated.
        /// </exception>
        public CloseableReference<SoftwareBitmap> CreateBitmap(int width, int height)
        {
            return CreateBitmap(width, height, BitmapPixelFormat.Bgra8);
        }

        /// <summary>
        /// Creates a bitmap of the specified width and height.
        /// </summary>
        /// <param name="width">The width of the bitmap.</param>
        /// <param name="height">The height of the bitmap.</param>
        /// <param name="bitmapConfig">
        /// The bitmap config used to create the bitmap.
        /// </param>
        /// <param name="callerContext">
        /// The Tag to track who create the bitmap.
        /// </param>
        /// <returns>A reference to the bitmap.</returns>
        /// <exception cref="OutOfMemoryException">
        /// If the bitmap cannot be allocated.
        /// </exception>
        public CloseableReference<SoftwareBitmap> CreateBitmap(
            int width,
            int height,
            BitmapPixelFormat bitmapConfig,
            object callerContext)
        {
            CloseableReference<SoftwareBitmap> reference = 
                CreateBitmapInternal(width, height, bitmapConfig);

            AddBitmapReference(reference.Get(), callerContext);
            return reference;
        }

        /// <summary>
        /// Creates a bitmap of the specified width and height.
        /// The bitmap will be created with the default Bgra8 configuration.
        /// </summary>
        /// <param name="width">The width of the bitmap.</param>
        /// <param name="height">The height of the bitmap.</param>
        /// <param name="callerContext">
        /// The Tag to track who create the bitmap.
        /// </param>
        /// <returns>A reference to the bitmap.</returns>
        /// <exception cref="OutOfMemoryException">
        /// If the bitmap cannot be allocated.
        /// </exception>
        public CloseableReference<SoftwareBitmap> CreateBitmap(
            int width,
            int height,
            object callerContext)
        {
            return CreateBitmap(width, height, BitmapPixelFormat.Bgra8, callerContext);
        }

        /// <summary>
        /// Creates a bitmap of the specified width and height.
        /// This is intended for ImagePipeline's internal use only.
        /// </summary>
        /// <param name="width">The width of the bitmap.</param>
        /// <param name="height">The height of the bitmap.</param>
        /// <param name="bitmapConfig">
        /// The bitmap config used to create the bitmap.
        /// </param>
        /// <returns>A reference to the bitmap.</returns>
        /// <exception cref="OutOfMemoryException">
        /// If the bitmap cannot be allocated.
        /// </exception>
        public abstract CloseableReference<SoftwareBitmap> CreateBitmapInternal(
            int width,
            int height,
            BitmapPixelFormat bitmapConfig);

        /// <summary>
        /// Bitmap creation observer.
        /// </summary>
        protected static IBitmapCreationObserver _bitmapCreationObserver;

        /// <summary>
        /// Sets the creation observer.
        /// </summary>
        public void SetCreationListener(IBitmapCreationObserver bitmapCreationObserver)
        {
            if (_bitmapCreationObserver == null)
            {
                _bitmapCreationObserver = bitmapCreationObserver;
            }
        }

        /// <summary>
        /// Adds bitmap reference.
        /// </summary>
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
