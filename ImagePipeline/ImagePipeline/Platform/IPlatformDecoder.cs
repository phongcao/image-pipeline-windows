using FBCore.Common.References;
using ImagePipeline.Image;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Platform
{
    /// <summary>
    /// Platform decoder interface.
    /// </summary>
    public interface IPlatformDecoder
    {
        /// <summary>
        /// Creates a bitmap from encoded bytes. Supports JPEG but callers should use 
        /// DecodeJPEGFromEncodedImagefor partial JPEGs.
        ///
        /// <param name="encodedImage">The reference to the encoded image with the reference 
        /// to the encoded bytes.</param>
        /// <param name="bitmapConfig">The <see cref="BitmapPixelFormat"/> used to create 
        /// the decoded Bitmap.</param>
        /// @return the bitmap.
        /// @throws TooManyBitmapsException if the pool is full.
        /// @throws OutOfMemoryError if the Bitmap cannot be allocated.
        /// </summary>
        Task<CloseableReference<SoftwareBitmap>> DecodeFromEncodedImageAsync(
            EncodedImage encodedImage,
            BitmapPixelFormat bitmapConfig);

        /// <summary>
        /// Creates a bitmap from encoded JPEG bytes. Supports a partial JPEG image.
        ///
        /// <param name="encodedImage">The reference to the encoded image with the reference 
        /// to the encoded bytes.</param>
        /// <param name="bitmapConfig">The <see cref="BitmapPixelFormat"/> used to create 
        /// the decoded Bitmap.</param>
        /// <param name="length">The number of encoded bytes in the buffer.</param>
        /// @return the bitmap.
        /// @throws TooManyBitmapsException if the pool is full.
        /// @throws OutOfMemoryError if the Bitmap cannot be allocated.
        /// </summary>
        Task<CloseableReference<SoftwareBitmap>> DecodeJPEGFromEncodedImageAsync(
            EncodedImage encodedImage,
            BitmapPixelFormat bitmapConfig,
            int length);
    }
}
