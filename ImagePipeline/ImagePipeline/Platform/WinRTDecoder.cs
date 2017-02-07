using FBCore.Common.Internal;
using FBCore.Common.References;
using FBCore.Common.Streams;
using FBCore.Concurrency;
using ImagePipeline.Image;
using ImageUtils;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Platform
{
    /// <summary>
    /// Bitmap decoder for Windows Runtime.
    /// </summary>
    public class WinRTDecoder : IPlatformDecoder
    {
        private IExecutorService _executor;

        // TODO (5884402) - remove dependency on JfifUtil
        private static readonly byte[] EOI_TAIL = new byte[]
        {
            JfifUtil.MARKER_FIRST_BYTE,
            JfifUtil.MARKER_EOI
        };

        /// <summary>
        /// Instantiates the <see cref="WinRTDecoder"/>.
        /// </summary>
        /// <param name="maxNumThreads"></param>
        public WinRTDecoder(int maxNumThreads)
        {
            _executor = Executors.NewFixedThreadPool(maxNumThreads);
        }

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
        public Task<CloseableReference<SoftwareBitmap>> DecodeFromEncodedImageAsync(
            EncodedImage encodedImage, BitmapPixelFormat bitmapConfig)
        {
            Stream inputStream = encodedImage.GetInputStream();
            Preconditions.CheckNotNull(inputStream);
            return _executor.Execute(async () =>
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(
                    inputStream.AsRandomAccessStream())
                    .AsTask()
                    .ConfigureAwait(false);

                SoftwareBitmap bitmap = await decoder
                    .GetSoftwareBitmapAsync(bitmapConfig, BitmapAlphaMode.Premultiplied)
                    .AsTask()
                    .ConfigureAwait(false);

                return CloseableReference<SoftwareBitmap>.of(bitmap);
            })
            .Unwrap();
        }

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
        public Task<CloseableReference<SoftwareBitmap>> DecodeJPEGFromEncodedImageAsync(
            EncodedImage encodedImage, BitmapPixelFormat bitmapConfig, int length)
        {
            return _executor.Execute(async () =>
            {
                bool isJpegComplete = encodedImage.IsCompleteAt(length);
                Stream jpegDataStream = encodedImage.GetInputStream();

                // At this point the InputStream from the encoded image should not be null since in the
                // pipeline,this comes from a call stack where this was checked before. Also this method 
                // needs the Stream to decode the image so this can't be null.
                Preconditions.CheckNotNull(jpegDataStream);
                if (encodedImage.Size > length)
                {
                    jpegDataStream = new LimitedInputStream(jpegDataStream, length);
                }

                if (!isJpegComplete)
                {
                    jpegDataStream = new TailAppendingInputStream(jpegDataStream, EOI_TAIL);
                }

                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(
                    jpegDataStream.AsRandomAccessStream())
                    .AsTask()
                    .ConfigureAwait(false);

                SoftwareBitmap bitmap = await decoder
                    .GetSoftwareBitmapAsync(bitmapConfig, BitmapAlphaMode.Premultiplied)
                    .AsTask()
                    .ConfigureAwait(false);

                return CloseableReference<SoftwareBitmap>.of(bitmap);
            })
            .Unwrap();
        }
    }
}
