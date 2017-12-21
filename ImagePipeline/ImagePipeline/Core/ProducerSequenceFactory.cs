using FBCore.Common.Internal;
using FBCore.Common.Media;
using FBCore.Common.References;
using FBCore.Common.Util;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using ImagePipeline.Producers;
using ImagePipeline.Request;
using System;
using System.Collections.Generic;

namespace ImagePipeline.Core
{
    /// <summary>
    /// Factory class that creates all producer sequences.
    /// </summary>
    public class ProducerSequenceFactory
    {
        private readonly object _gate = new object();

        private readonly ProducerFactory _producerFactory;
        private readonly INetworkFetcher<FetchState> _networkFetcher;
        private readonly bool _resizeAndRotateEnabledForNetwork;
        private readonly bool _webpSupportEnabled;
        private readonly bool _downsampleEnabled;
        private readonly ThreadHandoffProducerQueue _threadHandoffProducerQueue;
        private readonly int _throttlingMaxSimultaneousRequests;
        private readonly FlexByteArrayPool _flexByteArrayPool;

        // Saved sequences
        internal IProducer<CloseableReference<CloseableImage>> _networkFetchSequence;
        internal IProducer<EncodedImage> _backgroundNetworkFetchToEncodedMemorySequence;
        internal IProducer<CloseableReference<IPooledByteBuffer>> _encodedImageProducerSequence;
        internal IProducer<object> _networkFetchToEncodedMemoryPrefetchSequence;
        internal IProducer<EncodedImage> _commonNetworkFetchToEncodedMemorySequence;
        internal IProducer<CloseableReference<IPooledByteBuffer>> _encodedLocalImageFileFetchSequence;
        internal IProducer<CloseableReference<IPooledByteBuffer>> _encodedLocalResourceFetchSequence;
        internal IProducer<CloseableReference<IPooledByteBuffer>> _encodedLocalAssetFetchSequence;
        internal IProducer<CloseableReference<IPooledByteBuffer>> _encodedDataFetchSequence;
        internal IProducer<CloseableReference<IPooledByteBuffer>> _encodedFutureAccessListFetchSequence;
        internal IProducer<CloseableReference<CloseableImage>> _localImageFileFetchSequence;
        internal IProducer<CloseableReference<CloseableImage>> _localVideoFileFetchSequence;
        internal IProducer<CloseableReference<CloseableImage>> _localResourceFetchSequence;
        internal IProducer<CloseableReference<CloseableImage>> _localAssetFetchSequence;
        internal IProducer<CloseableReference<CloseableImage>> _dataFetchSequence;
        internal IProducer<CloseableReference<CloseableImage>> _futureAccessListFetchSequence;
        internal IDictionary<
            IProducer<CloseableReference<CloseableImage>>,
            IProducer<CloseableReference<CloseableImage>>> _postprocessorSequences;

        internal IDictionary<
            IProducer<CloseableReference<CloseableImage>>, 
            IProducer<object>> _closeableImagePrefetchSequences;

        /// <summary>
        /// Instantiates the <see cref="ProducerSequenceFactory"/>.
        /// </summary>
        public ProducerSequenceFactory(
            ProducerFactory producerFactory,
            INetworkFetcher<FetchState> networkFetcher,
            bool resizeAndRotateEnabledForNetwork,
            bool downsampleEnabled,
            bool webpSupportEnabled,
            ThreadHandoffProducerQueue threadHandoffProducerQueue,
            int throttlingMaxSimultaneousRequests,
            FlexByteArrayPool flexByteArrayPool)
        {
            _producerFactory = producerFactory;
            _networkFetcher = networkFetcher;
            _resizeAndRotateEnabledForNetwork = resizeAndRotateEnabledForNetwork;
            _downsampleEnabled = downsampleEnabled;
            _webpSupportEnabled = webpSupportEnabled;
            _postprocessorSequences = new Dictionary<
                IProducer<CloseableReference<CloseableImage>>,
                IProducer<CloseableReference<CloseableImage>>>();

            _closeableImagePrefetchSequences = new Dictionary<
                IProducer<CloseableReference<CloseableImage>>,
                IProducer<object>>();

            _threadHandoffProducerQueue = threadHandoffProducerQueue;
            _throttlingMaxSimultaneousRequests = throttlingMaxSimultaneousRequests;
            _flexByteArrayPool = flexByteArrayPool;
        }

        /// <summary>
        /// Returns a sequence that can be used for a request for
        /// an encoded image.
        /// </summary>
        /// <param name="imageRequest">
        /// The request that will be submitted.
        /// </param>
        /// <returns>
        /// The sequence that should be used to process the request.
        /// </returns>
        public IProducer<CloseableReference<IPooledByteBuffer>> GetEncodedImageProducerSequence(
            ImageRequest imageRequest)
        {
            IProducer<CloseableReference<IPooledByteBuffer>> pipelineSequence =
                GetBasicEncodedImageSequence(imageRequest);

            return pipelineSequence;
        }

        /// <summary>
        /// Returns a sequence that can be used for a prefetch request
        /// for an encoded image.
        ///
        /// <para />Guaranteed to return the same sequence as
        /// <code>GetEncodedImageProducerSequence(request)</code>,
        /// except that it is pre-pended with a
        /// <see cref="SwallowResultProducer{T}"/>.
        /// </summary>
        /// <param name="imageRequest">
        /// The request that will be submitted.
        /// </param>
        /// <returns>
        /// The sequence that should be used to process the request.
        /// </returns>
        public IProducer<object> GetEncodedImagePrefetchProducerSequence(ImageRequest imageRequest)
        {
            ValidateEncodedImageRequest(imageRequest);
            return GetNetworkFetchToEncodedMemoryPrefetchSequence();
        }

        /// <summary>
        /// Returns a sequence that can be used for a request for a
        /// decoded image.
        /// </summary>
        /// <param name="imageRequest">
        /// The request that will be submitted.
        /// </param>
        /// <returns>
        /// The sequence that should be used to process the request.
        /// </returns>
        public IProducer<CloseableReference<CloseableImage>> GetDecodedImageProducerSequence(
            ImageRequest imageRequest)
        {
            IProducer<CloseableReference<CloseableImage>> pipelineSequence =
                GetBasicDecodedImageSequence(imageRequest);

            if (imageRequest.Postprocessor != null)
            {
                return GetPostprocessorSequence(pipelineSequence);
            }
            else
            {
                return pipelineSequence;
            }
        }

        /// <summary>
        /// Returns a sequence that can be used for a prefetch request
        /// for a decoded image.
        /// </summary>
        /// <param name="imageRequest">
        /// The request that will be submitted.
        /// </param>
        /// <returns>
        /// The sequence that should be used to process the request.
        /// </returns>
        public IProducer<object> GetDecodedImagePrefetchProducerSequence(
            ImageRequest imageRequest)
        {
            return GetDecodedImagePrefetchSequence(GetBasicDecodedImageSequence(imageRequest));
        }

        /// <summary>
        /// post-processor producer -> copy producer -> inputProducer.
        /// </summary>
        private IProducer<CloseableReference<CloseableImage>> GetPostprocessorSequence(

            IProducer<CloseableReference<CloseableImage>> inputProducer)
        {
            lock (_gate)
            {
                if (!_postprocessorSequences.ContainsKey(inputProducer))
                {
                    PostprocessorProducer postprocessorProducer =
                        _producerFactory.NewPostprocessorProducer(inputProducer);

                    PostprocessedBitmapMemoryCacheProducer postprocessedBitmapMemoryCacheProducer =
                        _producerFactory.NewPostprocessorBitmapMemoryCacheProducer(postprocessorProducer);

                    _postprocessorSequences.Add(inputProducer, postprocessedBitmapMemoryCacheProducer);
                }

                var producer  = default(IProducer<CloseableReference<CloseableImage>>);
                _postprocessorSequences.TryGetValue(inputProducer, out producer);
                return producer;
            }
        }

        /// <summary>
        /// swallow-result -> background-thread hand-off -> multiplex -> 
        /// encoded cache -> disk cache -> (webp transcode) -> network fetch.
        /// </summary>
        private IProducer<object> GetNetworkFetchToEncodedMemoryPrefetchSequence()
        {
            lock (_gate)
            {
                if (_networkFetchToEncodedMemoryPrefetchSequence == null)
                {
                    _networkFetchToEncodedMemoryPrefetchSequence =
                        ProducerFactory.NewSwallowResultProducer(
                            GetBackgroundNetworkFetchToEncodedMemorySequence());
                }

                return _networkFetchToEncodedMemoryPrefetchSequence;
            }
        }

        /// <summary>
        /// encoded cache multiplex -> encoded cache -> (disk cache) ->
        /// (webp transcode).
        /// </summary>
        /// <param name="inputProducer">
        /// Producer providing the input to the transcode.
        /// </param>
        /// <returns>
        /// Encoded cache multiplex to webp transcode sequence.
        /// </returns>
        private IProducer<EncodedImage> NewEncodedCacheMultiplexToTranscodeSequence(
            IProducer<EncodedImage> inputProducer)
        {
            // TODO: check webp transcode support
            //inputProducer = _producerFactory.NewWebpTranscodeProducer(inputProducer);

            inputProducer = _producerFactory.NewDiskCacheProducer(inputProducer);
            EncodedMemoryCacheProducer encodedMemoryCacheProducer =
                _producerFactory.NewEncodedMemoryCacheProducer(inputProducer);

            return _producerFactory.NewEncodedCacheKeyMultiplexProducer(encodedMemoryCacheProducer);
        }

        /// <summary>
        /// multiplex -> encoded cache -> disk cache -> (webp transcode) ->
        /// network fetch.
        /// </summary>
        private IProducer<EncodedImage> GetCommonNetworkFetchToEncodedMemorySequence()
        {
            lock (_gate)
            {
                if (_commonNetworkFetchToEncodedMemorySequence == null)
                {
                    IProducer<EncodedImage> inputProducer = 
                        NewEncodedCacheMultiplexToTranscodeSequence(
                            _producerFactory.NewNetworkFetchProducer(_networkFetcher));

                    _commonNetworkFetchToEncodedMemorySequence =
                        ProducerFactory.NewAddImageTransformMetaDataProducer(inputProducer);

                    if (_resizeAndRotateEnabledForNetwork && !_downsampleEnabled)
                    {
                        _commonNetworkFetchToEncodedMemorySequence = 
                            _producerFactory.NewResizeAndRotateProducer(
                                _commonNetworkFetchToEncodedMemorySequence);
                    }
                }

                return _commonNetworkFetchToEncodedMemorySequence;
            }
        }

        /// <summary>
        /// swallow result if prefetch -> bitmap cache get ->
        /// background thread hand-off -> multiplex -> bitmap cache ->
        /// decode -> multiplex -> encoded cache -> disk cache ->
        /// (webp transcode) -> network fetch.
        /// </summary>
        private IProducer<CloseableReference<CloseableImage>> GetNetworkFetchSequence()
        {
            lock (_gate)
            {
                if (_networkFetchSequence == null)
                {
                    _networkFetchSequence =
                        NewBitmapCacheGetToDecodeSequence(GetCommonNetworkFetchToEncodedMemorySequence());
                }

                return _networkFetchSequence;
            }
        }

        /// <summary>
        /// background-thread hand-off -> multiplex -> encoded cache ->
        /// disk cache -> (webp transcode) -> network fetch.
        /// </summary>
        private IProducer<EncodedImage> GetBackgroundNetworkFetchToEncodedMemorySequence()
        {
            lock (_gate)
            {
                if (_backgroundNetworkFetchToEncodedMemorySequence == null)
                {
                    // Use hand-off producer to ensure that we don't do any unnecessary work 
                    // on the UI thread.
                    _backgroundNetworkFetchToEncodedMemorySequence =
                        _producerFactory.NewBackgroundThreadHandoffProducer(
                            GetCommonNetworkFetchToEncodedMemorySequence(),
                            _threadHandoffProducerQueue);
                }

                return _backgroundNetworkFetchToEncodedMemorySequence;
            }
        }

        /// <summary>
        /// encoded cache multiplex -> encoded cache -> local resource fetch.
        /// </summary>
        private IProducer<CloseableReference<IPooledByteBuffer>> GetEncodedLocalResourceFetchSequence()
        {
            lock (_gate)
            {
                if (_encodedLocalResourceFetchSequence == null)
                {
                    LocalResourceFetchProducer localResourceFetchProducer =
                        _producerFactory.NewLocalResourceFetchProducer();

                    _encodedLocalResourceFetchSequence = new RemoveImageTransformMetaDataProducer(
                        NewEncodedCacheMultiplexToTranscodeSequence(localResourceFetchProducer));
                }

                return _encodedLocalResourceFetchSequence;
            }
        }

        /// <summary>
        /// encoded cache multiplex -> encoded cache -> data fetch.
        /// </summary>
        private IProducer<CloseableReference<IPooledByteBuffer>> GetEncodedDataFetchSequence()
        {
            lock (_gate)
            {
                if (_encodedDataFetchSequence == null)
                {
                    DataFetchProducer dataFetchProducer =
                        _producerFactory.NewDataFetchProducer();

                    _encodedDataFetchSequence = new RemoveImageTransformMetaDataProducer(
                        NewEncodedCacheMultiplexToTranscodeSequence(dataFetchProducer));
                }

                return _encodedDataFetchSequence;
            }
        }

        /// <summary>
        /// encoded cache multiplex -> encoded cache -> local asset fetch.
        /// </summary>
        private IProducer<CloseableReference<IPooledByteBuffer>> GetEncodedLocalAssetFetchSequence()
        {
            lock (_gate)
            {
                if (_encodedLocalAssetFetchSequence == null)
                {
                    LocalAssetFetchProducer localAssetFetchProducer =
                        _producerFactory.NewLocalAssetFetchProducer();

                    _encodedLocalAssetFetchSequence = new RemoveImageTransformMetaDataProducer(
                        NewEncodedCacheMultiplexToTranscodeSequence(localAssetFetchProducer));
                }

                return _encodedLocalAssetFetchSequence;
            }
        }

        /// <summary>
        /// encoded cache multiplex -> encoded cache -> local file fetch.
        /// </summary>
        private IProducer<CloseableReference<IPooledByteBuffer>> GetEncodedLocalImageFileFetchSequence()
        {
            lock (_gate)
            {
                if (_encodedLocalImageFileFetchSequence == null)
                {
                    LocalFileFetchProducer localFileFetchProducer =
                        _producerFactory.NewLocalFileFetchProducer();

                    _encodedLocalImageFileFetchSequence = new RemoveImageTransformMetaDataProducer(
                        NewEncodedCacheMultiplexToTranscodeSequence(localFileFetchProducer));
                }

                return _encodedLocalImageFileFetchSequence;
            }
        }

        /// <summary>
        /// encoded cache multiplex -> encoded cache -> FutureAccessList fetch.
        /// </summary>
        private IProducer<CloseableReference<IPooledByteBuffer>> GetEncodedFutureAccessListFetchSequence()
        {
            lock (_gate)
            {
                if (_encodedFutureAccessListFetchSequence == null)
                {
                    FutureAccessListFetchProducer futureAccessListFetchProducer =
                        _producerFactory.NewFutureAccessListFetchProducer();

                    _encodedFutureAccessListFetchSequence = new RemoveImageTransformMetaDataProducer(
                        NewEncodedCacheMultiplexToTranscodeSequence(futureAccessListFetchProducer));
                }

                return _encodedFutureAccessListFetchSequence;
            }
        }

        private IProducer<CloseableReference<IPooledByteBuffer>> GetBasicEncodedImageSequence(
            ImageRequest imageRequest)
        {
            Preconditions.CheckNotNull(imageRequest);

            Uri uri = imageRequest.SourceUri;
            Preconditions.CheckNotNull(uri, "Uri is null.");
            if (UriUtil.IsNetworkUri(uri))
            {
                lock (_gate)
                {
                    if (_encodedImageProducerSequence == null)
                    {
                        _encodedImageProducerSequence = new RemoveImageTransformMetaDataProducer(
                            GetBackgroundNetworkFetchToEncodedMemorySequence());
                    }
                }

                return _encodedImageProducerSequence;
            }
            else if (UriUtil.IsAppDataUri(uri))
            {
                return GetEncodedLocalImageFileFetchSequence();
            }
            else if (UriUtil.IsAppPackageUri(uri))
            {
                return GetEncodedLocalAssetFetchSequence();
            }
            else if (UriUtil.IsAppResourceUri(uri))
            {
                return GetEncodedLocalResourceFetchSequence();
            }
            else if (UriUtil.IsDataUri(uri))
            {
                return GetEncodedDataFetchSequence();
            }
            else if (UriUtil.IsFileUri(uri))
            {
                return GetEncodedLocalImageFileFetchSequence();
            }
            else if (UriUtil.IsFutureAccessListUri(uri))
            {
                return GetEncodedFutureAccessListFetchSequence();
            }
            else
            {
                string uriString = uri.ToString();
                if (uriString.Length > 30)
                {
                    uriString = uriString.Substring(0, 30) + "...";
                }

                throw new Exception("Unsupported uri scheme! Uri is: " + uriString);
            }
        }

        /// <summary>
        /// Bitmap cache get -> thread hand off -> multiplex -> bitmap cache.
        /// </summary>
        /// <param name="inputProducer">
        /// Producer providing the input to the bitmap cache.
        /// </param>
        /// <returns>
        /// Bitmap cache get to bitmap cache sequence.
        /// </returns>
        private IProducer<CloseableReference<CloseableImage>> NewBitmapCacheGetToBitmapCacheSequence(
            IProducer<CloseableReference<CloseableImage>> inputProducer)
        {
            BitmapMemoryCacheProducer bitmapMemoryCacheProducer =
                _producerFactory.NewBitmapMemoryCacheProducer(inputProducer);

            BitmapMemoryCacheKeyMultiplexProducer bitmapKeyMultiplexProducer =
                _producerFactory.NewBitmapMemoryCacheKeyMultiplexProducer(bitmapMemoryCacheProducer);

            ThreadHandoffProducer<CloseableReference<CloseableImage>> threadHandoffProducer =
                _producerFactory.NewBackgroundThreadHandoffProducer(
                    bitmapKeyMultiplexProducer,
                    _threadHandoffProducerQueue);

            return _producerFactory.NewBitmapMemoryCacheGetProducer(threadHandoffProducer);
        }

        /// <summary>
        /// Same as <code>NewBitmapCacheGetToBitmapCacheSequence</code> but
        /// with an extra DecodeProducer.
        /// </summary>
        /// <param name="inputProducer">
        /// Producer providing the input to the decode.
        /// </param>
        /// <returns>
        /// Bitmap cache get to decode sequence.
        /// </returns>
        private IProducer<CloseableReference<CloseableImage>> NewBitmapCacheGetToDecodeSequence(
            IProducer<EncodedImage> inputProducer)
        {
            DecodeProducer decodeProducer = _producerFactory.NewDecodeProducer(inputProducer);
            return NewBitmapCacheGetToBitmapCacheSequence(decodeProducer);
        }

        private IProducer<EncodedImage> NewLocalThumbnailProducer(
            IThumbnailProducer<EncodedImage>[] thumbnailProducers)
        {
            ThumbnailBranchProducer thumbnailBranchProducer =
                _producerFactory.NewThumbnailBranchProducer(thumbnailProducers);

            if (_downsampleEnabled)
            {
                return thumbnailBranchProducer;
            }
            else
            {
                return _producerFactory.NewResizeAndRotateProducer(thumbnailBranchProducer);
            }
        }

        /// <summary>
        /// Branch on separate images
        ///   -> thumbnail resize and rotate -> thumbnail producers as provided
        ///   -> local image resize and rotate -> add meta data producer
        /// </summary>
        /// <param name="inputProducer">
        /// Producer providing the input to add meta data producer.
        /// </param>
        /// <param name="thumbnailProducers">
        /// The thumbnail producers from which to request the image before
        /// falling back to the full image producer sequence.
        /// </param>
        /// <returns>Local transformations sequence.</returns>
        private IProducer<EncodedImage> NewLocalTransformationsSequence(
            IProducer<EncodedImage> inputProducer,
            IThumbnailProducer<EncodedImage>[] thumbnailProducers)
        {
            IProducer<EncodedImage> localImageProducer =
                ProducerFactory.NewAddImageTransformMetaDataProducer(inputProducer);

            if (!_downsampleEnabled)
            {
                localImageProducer = _producerFactory.NewResizeAndRotateProducer(localImageProducer);
            }

            ThrottlingProducer<EncodedImage> localImageThrottlingProducer =
                _producerFactory.NewThrottlingProducer(
                    _throttlingMaxSimultaneousRequests,
                    localImageProducer);

            return ProducerFactory.NewBranchOnSeparateImagesProducer(
                NewLocalThumbnailProducer(thumbnailProducers),
                localImageThrottlingProducer);
        }

        /// <summary>
        /// Creates a new fetch sequence that just needs the source producer.
        /// </summary>
        /// <param name="inputProducer">
        /// The source producer.
        /// </param>
        /// <param name="thumbnailProducers">
        /// The thumbnail producers from which to request the image before
        /// falling back to the full image producer sequence.
        /// </param>
        /// <returns>The new sequence.</returns>
        private IProducer<CloseableReference<CloseableImage>> NewBitmapCacheGetToLocalTransformSequence(
            IProducer<EncodedImage> inputProducer,
            IThumbnailProducer<EncodedImage>[] thumbnailProducers)
        {
            inputProducer = NewEncodedCacheMultiplexToTranscodeSequence(inputProducer);
            IProducer<EncodedImage> inputProducerAfterDecode =
                NewLocalTransformationsSequence(inputProducer, thumbnailProducers);

            return NewBitmapCacheGetToDecodeSequence(inputProducerAfterDecode);
        }

        /// <summary>
        /// Creates a new fetch sequence that just needs the source producer.
        /// </summary>
        /// <param name="inputProducer">The source producer.</param>
        /// <returns>The new sequence.</returns>
        private IProducer<CloseableReference<CloseableImage>> NewBitmapCacheGetToLocalTransformSequence(
            IProducer<EncodedImage> inputProducer)
        {
            IThumbnailProducer<EncodedImage>[] defaultThumbnailProducers = 
                new IThumbnailProducer<EncodedImage>[1];

            defaultThumbnailProducers[0] = _producerFactory.NewLocalExifThumbnailProducer();
            return NewBitmapCacheGetToLocalTransformSequence(inputProducer, defaultThumbnailProducers);
        }

        /// <summary>
        /// bitmap cache get ->
        /// background thread hand-off -> multiplex -> bitmap cache ->
        /// decode -> branch on separate images
        ///   -> exif resize and rotate -> exif thumbnail creation
        ///   -> local image resize and rotate -> add meta data producer
        ///   -> multiplex -> encoded cache -> (webp transcode)
        ///   -> local resource fetch.
        /// </summary>
        private IProducer<CloseableReference<CloseableImage>> GetLocalResourceFetchSequence()
        {
            lock (_gate)
            {
                if (_localResourceFetchSequence == null)
                {
                    LocalResourceFetchProducer localResourceFetchProducer =
                        _producerFactory.NewLocalResourceFetchProducer();

                    _localResourceFetchSequence =
                        NewBitmapCacheGetToLocalTransformSequence(localResourceFetchProducer);
                }

                return _localResourceFetchSequence;
            }
        }

        /// <summary>
        /// bitmap cache get ->
        /// background thread hand-off -> bitmap cache -> decode ->
        /// resize and rotate -> (webp transcode) -> data fetch.
        /// </summary>
        private IProducer<CloseableReference<CloseableImage>> GetDataFetchSequence()
        {
            lock (_gate)
            {
                if (_dataFetchSequence == null)
                {
                    IProducer<EncodedImage> inputProducer = _producerFactory.NewDataFetchProducer();
                    inputProducer = ProducerFactory.NewAddImageTransformMetaDataProducer(inputProducer);
                    if (!_downsampleEnabled)
                    {
                        inputProducer = _producerFactory.NewResizeAndRotateProducer(inputProducer);
                    }

                    _dataFetchSequence = NewBitmapCacheGetToDecodeSequence(inputProducer);
                }

                return _dataFetchSequence;
            }
        }

        /// <summary>
        /// bitmap cache get ->
        /// background thread hand-off -> multiplex ->
        /// bitmap cache -> decode -> branch on separate images
        ///   -> exif resize and rotate -> exif thumbnail creation
        ///   -> local image resize and rotate -> add meta data producer
        ///   -> multiplex -> encoded cache -> (webp transcode)
        ///   -> local asset fetch.
        /// </summary>
        private IProducer<CloseableReference<CloseableImage>> GetLocalAssetFetchSequence()
        {
            lock (_gate)
            {
                if (_localAssetFetchSequence == null)
                {
                    LocalAssetFetchProducer localAssetFetchProducer =
                        _producerFactory.NewLocalAssetFetchProducer();

                    _localAssetFetchSequence =
                        NewBitmapCacheGetToLocalTransformSequence(localAssetFetchProducer);
                }

                return _localAssetFetchSequence;
            }
        }

        /// <summary>
        /// bitmap cache get ->
        /// background thread hand-off -> multiplex -> bitmap cache ->
        /// decode -> branch on separate images
        ///   -> exif resize and rotate -> exif thumbnail creation
        ///   -> local image resize and rotate -> add meta data producer
        ///   -> multiplex -> encoded cache -> (webp transcode)
        ///   -> local file fetch.
        /// </summary>
        private IProducer<CloseableReference<CloseableImage>> GetLocalImageFileFetchSequence()
        {
            lock (_gate)
            {
                if (_localImageFileFetchSequence == null)
                {
                    LocalFileFetchProducer localFileFetchProducer =
                        _producerFactory.NewLocalFileFetchProducer();

                    _localImageFileFetchSequence =
                        NewBitmapCacheGetToLocalTransformSequence(localFileFetchProducer);
                }

                return _localImageFileFetchSequence;
            }
        }

        /// <summary>
        /// Bitmap cache get -> thread hand off -> multiplex ->
        /// bitmap cache -> local video thumbnail
        /// </summary>
        private IProducer<CloseableReference<CloseableImage>> GetLocalVideoFileFetchSequence()
        {
            lock (_gate)
            {
                if (_localVideoFileFetchSequence == null)
                {
                    LocalVideoThumbnailProducer localVideoThumbnailProducer =
                        _producerFactory.NewLocalVideoThumbnailProducer();

                    _localVideoFileFetchSequence =
                        NewBitmapCacheGetToBitmapCacheSequence(localVideoThumbnailProducer);
                }

                return _localVideoFileFetchSequence;
            }
        }

        /// <summary>
        /// bitmap cache get ->
        /// background thread hand-off -> multiplex -> bitmap cache ->
        /// decode -> branch on separate images
        ///   -> exif resize and rotate -> exif thumbnail creation
        ///   -> local image resize and rotate -> add meta data producer
        ///   -> multiplex -> encoded cache -> (webp transcode)
        ///   -> FutureAccessList fetch.
        /// </summary>
        private IProducer<CloseableReference<CloseableImage>> GetFutureAccessListFetchSequence()
        {
            lock (_gate)
            {
                if (_futureAccessListFetchSequence == null)
                {
                    FutureAccessListFetchProducer futureAccessListFetchProducer =
                        _producerFactory.NewFutureAccessListFetchProducer();

                    _futureAccessListFetchSequence =
                        NewBitmapCacheGetToLocalTransformSequence(futureAccessListFetchProducer);
                }

                return _futureAccessListFetchSequence;
            }
        }

        private IProducer<CloseableReference<CloseableImage>> GetBasicDecodedImageSequence(
            ImageRequest imageRequest)
        {
            Preconditions.CheckNotNull(imageRequest);

            Uri uri = imageRequest.SourceUri;
            Preconditions.CheckNotNull(uri, "Uri is null.");
            if (UriUtil.IsNetworkUri(uri))
            {
                return GetNetworkFetchSequence();
            }
            else if (UriUtil.IsAppDataUri(uri))
            {
                if (MediaUtils.IsVideo(MediaUtils.ExtractMime(uri.ToString())))
                {
                    return GetLocalVideoFileFetchSequence();
                }
                else
                {
                    return GetLocalImageFileFetchSequence();
                }
            }
            else if (UriUtil.IsAppPackageUri(uri))
            {
                return GetLocalAssetFetchSequence();
            }
            else if (UriUtil.IsAppResourceUri(uri))
            {
                return GetLocalResourceFetchSequence();
            }
            else if (UriUtil.IsDataUri(uri))
            {
                return GetDataFetchSequence();
            }
            else if (UriUtil.IsFileUri(uri))
            {
                return GetLocalImageFileFetchSequence();
            }
            else if (UriUtil.IsFutureAccessListUri(uri))
            {
                return GetFutureAccessListFetchSequence();
            }
            else
            {
                string uriString = uri.ToString();
                if (uriString.Length > 30)
                {
                    uriString = uriString.Substring(0, 30) + "...";
                }

                throw new Exception("Unsupported uri scheme! Uri is: " + uriString);
            }
        }

        /// <summary>
        /// swallow result producer -> inputProducer
        /// </summary>
        private IProducer<object> GetDecodedImagePrefetchSequence(

            IProducer<CloseableReference<CloseableImage>> inputProducer)
        {
            lock (_gate)
            {
                if (!_closeableImagePrefetchSequences.ContainsKey(inputProducer))
                {
                    SwallowResultProducer<CloseableReference<CloseableImage>> swallowResultProducer =
                        ProducerFactory.NewSwallowResultProducer(inputProducer);

                    _closeableImagePrefetchSequences.Add(inputProducer, swallowResultProducer);
                }

                var producer = default(IProducer<object>);
                _closeableImagePrefetchSequences.TryGetValue(inputProducer, out producer);
                return producer;
            }
        }

        private static void ValidateEncodedImageRequest(ImageRequest imageRequest)
        {
            Preconditions.CheckNotNull(imageRequest);
            Preconditions.CheckArgument(UriUtil.IsNetworkUri(imageRequest.SourceUri));
            Preconditions.CheckArgument(
                imageRequest.LowestPermittedRequestLevel.Value <= RequestLevel.ENCODED_MEMORY_CACHE);
        }
    }
}
