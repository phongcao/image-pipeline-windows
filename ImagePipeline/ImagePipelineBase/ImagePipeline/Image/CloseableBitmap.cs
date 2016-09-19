using Windows.Graphics.Imaging;

namespace ImagePipeline.Image
{
    /// <summary>
    /// <see cref="CloseableImage"/> that wraps a bitmap.
    /// </summary>
    public abstract class CloseableBitmap : CloseableImage
    {
        /// <summary>
        /// Gets the underlying bitmap.
        /// Note: care must be taken because subclasses might be more sophisticated than that. For example,
        /// animated bitmap may have many frames and this method will only return the first one.
        /// @return the underlying bitmap
        /// </summary>
        public abstract SoftwareBitmap UnderlyingBitmap { get; }
    }
}
