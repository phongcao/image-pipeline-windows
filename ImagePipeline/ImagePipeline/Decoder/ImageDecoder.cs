using FBCore.Common.Internal;
using ImageFormatUtils;
using ImagePipeline.AnimatedFactory;
using ImagePipeline.Common;
using ImagePipeline.Image;
using ImagePipeline.Platform;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Decoder
{
    /// <summary>
    /// Decodes images.
    ///
    /// <para />ImageDecoder implements image type recognition and passes
    /// decode requests to specialized methods implemented by subclass.
    /// </summary>
    public class ImageDecoder
    {
        private readonly IAnimatedImageFactory _animatedImageFactory;
        private readonly BitmapPixelFormat _bitmapConfig;
        private readonly IPlatformDecoder _platformDecoder;

        /// <summary>
        /// Instantiates the <see cref="ImageDecoder"/>.
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
        /// </summary>
        /// <param name="encodedImage">
        /// Input image (encoded bytes plus meta data).
        /// </param>
        /// <param name="length">
        /// If image type supports decoding incomplete image then 
        /// determines where the image data should be cut for decoding.
        /// </param>
        /// <param name="qualityInfo">
        /// Quality information for the image.
        /// </param>
        /// <param name="options">
        /// Options that cange decode behavior.
        /// </param>
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
                        .ContinueWith(
                        task => ((CloseableImage)task.Result), 
                        TaskContinuationOptions.ExecuteSynchronously);

                case ImageFormat.GIF:
                    return DecodeGifAsync(encodedImage, options);

                case ImageFormat.WEBP_ANIMATED:
                    return DecodeAnimatedWebpAsync(encodedImage, options);

                default:
                    return DecodeStaticImageAsync(encodedImage)
                        .ContinueWith(
                        task => ((CloseableImage)task.Result),
                        TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        /// <summary>
        /// Decodes gif into CloseableImage.
        /// </summary>
        /// <param name="encodedImage">
        /// Input image (encoded bytes plus meta data).
        /// </param>
        /// <param name="options">Decode options.</param>
        /// <returns>A CloseableImage.</returns>
        public Task<CloseableImage> DecodeGifAsync(
            EncodedImage encodedImage,
            ImageDecodeOptions options)
        {
            Stream inputStream = encodedImage.GetInputStream();
            if (inputStream == null)
            {
                return Task.FromResult(default(CloseableImage));
            }

            try
            {
                // Phong Cao: always forceStaticImage
                return DecodeStaticImageAsync(encodedImage)
                    .ContinueWith(
                    task => ((CloseableImage)task.Result),
                    TaskContinuationOptions.ExecuteSynchronously);
            }
            finally
            {
                Closeables.CloseQuietly(inputStream);
            }
        }

        /// <summary>
        /// Decodes a static bitmap.
        /// </summary>
        /// <param name="encodedImage">
        /// Input image (encoded bytes plus meta data).
        /// </param>
        /// <returns>A CloseableStaticBitmap.</returns>
        public Task<CloseableStaticBitmap> DecodeStaticImageAsync(EncodedImage encodedImage)
        {
            return _platformDecoder
                .DecodeFromEncodedImageAsync(encodedImage, _bitmapConfig)
                .ContinueWith(
                task =>
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
                },
                TaskContinuationOptions.ExecuteSynchronously);
        }

        /// <summary>
        /// Decodes a partial jpeg.
        /// </summary>
        /// <param name="encodedImage">
        /// Input image (encoded bytes plus meta data).
        /// </param>
        /// <param name="length">
        /// Amount of currently available data in bytes.
        /// </param>
        /// <param name="qualityInfo">
        /// Quality info for the image.
        /// </param>
        /// <returns>A CloseableStaticBitmap.</returns>
        public Task<CloseableStaticBitmap> DecodeJpegAsync(
            EncodedImage encodedImage,
            int length,
            IQualityInfo qualityInfo)
        {
            return _platformDecoder
                .DecodeJPEGFromEncodedImageAsync(encodedImage, _bitmapConfig, length)
                .ContinueWith(
                task =>
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
                },
                TaskContinuationOptions.ExecuteSynchronously);
        }

        /// <summary>
        /// Decode a webp animated image into a CloseableImage.
        /// </summary>
        /// <param name="encodedImage">
        /// Input image (encoded bytes plus meta data).
        /// </param>
        /// <param name="options">Image decode options.</param>
        /// <returns>A <see cref="CloseableImage"/>.</returns>
        public Task<CloseableImage> DecodeAnimatedWebpAsync(
            EncodedImage encodedImage,
            ImageDecodeOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
