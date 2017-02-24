using FBCore.Common.References;
using ImagePipeline.Image;
using System;
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
        /// Creates a bitmap from encoded bytes. Supports JPEG but callers
        /// should use DecodeJPEGFromEncodedImage for partial JPEGs.
        /// </summary>
        /// <param name="encodedImage">
        /// The reference to the encoded image with the reference to the
        /// encoded bytes.
        /// </param>
        /// <param name="bitmapConfig">
        /// The <see cref="BitmapPixelFormat"/> used to create the decoded
        /// SoftwareBitmap.
        /// </param>
        /// <returns>The bitmap.</returns>
        /// <exception cref="OutOfMemoryException">
        /// If the Bitmap cannot be allocated.
        /// </exception>
        Task<CloseableReference<SoftwareBitmap>> DecodeFromEncodedImageAsync(
            EncodedImage encodedImage,
            BitmapPixelFormat bitmapConfig);

        /// <summary>
        /// Creates a bitmap from encoded JPEG bytes.
        /// Supports a partial JPEG image.
        /// </summary>
        /// <param name="encodedImage">
        /// The reference to the encoded image with the reference to the
        /// encoded bytes.
        /// </param>
        /// <param name="bitmapConfig">
        /// The <see cref="BitmapPixelFormat"/> used to create the decoded
        /// SoftwareBitmap.
        /// </param>
        /// <param name="length">
        /// The number of encoded bytes in the buffer.
        /// </param>
        /// <returns>The bitmap.</returns>
        /// <exception cref="OutOfMemoryException">
        /// If the Bitmap cannot be allocated.
        /// </exception>
        Task<CloseableReference<SoftwareBitmap>> DecodeJPEGFromEncodedImageAsync(
            EncodedImage encodedImage,
            BitmapPixelFormat bitmapConfig,
            int length);
    }
}
