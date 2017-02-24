using Windows.Graphics.Imaging;

namespace ImagePipeline.Bitmaps
{
    /// <summary>
    /// Observer that notifies external creation of bitmap using
    /// <see cref="PlatformBitmapFactory.CreateBitmap(int, int)"/> or
    /// <see cref="PlatformBitmapFactory.CreateBitmap(int, int, BitmapPixelFormat)"/>.
    /// </summary>
    public interface IBitmapCreationObserver
    {
        /// <summary>
        /// Notifies external creation of bitmap.
        /// </summary>
        void OnBitmapCreated(SoftwareBitmap bitmap, object callerContext);
    }
}
