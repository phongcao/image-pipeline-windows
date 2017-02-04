using FBCore.Common.References;
using ImageFormatUtils;
using ImagePipeline.AnimatedFactory;
using ImagePipeline.Common;
using ImagePipeline.Image;
using ImagePipeline.Platform;
using System;
using System.Threading.Tasks;
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
        public Task<CloseableImage> DecodeImageAsync(
            EncodedImage encodedImage,
            int length,
            IQualityInfo qualityInfo,
            ImageDecodeOptions options)
        {
            ImageFormat imageFormat = encodedImage.Format;
            if (imageFormat == ImageFormat.UNINITIALIZED || imageFormat == ImageFormat.UNKNOWN)
            {
                imageFormat = ImageFormatChecker.GetImageFormat_WrapIOException(
                    encodedImage.GetInputStream());

                encodedImage.Format = imageFormat;
            }

            switch (imageFormat)
            {
                case ImageFormat.UNKNOWN:
                    throw new ArgumentException("unknown image format");

                case ImageFormat.JPEG:
                    return DecodeJpegAsync(encodedImage, length, qualityInfo)
                        .ContinueWith(task => ((CloseableImage)task.Result));

                case ImageFormat.GIF:
                    return DecodeGifAsync(encodedImage, options);

                case ImageFormat.WEBP_ANIMATED:
                    return DecodeAnimatedWebpAsync(encodedImage, options);

                default:
                    return DecodeStaticImageAsync(encodedImage)
                        .ContinueWith(task => ((CloseableImage)task.Result));
            }
        }

        /// <summary>
        /// Decodes gif into CloseableImage.
        ///
        /// <param name="encodedImage">input image (encoded bytes plus meta data)</param>
        /// <param name="options">decode options</param>
        /// @return a CloseableImage
        /// </summary>
        public Task<CloseableImage> DecodeGifAsync(
            EncodedImage encodedImage,
            ImageDecodeOptions options)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <param name="encodedImage">input image (encoded bytes plus meta data)</param>
        /// @return a CloseableStaticBitmap
        /// </summary>
        public Task<CloseableStaticBitmap> DecodeStaticImageAsync(EncodedImage encodedImage)
        {
            return _platformDecoder
                .DecodeFromEncodedImageAsync(encodedImage, _bitmapConfig)
                .ContinueWith(task =>
                {
                    try
                    {
                        return new CloseableStaticBitmap(
                            task.Result,
                            ImmutableQualityInfo.FULL_QUALITY,
                            encodedImage.RotationAngle);
                    }
                    finally
                    {
                        task.Result.Dispose();
                    }
                });
        }

        /// <summary>
        /// Decodes a partial jpeg.
        ///
        /// <param name="encodedImage">input image (encoded bytes plus meta data)</param>
        /// <param name="length">amount of currently available data in bytes</param>
        /// <param name="qualityInfo">quality info for the image</param>
        /// @return a CloseableStaticBitmap
        /// </summary>
        public Task<CloseableStaticBitmap> DecodeJpegAsync(
            EncodedImage encodedImage,
            int length,
            IQualityInfo qualityInfo)
        {
            return _platformDecoder
                .DecodeJPEGFromEncodedImageAsync(encodedImage, _bitmapConfig, length)
                .ContinueWith(task =>
                {
                    try
                    {
                        return new CloseableStaticBitmap(
                            task.Result,
                            qualityInfo,
                            encodedImage.RotationAngle);
                    }
                    finally
                    {
                        task.Result.Dispose();
                    }
                });
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
        public Task<CloseableImage> DecodeAnimatedWebpAsync(
            EncodedImage encodedImage,
            ImageDecodeOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
