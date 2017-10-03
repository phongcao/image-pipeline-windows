using FBCore.Common.Internal;
using FBCore.Common.References;
using FBCore.Common.Util;
using FBCore.Concurrency;
using ImageFormatUtils;
using ImagePipeline.Common;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using ImagePipeline.NativeCode;
using ImagePipeline.Request;
using ImageUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Resizes and rotates JPEG image according to the EXIF orientation data.
    ///
    /// <para />If the image is not JPEG, no transformation is applied.
    /// <para />Should not be used if downsampling is in use.
    /// </summary>
    public class ResizeAndRotateProducer : IProducer<EncodedImage>
    {
        private const string PRODUCER_NAME = "ResizeAndRotateProducer";
        private const string ORIGINAL_SIZE_KEY = "Original size";
        private const string REQUESTED_SIZE_KEY = "Requested size";
        private const string FRACTION_KEY = "Fraction";

        internal const int DEFAULT_JPEG_QUALITY = 85;
        internal const int MAX_JPEG_SCALE_NUMERATOR = JpegTranscoder.SCALE_DENOMINATOR;
        internal const int MIN_TRANSFORM_INTERVAL_MS = 100;

        internal const float ROUNDUP_FRACTION = 2.0f / 3;

        private readonly IExecutorService _executor;
        private readonly IPooledByteBufferFactory _pooledByteBufferFactory;
        private readonly IProducer<EncodedImage> _inputProducer;

        /// <summary>
        /// Instantiates the <see cref="ResizeAndRotateProducer"/>.
        /// </summary>
        public ResizeAndRotateProducer(
            IExecutorService executor,
            IPooledByteBufferFactory pooledByteBufferFactory,
            IProducer<EncodedImage> inputProducer)
        {
            _executor = Preconditions.CheckNotNull(executor);
            _pooledByteBufferFactory = Preconditions.CheckNotNull(pooledByteBufferFactory);
            _inputProducer = Preconditions.CheckNotNull(inputProducer);
        }

        /// <summary>
        /// Start producing results for given context.
        /// Provided consumer is notified whenever progress is made
        /// (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(IConsumer<EncodedImage> consumer, IProducerContext context)
        {
            _inputProducer.ProduceResults(new TransformingConsumer(this, consumer, context), context);
        }

        private class TransformingConsumer : DelegatingConsumer<EncodedImage, EncodedImage> 
        {
            private readonly ResizeAndRotateProducer _parent;
            private readonly IProducerContext _producerContext;
            private bool _isCancelled;

            private readonly JobScheduler _jobScheduler;

            /// <summary>
            /// Instantiates the <see cref="TransformingConsumer"/>.
            /// </summary>
            public TransformingConsumer(
                ResizeAndRotateProducer parent,
                IConsumer<EncodedImage> consumer,
                IProducerContext producerContext) : 
                    base(consumer)
            {
                _parent = parent;
                _isCancelled = false;
                _producerContext = producerContext;

                Func<EncodedImage, bool, Task> job = (encodedImage, isLast) =>
                {
                    return DoTransform(encodedImage, isLast);
                };

                _jobScheduler = new JobScheduler(
                    _parent._executor, job, MIN_TRANSFORM_INTERVAL_MS);

                _producerContext.AddCallbacks(
                    new BaseProducerContextCallbacks(
                        () => 
                        {
                            _jobScheduler.ClearJob();
                            _isCancelled = true;

                            // This only works if it is safe to discard the output of 
                            // previous producer
                            consumer.OnCancellation();
                        },
                        () => { },
                        () =>
                        {
                            if (_producerContext.IsIntermediateResultExpected)
                            {
                                _jobScheduler.ScheduleJob();
                            }
                        },
                        () => {
                        }));

            }

            protected override void OnNewResultImpl(EncodedImage newResult, bool isLast)
            {
                if (_isCancelled)
                {
                    return;
                }

                if (newResult == null)
                {
                    if (isLast)
                    {
                        Consumer.OnNewResult(null, true);
                    }

                    return;
                }

                TriState shouldTransform = ShouldTransform(
                    _producerContext.ImageRequest, newResult);

                // Ignore the intermediate result if we don't know what to do with it
                if (!isLast && shouldTransform == TriState.UNSET)
                {
                    return;
                }

                // Just forward the result if we know that it shouldn't be transformed
                if (shouldTransform != TriState.YES)
                {
                    Consumer.OnNewResult(newResult, isLast);
                    return;
                }

                // We know that the result should be transformed, hence schedule it
                if (!_jobScheduler.UpdateJob(newResult, isLast))
                {
                    return;
                }

                if (isLast || _producerContext.IsIntermediateResultExpected)
                {
                    _jobScheduler.ScheduleJob();
                }
            }

            private async Task DoTransform(EncodedImage encodedImage, bool isLast)
            {
                _producerContext.Listener.OnProducerStart(_producerContext.Id, PRODUCER_NAME);
                ImageRequest imageRequest = _producerContext.ImageRequest;
                PooledByteBufferOutputStream outputStream = 
                    _parent._pooledByteBufferFactory.NewOutputStream();

                IDictionary<string, string> extraMap = default(IDictionary<string, string>);
                EncodedImage ret = default(EncodedImage);
                Stream inputStream = default(Stream);

                try
                {
                    int numerator = GetScaleNumerator(imageRequest, encodedImage);
                    extraMap = GetExtraMap(encodedImage, imageRequest, numerator);
                    inputStream = encodedImage.GetInputStream();
#if HAS_LIBJPEGTURBO
                    JpegTranscoder.TranscodeJpeg(
                        inputStream.AsIStream(),
                        outputStream.AsIStream(),
                        GetRotationAngle(imageRequest, encodedImage),
                        numerator,
                        DEFAULT_JPEG_QUALITY);
#else // HAS_LIBJPEGTURBO
                    inputStream.CopyTo(outputStream);
#endif // HAS_LIBJPEGTURBO

                    CloseableReference<IPooledByteBuffer> reference =
                           CloseableReference<IPooledByteBuffer>.of(outputStream.ToByteBuffer());

                    try
                    {
                        ret = new EncodedImage(reference);
                        ret.Format = ImageFormat.JPEG;

                        try
                        {
                            await ret.ParseMetaDataAsync().ConfigureAwait(false);
                            _producerContext.Listener.OnProducerFinishWithSuccess(
                                _producerContext.Id, PRODUCER_NAME, extraMap);

                            Consumer.OnNewResult(ret, isLast);
                        }
                        finally
                        {
                            EncodedImage.CloseSafely(ret);
                        }
                    }
                    finally
                    {
                        CloseableReference<IPooledByteBuffer>.CloseSafely(reference);
                    }
                }
                catch (Exception e)
                {
                    _producerContext.Listener.OnProducerFinishWithFailure(
                        _producerContext.Id, PRODUCER_NAME, e, extraMap);

                    Consumer.OnFailure(e);
                    return;
                }
                finally
                {
                    Closeables.CloseQuietly(inputStream);
                    outputStream.Dispose();
                }
            }

            private IDictionary<string, string> GetExtraMap(
                EncodedImage encodedImage,
                ImageRequest imageRequest,
                int numerator)
            {
                if (!_producerContext.Listener.RequiresExtraMap(_producerContext.Id))
                {
                    return null;
                }

                string originalSize = encodedImage.Width + "x" + encodedImage.Height;
                string requestedSize;
                if (imageRequest.ResizeOptions != null)
                {
                    requestedSize = imageRequest.ResizeOptions.Width + "x" + 
                        imageRequest.ResizeOptions.Height;
                }
                else
                {
                    requestedSize = "Unspecified";
                }

                string fraction = numerator > 0 ? numerator + "/8" : "";
                IDictionary<string, string> extraMap = new Dictionary<string, string>()
                {
                    {  ORIGINAL_SIZE_KEY, originalSize },
                    {  REQUESTED_SIZE_KEY, requestedSize },
                    {  FRACTION_KEY, fraction },
                    {  JobScheduler.QUEUE_TIME_KEY, _jobScheduler.GetQueuedTime().ToString() }
                };

                return extraMap;
            }

            private static TriState ShouldTransform(
                ImageRequest request,
                EncodedImage encodedImage)
            {
                if (encodedImage == null || encodedImage.Format == ImageFormat.UNKNOWN)
                {
                    return TriState.UNSET;
                }

                if (encodedImage.Format != ImageFormat.JPEG)
                {
                    return TriState.NO;
                }

                return TriStateHelper.ValueOf(
                    GetRotationAngle(request, encodedImage) != 0 ||
                    ShouldResize(GetScaleNumerator(request, encodedImage)));
            }

            internal static float DetermineResizeRatio(
                ResizeOptions resizeOptions,
                int width,
                int height)
            {

                if (resizeOptions == null)
                {
                    return 1.0f;
                }

                float widthRatio = ((float)resizeOptions.Width) / width;
                float heightRatio = ((float)resizeOptions.Height) / height;
                float ratio = Math.Max(widthRatio, heightRatio);

                // TODO: The limit is larger than this on newer devices.
                if (width * ratio > BitmapUtil.MAX_BITMAP_SIZE)
                {
                    ratio = BitmapUtil.MAX_BITMAP_SIZE / width;
                }

                if (height * ratio > BitmapUtil.MAX_BITMAP_SIZE)
                {
                    ratio = BitmapUtil.MAX_BITMAP_SIZE / height;
                }

                return ratio;
            }

            internal static int RoundNumerator(float maxRatio)
            {
                return (int)(ROUNDUP_FRACTION + maxRatio * JpegTranscoder.SCALE_DENOMINATOR);
            }

            private static int GetScaleNumerator(
                ImageRequest imageRequest,
                EncodedImage encodedImage)
            {
                ResizeOptions resizeOptions = imageRequest.ResizeOptions;
                if (resizeOptions == null)
                {
                    return JpegTranscoder.SCALE_DENOMINATOR;
                }

                int rotationAngle = GetRotationAngle(imageRequest, encodedImage);
                bool swapDimensions = rotationAngle == 90 || rotationAngle == 270;
                int widthAfterRotation = swapDimensions ? encodedImage.Height :
                        encodedImage.Width;

                int heightAfterRotation = swapDimensions ? encodedImage.Width : encodedImage.Height;
                float ratio = DetermineResizeRatio(
                    resizeOptions, widthAfterRotation, heightAfterRotation);

                int numerator = RoundNumerator(ratio);
                if (numerator > MAX_JPEG_SCALE_NUMERATOR)
                {
                    return MAX_JPEG_SCALE_NUMERATOR;
                }

                return (numerator < 1) ? 1 : numerator;
            }

            private static int GetRotationAngle(ImageRequest imageRequest, EncodedImage encodedImage)
            {
                if (!imageRequest.IsAutoRotateEnabled)
                {
                    return 0;
                }

                int rotationAngle = encodedImage.RotationAngle;
                Preconditions.CheckArgument(
                    rotationAngle == 0 || 
                    rotationAngle == 90 || 
                    rotationAngle == 180 || 
                    rotationAngle == 270);

                return rotationAngle;
            }

            private static bool ShouldResize(int numerator)
            {
                return numerator < MAX_JPEG_SCALE_NUMERATOR;
            }
        }
    }
}
