using System;
using FBCore.Common.Internal;
using FBCore.Common.References;
using FBCore.Concurrency;
using ImagePipeline.Bitmaps;
using ImagePipeline.Image;
using ImagePipeline.Request;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Graphics.Imaging;
using ImagePipeline.Memory;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Runs a caller-supplied post-processor object.
    ///
    /// <para />Post-processors are only supported for static bitmaps.
    /// If the request is for an animated image, the post-processor
    /// step will be skipped without warning.
    /// </summary>
    public class PostprocessorProducer : IProducer<CloseableReference<CloseableImage>>
    {
        internal const string NAME = "PostprocessorProducer";
        internal const string POSTPROCESSOR = "Postprocessor";

        private readonly IProducer<CloseableReference<CloseableImage>> _inputProducer;
        private readonly PlatformBitmapFactory _bitmapFactory;
        private readonly FlexByteArrayPool _flexByteArrayPool;
        private readonly IExecutorService _executor;

        /// <summary>
        /// Instantiates the <see cref="PostprocessorProducer"/>.
        /// </summary>
        public PostprocessorProducer(
            IProducer<CloseableReference<CloseableImage>> inputProducer,
            PlatformBitmapFactory platformBitmapFactory,
            FlexByteArrayPool flexByteArrayPool,
            IExecutorService executor)
        {
            _inputProducer = Preconditions.CheckNotNull(inputProducer);
            _bitmapFactory = platformBitmapFactory;
            _flexByteArrayPool = flexByteArrayPool;
            _executor = Preconditions.CheckNotNull(executor);
        }

        /// <summary>
        /// Start producing results for given context.
        /// Provided consumer is notified whenever progress is made
        /// (new value is ready or error occurs).
        /// </summary>
        public void ProduceResults(
            IConsumer<CloseableReference<CloseableImage>> consumer, 
            IProducerContext context)
        {
            IProducerListener listener = context.Listener;
            IPostprocessor postprocessor = context.ImageRequest.Postprocessor;
            PostprocessorConsumer basePostprocessorConsumer =
                new PostprocessorConsumer(this, consumer, listener, context.Id, postprocessor, context);

            IConsumer< CloseableReference<CloseableImage>> postprocessorConsumer;
            if (postprocessor.GetType() == typeof(IRepeatedPostprocessor))
            {
                postprocessorConsumer = new RepeatedPostprocessorConsumer(
                    basePostprocessorConsumer,
                    (IRepeatedPostprocessor)postprocessor,
                    context);
            }
            else
            {
                postprocessorConsumer = new SingleUsePostprocessorConsumer(basePostprocessorConsumer);
            }

            _inputProducer.ProduceResults(postprocessorConsumer, context);
        }

        /// <summary>
        /// Performs postprocessing and takes care of scheduling.
        /// </summary>
        internal class PostprocessorConsumer : 
            DelegatingConsumer<CloseableReference<CloseableImage>, CloseableReference<CloseableImage>>
        {
            private readonly object _gate = new object();

            private PostprocessorProducer _parent;
            private readonly IProducerListener _listener;
            private readonly string _requestId;
            private readonly IPostprocessor _postprocessor;
            private bool _isClosed;
            private CloseableReference<CloseableImage> _sourceImageRef;
            private bool _isLast;
            private bool _isDirty;
            private bool _isPostProcessingRunning;

            internal PostprocessorConsumer(
                PostprocessorProducer parent,
                IConsumer<CloseableReference<CloseableImage>> consumer,
                IProducerListener listener,
                string requestId,
                IPostprocessor postprocessor,
                IProducerContext producerContext) : 
                base(consumer)
            {
                _parent = parent;
                _listener = listener;
                _requestId = requestId;
                _postprocessor = postprocessor;
                producerContext.AddCallbacks(
                    new BaseProducerContextCallbacks(
                        () => 
                        {
                            MaybeNotifyOnCancellation();
                        },
                        () => { },
                        () => { },
                        () => { }));
            }

            protected override void OnNewResultImpl(
                CloseableReference<CloseableImage> newResult, bool isLast)
            {
                if (!CloseableReference<CloseableImage>.IsValid(newResult))
                {
                    // try to propagate if the last result is invalid
                    if (isLast)
                    {
                        MaybeNotifyOnNewResult(null, true);
                    }

                    // ignore if invalid
                    return;
                }

                UpdateSourceImageRef(newResult, isLast);
            }

            protected override void OnFailureImpl(Exception t)
            {
                MaybeNotifyOnFailure(t);
            }

            protected override void OnCancellationImpl()
            {
                MaybeNotifyOnCancellation();
            }

            private void UpdateSourceImageRef(
                CloseableReference<CloseableImage> sourceImageRef,
                bool isLast)
            {
                CloseableReference<CloseableImage> oldSourceImageRef;
                bool shouldSubmit;
                lock (_gate)
                {
                    if (_isClosed)
                    {
                        return;
                    }

                    oldSourceImageRef = _sourceImageRef;
                    _sourceImageRef = CloseableReference<CloseableImage>.CloneOrNull(sourceImageRef);
                    _isLast = isLast;
                    _isDirty = true;
                    shouldSubmit = SetRunningIfDirtyAndNotRunning();
                }

                CloseableReference<CloseableImage>.CloseSafely(oldSourceImageRef);
                if (shouldSubmit)
                {
                    SubmitPostprocessing();
                }
            }

            private void SubmitPostprocessing()
            {
                _parent._executor.Execute(() =>
                {
                    CloseableReference<CloseableImage> closeableImageRef;
                    bool isLast;
                    lock (_gate)
                    {
                        // instead of cloning and closing the reference, we do a more
                        // efficient move.
                        closeableImageRef = _sourceImageRef;
                        isLast = _isLast;
                        _sourceImageRef = null;
                        _isDirty = false;
                    }

                    if (CloseableReference<CloseableImage>.IsValid(closeableImageRef))
                    {
                        try
                        {
                            DoPostprocessing(closeableImageRef, isLast);
                        }
                        finally
                        {
                            CloseableReference<CloseableImage>.CloseSafely(closeableImageRef);
                        }
                    }

                    ClearRunningAndStartIfDirty();
                });
            }

            private void ClearRunningAndStartIfDirty()
            {
                bool shouldExecuteAgain;
                lock (_gate)
                {
                    _isPostProcessingRunning = false;
                    shouldExecuteAgain = SetRunningIfDirtyAndNotRunning();
                }

                if (shouldExecuteAgain)
                {
                    SubmitPostprocessing();
                }
            }

            private bool SetRunningIfDirtyAndNotRunning()
            {
                lock (_gate)
                {
                    if (!_isClosed && _isDirty && !_isPostProcessingRunning &&
                        CloseableReference<CloseableImage>.IsValid(_sourceImageRef))
                    {
                        _isPostProcessingRunning = true;
                        return true;
                    }

                    return false;
                }
            }

            private void DoPostprocessing(
                CloseableReference<CloseableImage> sourceImageRef,
                bool isLast)
            {
                Preconditions.CheckArgument(CloseableReference<CloseableImage>.IsValid(sourceImageRef));
                if (!ShouldPostprocess(sourceImageRef.Get()))
                {
                    MaybeNotifyOnNewResult(sourceImageRef, isLast);
                    return;
                }

                _listener.OnProducerStart(_requestId, NAME);
                CloseableReference<CloseableImage> destImageRef = null;

                try
                {
                    try
                    {
                        destImageRef = PostprocessInternal(sourceImageRef.Get());
                    }
                    catch (Exception e)
                    {
                        _listener.OnProducerFinishWithFailure(
                            _requestId, NAME, e, GetExtraMap(_listener, _requestId, _postprocessor));

                        MaybeNotifyOnFailure(e);
                        return;
                    }

                    _listener.OnProducerFinishWithSuccess(
                        _requestId, NAME, GetExtraMap(_listener, _requestId, _postprocessor));

                    MaybeNotifyOnNewResult(destImageRef, isLast);
                }
                finally
                {
                    CloseableReference<CloseableImage>.CloseSafely(destImageRef);
                }
            }

            private IDictionary<string, string> GetExtraMap(
                IProducerListener listener,
                string requestId,
                IPostprocessor postprocessor)
            {
                if (!listener.RequiresExtraMap(requestId))
                {
                    return null;
                }

                var extraMap = new Dictionary<string, string>()
                {
                    {  POSTPROCESSOR, postprocessor.Name }
                };

                return new ReadOnlyDictionary<string, string>(extraMap);
            }

            private bool ShouldPostprocess(CloseableImage sourceImage)
            {
                return (sourceImage.GetType() == typeof(CloseableStaticBitmap));
            }

            private CloseableReference<CloseableImage> PostprocessInternal(CloseableImage sourceImage)
            {
                CloseableStaticBitmap staticBitmap = (CloseableStaticBitmap)sourceImage;
                SoftwareBitmap sourceBitmap = staticBitmap.UnderlyingBitmap;
                CloseableReference<SoftwareBitmap> bitmapRef = 
                    _postprocessor.Process(
                        sourceBitmap, 
                        _parent._bitmapFactory, 
                        _parent._flexByteArrayPool);

                int rotationAngle = staticBitmap.RotationAngle;

                try
                {
                    return CloseableReference<CloseableImage>.of(
                        new CloseableStaticBitmap(bitmapRef, sourceImage.QualityInfo, rotationAngle));
                }
                finally
                {
                    CloseableReference<SoftwareBitmap>.CloseSafely(bitmapRef);
                }
            }

            private void MaybeNotifyOnNewResult(CloseableReference<CloseableImage> newRef, bool isLast)
            {
                if ((!isLast && !IsClosed()) || (isLast && Close()))
                {
                    Consumer.OnNewResult(newRef, isLast);
                }
            }

            private void MaybeNotifyOnFailure(Exception throwable)
            {
                if (Close())
                {
                    Consumer.OnFailure(throwable);
                }
            }

            private void MaybeNotifyOnCancellation()
            {
                if (Close())
                {
                    Consumer.OnCancellation();
                }
            }

            private bool IsClosed()
            {
                lock (_gate)
                {
                    return _isClosed;
                }
            }

            private bool Close()
            {
                CloseableReference<CloseableImage> oldSourceImageRef;
                lock (_gate)
                {
                    if (_isClosed)
                    {
                        return false;
                    }

                    oldSourceImageRef = _sourceImageRef;
                    _sourceImageRef = null;
                    _isClosed = true;
                }

                CloseableReference<CloseableImage>.CloseSafely(oldSourceImageRef);
                return true;
            }
        }

        /// <summary>
        /// PostprocessorConsumer wrapper that ignores intermediate results.
        /// </summary>
        internal class SingleUsePostprocessorConsumer :
            DelegatingConsumer<CloseableReference<CloseableImage>, CloseableReference<CloseableImage>>
        {
            internal SingleUsePostprocessorConsumer(PostprocessorConsumer postprocessorConsumer) :
                base(postprocessorConsumer)
            {
            }

            protected override void OnNewResultImpl(
                CloseableReference<CloseableImage> newResult,
                bool isLast)
            {
                // ignore intermediate results
                if (!isLast)
                {
                    return;
                }

                Consumer.OnNewResult(newResult, isLast);
            }
        }

        /// <summary>
        /// PostprocessorConsumer wrapper that allows repeated postprocessing.
        ///
        /// <para />Reference to the last result received is cloned and kept
        /// until the request is cancelled. In order to allow multiple
        /// postprocessing, results are always propagated as non-final.
        /// When Update() is called, a new postprocessing of the last received
        /// result is requested.
        ///
        /// <para />Intermediate results are ignored.
        /// </summary>
        internal class RepeatedPostprocessorConsumer :
            DelegatingConsumer<CloseableReference<CloseableImage>, CloseableReference<CloseableImage>>,
            IRepeatedPostprocessorRunner
        {
            private readonly object _gate = new object();
            private bool _isClosed = false;
            private CloseableReference<CloseableImage> _sourceImageRef = null;

            internal RepeatedPostprocessorConsumer(
                PostprocessorConsumer postprocessorConsumer,
                IRepeatedPostprocessor repeatedPostprocessor,
                IProducerContext context) :
                base(postprocessorConsumer)
            {
                repeatedPostprocessor.SetCallback(this);
                context.AddCallbacks(
                    new BaseProducerContextCallbacks(
                        () => 
                        {
                            if (Close())
                            {
                                Consumer.OnCancellation();
                            }
                        },
                        () => { },
                        () => { },
                        () => { }));
            }

            protected override void OnNewResultImpl(
                CloseableReference<CloseableImage> newResult, bool isLast)
            {
                // ignore intermediate results
                if (!isLast)
                {
                    return;
                }

                SetSourceImageRef(newResult);
                UpdateInternal();
            }

            protected override void OnFailureImpl(Exception throwable)
            {
                if (Close())
                {
                    Consumer.OnFailure(throwable);
                }
            }

            protected override void OnCancellationImpl()
            {
                if (Close())
                {
                    Consumer.OnCancellation();
                }
            }

            public void Update()
            {
                lock (_gate)
                {
                    UpdateInternal();
                }
            }

            private void UpdateInternal()
            {
                CloseableReference<CloseableImage> sourceImageRef;
                lock (_gate)
                {
                    if (_isClosed)
                    {
                        return;
                    }

                    sourceImageRef = CloseableReference<CloseableImage>.CloneOrNull(_sourceImageRef);
                }

                try
                {
                    Consumer.OnNewResult(sourceImageRef, false /* isLast */);
                }
                finally
                {
                    CloseableReference<CloseableImage>.CloseSafely(sourceImageRef);
                }
            }

            private void SetSourceImageRef(CloseableReference<CloseableImage> sourceImageRef)
            {
                CloseableReference<CloseableImage> oldSourceImageRef;
                lock (_gate)
                {
                    if (_isClosed)
                    {
                        return;
                    }

                    oldSourceImageRef = _sourceImageRef;
                    _sourceImageRef = CloseableReference<CloseableImage>.CloneOrNull(sourceImageRef);
                }

                CloseableReference<CloseableImage>.CloseSafely(oldSourceImageRef);
            }

            private bool Close()
            {
                CloseableReference<CloseableImage> oldSourceImageRef;
                lock (_gate)
                {
                    if (_isClosed)
                    {
                        return false;
                    }

                    oldSourceImageRef = _sourceImageRef;
                    _sourceImageRef = null;
                    _isClosed = true;
                }

                CloseableReference<CloseableImage>.CloseSafely(oldSourceImageRef);
                return true;
            }
        }
    }
}
