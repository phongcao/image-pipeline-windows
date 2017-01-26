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
        /// <param name="encodedImage">encoded image (native byte array holding the encoded bytes and meta data)</param>
        /// <param name="options">the options for the decode</param>
        /// <param name="bitmapConfig">the Bitmap.Config used to generate the output bitmaps</param>
        /// @return a <see cref="CloseableImage"/> for the GIF image
        /// </summary>
        CloseableImage DecodeGif(
            EncodedImage encodedImage,
            ImageDecodeOptions options,
            BitmapPixelFormat bitmapConfig);

        /// <summary>
        /// Decode a WebP into a CloseableImage.
        /// <param name="encodedImage">encoded image (native byte array holding the encoded bytes and meta data)</param>
        /// <param name="options">the options for the decode</param>
        /// <param name="bitmapConfig">the Bitmap.Config used to generate the output bitmaps</param>
        /// @return a <see cref="CloseableImage"/> for the WebP image
        /// </summary>
        CloseableImage DecodeWebP(
            EncodedImage encodedImage,
            ImageDecodeOptions options,
            BitmapPixelFormat bitmapConfig);
    }
}
