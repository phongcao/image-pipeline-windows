using FBCore.Common.References;
using FBCore.Concurrency;
using ImageFormatUtils;
using ImagePipeline.Common;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using ImagePipeline.Request;
using ImageUtils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// A producer that retrieves exif thumbnails.
    ///
    /// <para />At present, these thumbnails are retrieved on the managed
    /// memory before being put into native memory.
    /// </summary>
    public class LocalExifThumbnailProducer : IThumbnailProducer<EncodedImage>
    {
        private const int COMMON_EXIF_THUMBNAIL_MAX_DIMENSION = 512;

        internal const string PRODUCER_NAME = "LocalExifThumbnailProducer";
        internal const string CREATED_THUMBNAIL = "createdThumbnail";

        private readonly IExecutorService _executor;
        private readonly IPooledByteBufferFactory _pooledByteBufferFactory;

        /// <summary>
        /// Instantiates the <see cref="LocalExifThumbnailProducer"/>.
        /// </summary>
        public LocalExifThumbnailProducer(
            IExecutorService executor,
            IPooledByteBufferFactory pooledByteBufferFactory)
        {
            _executor = executor;
            _pooledByteBufferFactory = pooledByteBufferFactory;
        }

        /// <summary>
        /// Checks whether the producer may be able to produce images of
        /// the specified size. This makes no promise about being able to
        /// produce images for a particular source, only generally being
        /// able to produce output of the desired resolution.
        ///
        /// <para />In this case, assumptions are made about the common
        /// size of EXIF thumbnails which is that they may be up to 512
        /// pixels in each dimension.
        /// </summary>
        /// <param name="resizeOptions">
        /// The resize options from the current request.
        /// </param>
        /// <returns>
        /// true if the producer can meet these needs.
        /// </returns>
        public bool CanProvideImageForSize(ResizeOptions resizeOptions)
        {
            return ThumbnailSizeChecker.IsImageBigEnough(
                COMMON_EXIF_THUMBNAIL_MAX_DIMENSION,
                COMMON_EXIF_THUMBNAIL_MAX_DIMENSION,
                resizeOptions);
        }

        /// <summary>
        /// Start producing results for given context.
        /// Provided consumer is notified whenever progress is made
        /// (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(
          IConsumer<EncodedImage> consumer,
          IProducerContext producerContext)
        {
            IProducerListener listener = producerContext.Listener;
            string requestId = producerContext.Id;
            ImageRequest imageRequest = producerContext.ImageRequest;

            StatefulProducerRunnable<EncodedImage> cancellableProducerRunnable =
                new StatefulProducerRunnableImpl<EncodedImage>(
                    consumer,
                    listener,
                    PRODUCER_NAME,
                    requestId,
                    null,
                    null,
                    null,
                    (result) =>
                    {
                        IDictionary<string, string> extraMap = new Dictionary<string, string>()
                        {
                            {  CREATED_THUMBNAIL, (result != null).ToString() }
                        };

                        return new ReadOnlyDictionary<string, string>(extraMap);
                    },
                    null,
                    null,
                    (result) =>
                    {
                        EncodedImage.CloseSafely(result);
                    },
                    async () =>
                    {
                        Uri sourceUri = imageRequest.SourceUri;
                        StorageFile file = await StorageFile
                            .GetFileFromApplicationUriAsync(sourceUri)
                            .AsTask()
                            .ConfigureAwait(false);

                        using (var fileStream = await file.OpenReadAsync().AsTask().ConfigureAwait(false))
                        {
                            byte[] bytes = await BitmapUtil
                                .GetThumbnailAsync(fileStream)
                                .ConfigureAwait(false);

                            if (bytes != null)
                            {
                                IPooledByteBuffer pooledByteBuffer =
                                    _pooledByteBufferFactory.NewByteBuffer(bytes);

                                return await BuildEncodedImage(pooledByteBuffer, fileStream)
                                    .ConfigureAwait(false);
                            }
                            else
                            {
                                return null;
                            }
                        }
                    });

            producerContext.AddCallbacks(
                new BaseProducerContextCallbacks(
                    () =>
                    {
                        cancellableProducerRunnable.Cancel();
                    },
                    () => { },
                    () => { },
                    () => { }));

            _executor.Execute(cancellableProducerRunnable.Runnable);
        }

        private async Task<EncodedImage> BuildEncodedImage(
            IPooledByteBuffer imageBytes,
            IRandomAccessStream imageStream)
        {
            using (var stream = imageStream.AsStream())
            {
                Tuple<int, int> dimensions = await BitmapUtil
                    .DecodeDimensionsAsync(stream)
                    .ConfigureAwait(false);

                int rotationAngle = GetRotationAngle(stream);
                int width = dimensions != default(Tuple<int, int>) ? 
                    dimensions.Item1 : 
                    EncodedImage.UNKNOWN_WIDTH;

                int height = dimensions != default(Tuple<int, int>) ? 
                    dimensions.Item2 : 
                    EncodedImage.UNKNOWN_HEIGHT;

                EncodedImage encodedImage;
                CloseableReference<IPooledByteBuffer> closeableByteBuffer = 
                    CloseableReference<IPooledByteBuffer>.of(imageBytes);

                try
                {
                    encodedImage = new EncodedImage(closeableByteBuffer);
                }
                finally
                {
                    CloseableReference<IPooledByteBuffer>.CloseSafely(
                        closeableByteBuffer);
                }

                encodedImage.Format = ImageFormat.JPEG;
                encodedImage.RotationAngle = rotationAngle;
                encodedImage.Width = width;
                encodedImage.Height = height;

                return encodedImage;
            }
        }

        /// <summary>
        /// Gets the correction angle based on the image's orientation.
        /// </summary>
        /// <param name="stream">The image stream.</param>
        /// <returns>The rotation angle.</returns>
        private int GetRotationAngle(Stream stream)
        {
            return JfifUtil.GetAutoRotateAngleFromOrientation(
                JfifUtil.GetOrientation(stream));
        }
    }
}
