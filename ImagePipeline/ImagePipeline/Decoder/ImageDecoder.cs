using ImagePipeline.AnimatedFactory;
using ImagePipeline.Common;
using ImagePipeline.Image;
using ImagePipeline.Platform;
using System;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Decoder
{
    /// <summary>
    /// Decodes images.
    ///
    /// <para /> ImageDecoder implements image type recognition and passes decode requests to
    /// specialized methods implemented by subclass
    /// </summary>
    public class ImageDecoder
    {
        private readonly IAnimatedImageFactory _animatedImageFactory;
        private readonly BitmapPixelFormat _bitmapConfig;
        private readonly IPlatformDecoder _platformDecoder;

        /// <summary>
        /// Instantiates the <see cref="ImageDecoder"/>
        /// </summary>
        public ImageDecoder(
            IAnimatedImageFactory animatedImageFactory,
            IPlatformDecoder platformDecoder,
            BitmapPixelFormat bitmapConfig)
        {
            _animatedImageFactory = animatedImageFactory;
            _bitmapConfig = bitmapConfig;
            _platformDecoder = platformDecoder;
        }

        /// <summary>
        /// Decodes image.
        ///
        /// <param name="encodedImage">input image (encoded bytes plus meta data)</param>
        /// <param name="length">if image type supports decoding incomplete image then 
        /// determines where the image data should be cut for decoding.</param>
        /// <param name="qualityInfo">quality information for the image</param>
        /// <param name="options">options that cange decode behavior</param>
        /// </summary>
        public CloseableImage DecodeImage(
            EncodedImage encodedImage,
            int length,
            IQualityInfo qualityInfo,
            ImageDecodeOptions options)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Decodes gif into CloseableImage.
        ///
        /// <param name="encodedImage">input image (encoded bytes plus meta data)</param>
        /// <param name="options">decode options</param>
        /// @return a CloseableImage
        /// </summary>
        public CloseableImage DecodeGif(
            EncodedImage encodedImage,
            ImageDecodeOptions options)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <param name="encodedImage">input image (encoded bytes plus meta data)</param>
        /// @return a CloseableStaticBitmap
        /// </summary>
        public CloseableStaticBitmap DecodeStaticImage(EncodedImage encodedImage)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Decodes a partial jpeg.
        ///
        /// <param name="encodedImage">input image (encoded bytes plus meta data)</param>
        /// <param name="length">amount of currently available data in bytes</param>
        /// <param name="qualityInfo">quality info for the image</param>
        /// @return a CloseableStaticBitmap
        /// </summary>
        public CloseableStaticBitmap DecodeJpeg(
            EncodedImage encodedImage,
            int length,
            IQualityInfo qualityInfo)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Decode a webp animated image into a CloseableImage.
        ///
        /// <para /> The image is decoded into a 'pinned' purgeable bitmap.
        ///
        /// <param name="encodedImage">input image (encoded bytes plus meta data)</param>
        /// <param name="options"></param>
        /// @return a <see cref="CloseableImage"/>
        /// </summary>
        public CloseableImage DecodeAnimatedWebp(
            EncodedImage encodedImage,
            ImageDecodeOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
