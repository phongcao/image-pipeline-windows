using FBCore.Common.Internal;
using FBCore.Common.Util;
using ImagePipeline.Common;
using ImagePipeline.Listener;
using System;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Request
{
    /// <summary>
    /// Builder class for <see cref="ImageRequest"/>s.
    /// </summary>
    public class ImageRequestBuilder
    {
        private bool _diskCacheEnabled = true;

        internal Uri SourceUri { get; private set; } = null;

        internal RequestLevel LowestPermittedRequestLevel { get; private set; } = 
            new RequestLevel(RequestLevel.FULL_FETCH);

        internal bool IsAutoRotateEnabled { get; private set; } = false;

        internal ResizeOptions ResizeOptions { get; private set; } = null;

        internal ImageDecodeOptions ImageDecodeOptions { get; private set; } = 
            ImageDecodeOptions.Defaults;

        internal CacheChoice CacheChoice { get; private set; } = CacheChoice.DEFAULT;

        internal bool IsProgressiveRenderingEnabled { get; private set; } = false;

        internal bool IsLocalThumbnailPreviewsEnabled { get; private set; } = false;

        internal int Priority { get; private set; } = Common.Priority.HIGH;

        internal IPostprocessor Postprocessor { get; private set; } = null;

        internal bool IsDiskCacheEnabled
        {
            get
            {
                return _diskCacheEnabled && UriUtil.IsNetworkUri(SourceUri);
            }
        }

        internal IRequestListener RequestListener;

        /// <summary>
        /// Creates a new request builder instance.
        /// The setting will be done according to the source type.
        /// </summary>
        /// <param name="uri">The uri to fetch.</param>
        /// <returns>A new request builder instance.</returns>
        public static ImageRequestBuilder NewBuilderWithSource(Uri uri)
        {
            return new ImageRequestBuilder().SetSource(uri);
        }

        /// <summary>
        /// Creates a new request builder instance with the same parameters as
        /// the imageRequest passed in.
        /// <param name="imageRequest">
        /// The ImageRequest from where to copy the parameters to the builder.
        /// </param>
        /// <returns>A new request builder instance.</returns>
        /// </summary>
        public static ImageRequestBuilder FromRequest(ImageRequest imageRequest)
        {
            return NewBuilderWithSource(imageRequest.SourceUri)
                .SetAutoRotateEnabled(imageRequest.IsAutoRotateEnabled)
                .SetImageDecodeOptions(imageRequest.ImageDecodeOptions)
                .SetCacheChoice(imageRequest.CacheChoice)
                .SetLocalThumbnailPreviewsEnabled(imageRequest.IsLocalThumbnailPreviewsEnabled)
                .SetLowestPermittedRequestLevel(imageRequest.LowestPermittedRequestLevel)
                .SetPostprocessor(imageRequest.Postprocessor)
                .SetProgressiveRenderingEnabled(imageRequest.IsProgressiveRenderingEnabled)
                .SetRequestPriority(imageRequest.Priority)
                .SetResizeOptions(imageRequest.ResizeOptions)
                .SetRequestListener(imageRequest.RequestListener);
        }

        private ImageRequestBuilder()
        {
        }

        /// <summary>
        /// Sets the source uri (both network and local uris are supported).
        /// Note: this will enable disk caching for network sources, and
        /// disable it for local sources.
        /// </summary>
        /// <param name="uri">The uri to fetch the image from.</param>
        /// <returns>The updated builder instance.</returns>
        public ImageRequestBuilder SetSource(Uri uri)
        {
            Preconditions.CheckNotNull(uri);

            SourceUri = uri;
            return this;
        }

        /// <summary>
        /// Sets the lowest level that is permitted to request the image from.
        /// </summary>
        /// <param name="requestLevel">
        /// The lowest request level that is allowed.
        /// </param>
        /// <returns>The updated builder instance.</returns>
        public ImageRequestBuilder SetLowestPermittedRequestLevel(RequestLevel requestLevel)
        {
            LowestPermittedRequestLevel = requestLevel;
            return this;
        }

        /// <summary>
        /// Enables or disables auto-rotate for the image in case image
        /// has orientation.
        /// </summary>
        /// <returns>The updated builder instance.</returns>
        public ImageRequestBuilder SetAutoRotateEnabled(bool enabled)
        {
            IsAutoRotateEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets resize options in case resize should be performed.
        /// </summary>
        /// <param name="resizeOptions">Resize options.</param>
        /// <returns>The modified builder instance.</returns>
        public ImageRequestBuilder SetResizeOptions(ResizeOptions resizeOptions)
        {
            ResizeOptions = resizeOptions;
            return this;
        }

        /// <summary>
        /// Sets image decode options.
        /// </summary>
        public ImageRequestBuilder SetImageDecodeOptions(ImageDecodeOptions imageDecodeOptions)
        {
            ImageDecodeOptions = imageDecodeOptions;
            return this;
        }

        /// <summary>
        /// Sets the cache option.
        /// Pipeline might use different caches and eviction policies for
        /// each image type.
        /// </summary>
        /// <param name="cacheChoice">The cache choice to set.</param>
        /// <returns>The modified builder instance.</returns>
        public ImageRequestBuilder SetCacheChoice(CacheChoice cacheChoice)
        {
            CacheChoice = cacheChoice;
            return this;
        }

        /// <summary>
        /// Enables or disables progressive rendering.
        /// </summary>
        /// <returns>The modified builder instance.</returns>
        public ImageRequestBuilder SetProgressiveRenderingEnabled(bool enabled)
        {
            IsProgressiveRenderingEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Enables or disables the use of local thumbnails as previews.
        /// </summary>
        /// <returns>The modified builder instance.</returns>
        public ImageRequestBuilder SetLocalThumbnailPreviewsEnabled(bool enabled)
        {
            IsLocalThumbnailPreviewsEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Disables disk cache for this request, regardless where the image
        /// will come from.
        /// </summary>
        public ImageRequestBuilder DisableDiskCache()
        {
            _diskCacheEnabled = false;
            return this;
        }

        /// <summary>
        /// Set priority for the request.
        /// </summary>
        /// <param name="requestPriority">The request priority.</param>
        /// <returns>The modified builder instance.</returns>
        public ImageRequestBuilder SetRequestPriority(int requestPriority)
        {
            Priority = requestPriority;
            return this;
        }

        /// <summary>
        /// Sets the postprocessor.
        /// <param name="postprocessor">
        /// IPostprocessor to postprocess the output bitmap with.
        /// </param>
        /// <returns>The modified builder instance.</returns>
        /// </summary>
        public ImageRequestBuilder SetPostprocessor(IPostprocessor postprocessor)
        {
            Postprocessor = postprocessor;
            return this;
        }

        /// <summary>
        /// Sets the postprocessor.
        /// <param name="postprocessor">
        /// IPostprocessor to postprocess the output bitmap with.
        /// </param>
        /// <returns>The modified builder instance.</returns>
        /// </summary>
        public ImageRequestBuilder SetPostprocessor(
            Action<byte[], int, int, BitmapPixelFormat, BitmapAlphaMode> postprocessor)
        {
            Postprocessor = new BasePostprocessorImpl(postprocessor);
            return this;
        }

        /// <summary>
        /// Sets a request listener to use for just this image request.
        /// </summary>
        public ImageRequestBuilder SetRequestListener(IRequestListener requestListener)
        {
            RequestListener = requestListener;
            return this;
        }

        /// <summary>
        /// Builds the Request.
        /// </summary>
        /// <returns>A valid image request.</returns>
        public ImageRequest Build()
        {
            Validate();
            return new ImageRequest(this);
        }

        ///  An exception class for builder methods.
        public class BuilderException : Exception
        {
            /// <summary>
            /// Instantiates the <see cref="BuilderException"/>.
            /// </summary>
            public BuilderException(string message) : base($"Invalid request builder: { message }")
            {
            }
        }

        /// <summary>
        /// Performs validation.
        /// </summary>
        protected void Validate()
        {
            // Make sure that the source uri is set correctly.
            if (SourceUri == null)
            {
                throw new BuilderException("Source must be set!");
            }
        }
    }
}
