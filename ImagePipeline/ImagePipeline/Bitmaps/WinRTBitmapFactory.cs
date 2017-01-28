using FBCore.Common.References;
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
        /// <param name="width">The width of the bitmap.</param>
        /// <param name="height">The height of the bitmap.</param>
        /// <param name="bitmapConfig">The <see cref="BitmapPixelFormat"/> used to create the 
        /// decoded Bitmap.</param>
        /// @return a reference to the bitmap
        /// @exception OutOfMemoryError if the Bitmap cannot be allocated
        /// </summary>
        public override CloseableReference<SoftwareBitmap> CreateBitmapInternal(
            int width,
            int height,
            BitmapPixelFormat bitmapConfig)
        {
            SoftwareBitmap bitmap = new SoftwareBitmap(bitmapConfig, width, height);
            return CloseableReference<SoftwareBitmap>.of(bitmap, SimpleBitmapReleaser.Instance);
        }
    }
}
