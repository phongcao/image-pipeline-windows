using System;

namespace ImageFormatUtils
{
    /// <summary>
    /// Image format enum.
    /// </summary>
    public enum ImageFormat
    {
        /// <summary>
        /// Uninitialized.
        /// </summary>
        UNINITIALIZED,

        /// <summary>
        /// WebP simple.
        /// </summary>
        WEBP_SIMPLE,

        /// <summary>
        /// WebP lossless.
        /// </summary>
        WEBP_LOSSLESS,

        /// <summary>
        /// WebP extended.
        /// </summary>
        WEBP_EXTENDED,

        /// <summary>
        /// WebP extended with alpha.
        /// </summary>
        WEBP_EXTENDED_WITH_ALPHA,

        /// <summary>
        /// WebP animated.
        /// </summary>
        WEBP_ANIMATED,

        /// <summary>
        /// jpeg.
        /// </summary>
        JPEG,

        /// <summary>
        /// png.
        /// </summary>
        PNG,

        /// <summary>
        /// gif.
        /// </summary>
        GIF,

        /// <summary>
        /// bmp.
        /// </summary>
        BMP,

        /// <summary>
        /// Unknown image.
        /// This is needed in case we fail to detect any type for particular image.
        /// </summary>
        UNKNOWN
    }

    /// <summary>
    /// Extensions methods.
    /// </summary>
    public static class ImageFormatHelper
    {
        /// <summary>
        /// Checks if the image format is WebP.
        /// </summary>
        public static bool IsWebpFormat(ImageFormat imageFormat)
        {
            return imageFormat == ImageFormat.WEBP_SIMPLE ||
                imageFormat == ImageFormat.WEBP_LOSSLESS ||
                imageFormat == ImageFormat.WEBP_EXTENDED ||
                imageFormat == ImageFormat.WEBP_EXTENDED_WITH_ALPHA ||
                imageFormat == ImageFormat.WEBP_ANIMATED;
        }

        /// <summary>
        /// Maps an image format to the file extension.
        /// </summary>
        /// <param name="imageFormat">Image format.</param>
        /// <returns>File extension for the image format.</returns>
        /// <exception cref="InvalidOperationException">
        /// Unknown image format.
        /// </exception>
        public static string GetFileExtension(ImageFormat imageFormat)
        {
            switch (imageFormat)
            {
                case ImageFormat.WEBP_SIMPLE:
                case ImageFormat.WEBP_LOSSLESS:
                case ImageFormat.WEBP_EXTENDED:
                case ImageFormat.WEBP_EXTENDED_WITH_ALPHA:
                case ImageFormat.WEBP_ANIMATED:
                    return "webp";
                case ImageFormat.JPEG:
                    return "jpeg";
                case ImageFormat.PNG:
                    return "png";
                case ImageFormat.GIF:
                    return "gif";          
                case ImageFormat.BMP:
                    return "bmp";
                default:
                    throw new InvalidOperationException("Unknown image format " + nameof(imageFormat));
            }
        }
    }
}
