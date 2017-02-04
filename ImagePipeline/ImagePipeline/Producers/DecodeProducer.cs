using FBCore.Common.Internal;
using FBCore.Common.References;
using FBCore.Common.Util;
using FBCore.Concurrency;
using ImagePipeline.Common;
using ImagePipeline.Decoder;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using ImagePipeline.Request;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Decodes images.
    ///
    /// <p/> Progressive JPEGs are decoded progressively as new data arrives.
    /// </summary>
    public class DecodeProducer : IProducer<CloseableReference<CloseableImage>>
    {
        /// <summary>
        /// The producer name
        /// </summary>
        public const string PRODUCER_NAME = "DecodeProducer";

        /// <summary>
        /// Keys for extra map
        /// </summary>
        private const string BITMAP_SIZE_KEY = "bitmapSize";
        private const string HAS_GOOD_QUALITY_KEY = "hasGoodQuality";
        private const string IMAGE_TYPE_KEY = "imageType";
        private const string IS_FINAL_KEY = "isFinal";

        private readonly IByteArrayPool _byteArrayPool;
        private readonly IExecutorService _executor;
        private readonly ImageDecoder _imageDecoder;
        private readonly IProgressiveJpegConfig _progressiveJpegConfig;
        private readonly IProducer<EncodedImage> _inputProducer;
        private readonly bool _downsampleEnabled;
        private readonly bool _downsampleEnabledForNetwork;

        /// <summary>
        /// Instantiates the <see cref="DecodeProducer"/>
        /// </summary>
        public DecodeProducer(
            IByteArrayPool byteArrayPool,
            IExecutorService executor,
            ImageDecoder imageDecoder,
            IProgressiveJpegConfig progressiveJpegConfig,
            bool downsampleEnabled,
            bool downsampleEnabledForNetwork,
            IProducer<EncodedImage> inputProducer)
        {
            _byteArrayPool = Preconditions.CheckNotNull(byteArrayPool);
            _executor = Preconditions.CheckNotNull(executor);
            _imageDecoder = Preconditions.CheckNotNull(imageDecoder);
            _progressiveJpegConfig = Preconditions.CheckNotNull(progressiveJpegConfig);
            _downsampleEnabled = downsampleEnabled;
            _downsampleEnabledForNetwork = downsampleEnabledForNetwork;
            _inputProducer = Preconditions.CheckNotNull(inputProducer);
        }

        /// <summary>
        /// Start producing results for given context. Provided consumer is notified whenever 
        /// progress is made (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(
            IConsumer<CloseableReference<CloseableImage>> consumer,
            IProducerContext producerContext)
        {
            ImageRequest imageRequest = producerContext.ImageRequest;
            ProgressiveDecoder progressiveDecoder;
            if (!UriUtil.IsNetworkUri(imageRequest.SourceUri))
            {
                progressiveDecoder = new LocalImagesProgressiveDecoder(this, consumer, producerContext);
            }
            else
            {
                ProgressiveJpegParser jpegParser = new ProgressiveJpegParser(_byteArrayPool);
                progressiveDecoder = new NetworkImagesProgressiveDecoder(
                    this,
                    consumer,
                    producerContext,
                    jpegParser,
                    _progressiveJpegConfig);
            }

            _inputProducer.ProduceResults(progressiveDecoder, producerContext);
        }

        /// <summary>
        /// Progressive decoder.
        /// </summary>
        internal abstract class ProgressiveDecoder : DelegatingConsumer<
            EncodedImage, CloseableReference<CloseableImage>> 
        {
            protected readonly object _gate = new object();

            private readonly DecodeProducer _parent;
            private readonly IProducerContext _producerContext;
            private readonly IProducerListener _producerListener;
            private readonly ImageDecodeOptions _imageDecodeOptions;

            private bool _isFinished;

            private readonly JobScheduler _jobScheduler;

            /// <summary>
            /// Instantiates the <see cref="ProgressiveDecoder"/>.
            /// </summary>
            public ProgressiveDecoder(
                DecodeProducer parent,
                IConsumer<CloseableReference<CloseableImage>> consumer,
                IProducerContext producerContext) : 
                base(consumer)
            {
                _parent = parent;
                _producerContext = producerContext;
                _producerListener = producerContext.Listener;
                _imageDecodeOptions = producerContext.ImageRequest.ImageDecodeOptions;
                _isFinished = false;
                Action<EncodedImage, bool> job = (encodedImage, isLast) =>
                {
                    if (encodedImage != null)
                    {
                        if (_parent._downsampleEnabled)
                        {
                            ImageRequest request = producerContext.ImageRequest;
                            if (_parent._downsampleEnabledForNetwork ||
                                !UriUtil.IsNetworkUri(request.SourceUri))
                            {
                                encodedImage.SampleSize = DownsampleUtil.DetermineSampleSize(
                                    request, encodedImage);
                            }
                        }

                        DoDecode(encodedImage, isLast);
                    }
                };

                _jobScheduler = new JobScheduler(
                    _parent._executor, job, _imageDecodeOptions.MinDecodeIntervalMs);

                _producerContext.AddCallbacks(
                    new BaseProducerContextCallbacks(
                    () => { },
                    () => { },
                    () => 
                    {
                        if (_producerContext.IsIntermediateResultExpected)
                        {
                            _jobScheduler.ScheduleJob();
                        }
                    },
                    () => { }));
            }

            /// <summary>
            /// Called by a producer whenever new data is produced. This method should not 
            /// throw an exception.
            ///
            /// <para /> In case when result is closeable resource producer will close it 
            /// after OnNewResult returns. Consumer needs to make copy of it if the resource 
            /// must be accessed after that. Fortunately, with CloseableReferences, that 
            /// should not impose too much overhead.
            ///
            /// <param name="newResult">The result provided by the producer</param>
            /// <param name="isLast">True if newResult is the last result</param>
            /// </summary>

            protected override void OnNewResultImpl(EncodedImage newResult, bool isLast)
            {
                if (isLast && !EncodedImage.IsValid(newResult))
                {
                    HandleError(new ArgumentNullException("Encoded image is not valid."));
                    return;
                }

                if (!UpdateDecodeJob(newResult, isLast))
                {
                    return;
                }

                if (isLast || _producerContext.IsIntermediateResultExpected)
                {
                    _jobScheduler.ScheduleJob();
                }
            }

            /// <summary>
            /// Called when the progress updates.
            /// </summary>
            protected override void OnProgressUpdateImpl(float progress)
            {
                base.OnProgressUpdateImpl(progress * 0.99f);
            }

            /// <summary>
            /// Called by OnFailure.
            /// </summary>
            protected override void OnFailureImpl(Exception t)
            {
                HandleError(t);
            }

            /// <summary>
            /// Called by OnCancellation.
            /// </summary>
            protected override void OnCancellationImpl()
            {
                HandleCancellation();
            }

            /// <summary>
            /// Updates the decode job.
            /// </summary>
            protected virtual bool UpdateDecodeJob(EncodedImage reference, bool isLast)
            {
                return _jobScheduler.UpdateJob(reference, isLast);
            }

            /// <summary>
            /// Performs the decode synchronously.
            /// </summary>
            private void DoDecode(EncodedImage encodedImage, bool isLast)
            {
                if (IsFinished() || !EncodedImage.IsValid(encodedImage))
                {
                    return;
                }

                try
                {
                    long queueTime = _jobScheduler.GetQueuedTime();
                    int length = isLast ?
                        encodedImage.Size : GetIntermediateImageEndOffset(encodedImage);

                    IQualityInfo quality = isLast ? ImmutableQualityInfo.FULL_QUALITY : GetQualityInfo();

                    _producerListener.OnProducerStart(_producerContext.Id, PRODUCER_NAME);
                    CloseableImage image = null;

                    try
                    {
                        image = _parent._imageDecoder
                            .DecodeImageAsync(encodedImage, length, quality, _imageDecodeOptions)
                            .GetAwaiter()
                            .GetResult();
                    }
                    catch (Exception e)
                    {
                        _producerListener.OnProducerFinishWithFailure(
                            _producerContext.Id, 
                            PRODUCER_NAME, 
                            e, 
                            GetExtraMap(image, queueTime, quality, isLast));

                        HandleError(e);
                        return;
                    }

                    _producerListener.OnProducerFinishWithSuccess(
                        _producerContext.Id, 
                        PRODUCER_NAME,
                        GetExtraMap(image, queueTime, quality, isLast));

                    HandleResult(image, isLast);
                }
                finally
                {
                    EncodedImage.CloseSafely(encodedImage);
                }
            }

            private IDictionary<string, string> GetExtraMap(
                CloseableImage image,
                long queueTime,
                IQualityInfo quality,
                bool isFinal)
            {
                if (!_producerListener.RequiresExtraMap(_producerContext.Id))
                {
                    return null;
                }

                string queueStr = queueTime.ToString();
                string qualityStr = quality.IsOfGoodEnoughQuality.ToString();
                string finalStr = isFinal.ToString();
                string cacheChoiceStr = _producerContext.ImageRequest.CacheChoice.ToString();
                if (image.GetType() == typeof(CloseableStaticBitmap))
                {
                    SoftwareBitmap bitmap = ((CloseableStaticBitmap)image).UnderlyingBitmap;
                    string sizeStr = bitmap.PixelWidth + "x" + bitmap.PixelHeight;
                    var extraMap = new Dictionary<string, string>()
                    {
                        {  BITMAP_SIZE_KEY, sizeStr },
                        {  JobScheduler.QUEUE_TIME_KEY, queueStr },
                        {  HAS_GOOD_QUALITY_KEY, qualityStr },
                        {  IS_FINAL_KEY, finalStr },
                        {  IMAGE_TYPE_KEY, cacheChoiceStr }
                    };

                    return new ReadOnlyDictionary<string, string>(extraMap);
                }
                else
                {
                    var extraMap = new Dictionary<string, string>()
                    {
                        {  JobScheduler.QUEUE_TIME_KEY, queueStr },
                        {  HAS_GOOD_QUALITY_KEY, qualityStr },
                        {  IS_FINAL_KEY, finalStr },
                        {  IMAGE_TYPE_KEY, cacheChoiceStr }
                    };

                    return new ReadOnlyDictionary<string, string>(extraMap);
                }
            }

            /// <summary>
            /// @return true if producer is finished.
            /// </summary>
            private bool IsFinished()
            {
                lock (_gate)
                {
                    return _isFinished;
                }
            }

            /// <summary>
            /// Finishes if not already finished and <code>shouldFinish</code> is specified.
            /// <para /> If just finished, the intermediate image gets released.
            /// </summary>
            private void MaybeFinish(bool shouldFinish)
            {
                lock (_gate)
                {
                    if (!shouldFinish || _isFinished)
                    {
                        return;
                    }

                    Consumer.OnProgressUpdate(1.0f);
                    _isFinished = true;
                }

                _jobScheduler.ClearJob();
            }

            /// <summary>
            /// Notifies consumer of new result and finishes if the result is final.
            /// </summary>
            private void HandleResult(CloseableImage decodedImage, bool isFinal)
            {
                var decodedImageRef = CloseableReference<CloseableImage>.of(decodedImage);

                try
                {
                    MaybeFinish(isFinal);
                    Consumer.OnNewResult(decodedImageRef, isFinal);
                }
                finally
                {
                    CloseableReference<CloseableImage>.CloseSafely(decodedImageRef);
                }
            }

            /// <summary>
            /// Notifies consumer about the failure and finishes.
            /// </summary>
            private void HandleError(Exception t)
            {
                MaybeFinish(true);
                Consumer.OnFailure(t);
            }

            /// <summary>
            /// Notifies consumer about the cancellation and finishes.
            /// </summary>
            private void HandleCancellation()
            {
                MaybeFinish(true);
                Consumer.OnCancellation();
            }

            /// <summary>
            /// Gets the intermediate image end offset.
            /// </summary>
            protected abstract int GetIntermediateImageEndOffset(EncodedImage encodedImage);

            /// <summary>
            /// Gets the quality info.
            /// </summary>
            /// <returns></returns>
            protected abstract IQualityInfo GetQualityInfo();
        }

        internal class LocalImagesProgressiveDecoder : ProgressiveDecoder
        {
            public LocalImagesProgressiveDecoder(
                DecodeProducer parent,
                IConsumer<CloseableReference<CloseableImage>> consumer,
                IProducerContext producerContext) : base(
                    parent,
                    consumer,
                    producerContext)
            {
            }

            protected override bool UpdateDecodeJob(EncodedImage encodedImage, bool isLast)
            {
                lock (_gate)
                {
                    if (!isLast)
                    {
                        return false;
                    }

                    return base.UpdateDecodeJob(encodedImage, isLast);
                }
            }

            protected override int GetIntermediateImageEndOffset(EncodedImage encodedImage)
            {
                return encodedImage.Size;
            }

            protected override IQualityInfo GetQualityInfo()
            {
                return ImmutableQualityInfo.of(0, false, false);
            }
        }

        internal class NetworkImagesProgressiveDecoder : ProgressiveDecoder
        {
            private ProgressiveJpegParser _progressiveJpegParser;
            private IProgressiveJpegConfig _progressiveJpegConfig;
            private int _lastScheduledScanNumber;

            public NetworkImagesProgressiveDecoder(
                DecodeProducer parent,
                IConsumer<CloseableReference<CloseableImage>> consumer,
                IProducerContext producerContext,
                ProgressiveJpegParser progressiveJpegParser,
                IProgressiveJpegConfig progressiveJpegConfig) : base(
                    parent,
                    consumer,
                    producerContext)
            {
                _progressiveJpegParser = Preconditions.CheckNotNull(progressiveJpegParser);
                _progressiveJpegConfig = Preconditions.CheckNotNull(progressiveJpegConfig);
                _lastScheduledScanNumber = 0;
            }

            protected override bool UpdateDecodeJob(EncodedImage encodedImage, bool isLast)
            {
                lock (_gate)
                {
                    bool ret = base.UpdateDecodeJob(encodedImage, isLast);
                    if (!isLast && EncodedImage.IsValid(encodedImage))
                    {
                        if (!_progressiveJpegParser.ParseMoreData(encodedImage))
                        {
                            return false;
                        }

                        int scanNum = _progressiveJpegParser.BestScanNumber;
                        if (scanNum <= _lastScheduledScanNumber ||
                            scanNum < _progressiveJpegConfig.GetNextScanNumberToDecode(
                                _lastScheduledScanNumber))
                        {
                            return false;
                        }

                        _lastScheduledScanNumber = scanNum;
                    }

                    return ret;
                }
            }

            protected override int GetIntermediateImageEndOffset(EncodedImage encodedImage)
            {
                return _progressiveJpegParser.BestScanEndOffset;
            }

            protected override IQualityInfo GetQualityInfo()
            {
                return _progressiveJpegConfig.GetQualityInfo(_progressiveJpegParser.BestScanNumber);
            }
        }
    }
}
