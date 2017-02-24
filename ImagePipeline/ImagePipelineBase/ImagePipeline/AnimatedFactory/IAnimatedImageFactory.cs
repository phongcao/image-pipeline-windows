using ImagePipeline.Common;
using ImagePipeline.Image;
using Windows.Graphics.Imaging;

namespace ImagePipeline.AnimatedFactory
{
    /// <summary>
    /// Decoder for animated images.
    /// </summary>
    public interface IAnimatedImageFactory
    {
        /// <summary>
        /// Decodes a GIF into a CloseableImage.
        /// </summary>
        /// <param name="encodedImage">
        /// Encoded image (native byte array holding the encoded bytes and
        /// meta data).
        /// </param>
        /// <param name="options">The options for the decode.</param>
        /// <param name="bitmapConfig">
        /// The bitmap config used to generate the output bitmaps.
        /// </param>
        /// <returns>
        /// A <see cref="CloseableImage"/> for the GIF image.
        /// </returns>
        CloseableImage DecodeGif(
            EncodedImage encodedImage,
            ImageDecodeOptions options,
            BitmapPixelFormat bitmapConfig);

        /// <summary>
        /// Decode a WebP into a CloseableImage.
        /// </summary>
        /// <param name="encodedImage">
        /// Encoded image (native byte array holding the encoded bytes and
        /// meta data).
        /// </param>
        /// <param name="options">The options for the decode.</param>
        /// <param name="bitmapConfig">
        /// The bitmap config used to generate the output bitmaps.
        /// </param>
        /// <returns>
        /// A <see cref="CloseableImage"/> for the WebP image.
        /// </returns>
        CloseableImage DecodeWebP(
            EncodedImage encodedImage,
            ImageDecodeOptions options,
            BitmapPixelFormat bitmapConfig);
    }
}
