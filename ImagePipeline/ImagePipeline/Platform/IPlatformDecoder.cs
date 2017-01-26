using FBCore.Common.References;
using ImagePipeline.Image;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Platform
{
    /// <summary>
    /// Platform decoder interface
    /// </summary>
    public interface IPlatformDecoder
    {
        /// <summary>
        /// Creates a bitmap from encoded bytes. Supports JPEG but callers should use 
        /// DecodeJPEGFromEncodedImagefor partial JPEGs.
        ///
        /// <param name="encodedImage">the reference to the encoded image with the reference 
        /// to the encoded bytes</param>
        /// <param name="bitmapConfig">the <see cref="BitmapPixelFormat"/> used to create 
        /// the decoded</param> Bitmap
        /// @return the bitmap
        /// @throws TooManyBitmapsException if the pool is full
        /// @throws OutOfMemoryError if the Bitmap cannot be allocated
        /// </summary>
        CloseableReference<SoftwareBitmap> DecodeFromEncodedImage(
            EncodedImage encodedImage,
            BitmapPixelFormat bitmapConfig);

        /// <summary>
        /// Creates a bitmap from encoded JPEG bytes. Supports a partial JPEG image.
        ///
        /// <param name="encodedImage">the reference to the encoded image with the reference 
        /// to the encoded bytes</param>
        /// <param name="bitmapConfig">the <see cref="BitmapPixelFormat"/> used to create 
        /// the decoded</param> Bitmap
        /// <param name="length">the number of encoded bytes in the buffer</param>
        /// @return the bitmap
        /// @throws TooManyBitmapsException if the pool is full
        /// @throws OutOfMemoryError if the Bitmap cannot be allocated
        /// </summary>
        CloseableReference<SoftwareBitmap> DecodeJPEGFromEncodedImage(
            EncodedImage encodedImage,
            BitmapPixelFormat bitmapConfig,
            int length);
    }
}
