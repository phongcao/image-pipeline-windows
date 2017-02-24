using FBCore.Common.References;
using System;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Bitmaps
{
    /// <summary>
    /// Bitmap factory for Windows Runtime.
    /// </summary>
    public class WinRTBitmapFactory : PlatformBitmapFactory
    {
        /// <summary>
        /// Creates a bitmap of the specified width and height.
        /// </summary>
        /// <param name="width">The width of the bitmap.</param>
        /// <param name="height">The height of the bitmap.</param>
        /// <param name="bitmapConfig">
        /// The <see cref="BitmapPixelFormat"/> used to create the 
        /// decoded Bitmap.
        /// </param>
        /// <returns>A reference to the bitmap.</returns>
        /// <exception cref="OutOfMemoryException">
        /// if the Bitmap cannot be allocated.
        /// </exception>
        public override CloseableReference<SoftwareBitmap> CreateBitmapInternal(
            int width,
            int height,
            BitmapPixelFormat bitmapConfig)
        {
            SoftwareBitmap bitmap = new SoftwareBitmap(
                bitmapConfig, width, height, BitmapAlphaMode.Premultiplied);

            return CloseableReference<SoftwareBitmap>.of(bitmap, SimpleBitmapReleaser.Instance);
        }
    }
}
