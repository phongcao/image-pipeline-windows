using FBCore.Common.Internal;
using FBCore.Common.Util;
using ImagePipeline.Common;
using ImagePipeline.Listener;
using System;

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

        internal bool AutoRotateEnabled { get; private set; } = false;

        internal ResizeOptions ResizeOptions { get; private set; } = null;

        internal ImageDecodeOptions ImageDecodeOptions { get; private set; } = 
            ImageDecodeOptions.Defaults;

        internal CacheChoice CacheChoice { get; private set; } = CacheChoice.DEFAULT;

        internal bool ProgressiveRenderingEnabled { get; private set; } = false;

        internal bool LocalThumbnailPreviewsEnabled { get; private set; } = false;

        internal int Priority { get; private set; } = Common.Priority.HIGH;

        internal IPostprocessor Postprocessor { get; private set; } = null;

        internal bool DiskCacheEnabled
        {
            get
            {
                return _diskCacheEnabled && UriUtil.IsNetworkUri(SourceUri);
            }
        }

        internal IRequestListener RequestListener;

        /// <summary>
        /// Creates a new request builder instance. The setting will be done according to the source type.
        /// <param name="uri">the uri to fetch</param>
        /// @return a new request builder instance
        /// </summary>
        public static ImageRequestBuilder NewBuilderWithSource(Uri uri)
        {
            return new ImageRequestBuilder().SetSource(uri);
        }

        /// <summary>
        /// Creates a new request builder instance with the same parameters as the imageRequest passed in.
        /// <param name="imageRequest">the ImageRequest from where to copy the parameters to the builder.</param>
        /// @return a new request builder instance
        /// </summary>
        public static ImageRequestBuilder FromRequest(ImageRequest imageRequest)
        {
            return NewBuilderWithSource(imageRequest.SourceUri)
                .SetAutoRotateEnabled(imageRequest.AutoRotateEnabled)
                .SetImageDecodeOptions(imageRequest.ImageDecodeOptions)
                .SetCacheChoice(imageRequest.CacheChoice)
                .SetLocalThumbnailPreviewsEnabled(imageRequest.LocalThumbnailPreviewsEnabled)
                .SetLowestPermittedRequestLevel(imageRequest.LowestPermittedRequestLevel)
                .SetPostprocessor(imageRequest.Postprocessor)
                .SetProgressiveRenderingEnabled(imageRequest.ProgressiveRenderingEnabled)
                .SetRequestPriority(imageRequest.Priority)
                .SetResizeOptions(imageRequest.ResizeOptions)
                .SetRequestListener(imageRequest.RequestListener);
        }

        private ImageRequestBuilder()
        {
        }

        /// <summary>
        /// Sets the source uri (both network and local uris are supported).
        /// Note: this will enable disk caching for network sources, and disable it for local sources.
        /// <param name="uri">the uri to fetch the image from</param>
        /// @return the updated builder instance
        /// </summary>
        public ImageRequestBuilder SetSource(Uri uri)
        {
            Preconditions.CheckNotNull(uri);

            SourceUri = uri;
            return this;
        }

        /// <summary>
        /// Sets the lowest level that is permitted to request the image from.
        /// <param name="requestLevel">the lowest request level that is allowed</param>
        /// @return the updated builder instance
        /// </summary>
        public ImageRequestBuilder SetLowestPermittedRequestLevel(RequestLevel requestLevel)
        {
            LowestPermittedRequestLevel = requestLevel;
            return this;
        }

        /// <summary>
        /// Enables or disables auto-rotate for the image in case image has orientation.
        /// @return the updated builder instance
        /// <param name="enabled"></param>
        /// </summary>
        public ImageRequestBuilder SetAutoRotateEnabled(bool enabled)
        {
            AutoRotateEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets resize options in case resize should be performed.
        /// <param name="resizeOptions">resize options</param>
        /// @return the modified builder instance
        /// </summary>
        public ImageRequestBuilder SetResizeOptions(ResizeOptions resizeOptions)
        {
            ResizeOptions = resizeOptions;
            return this;
        }

        /// <summary>
        /// Sets image decode options
        /// </summary>
        /// <param name="imageDecodeOptions"></param>
        /// <returns></returns>
        public ImageRequestBuilder SetImageDecodeOptions(ImageDecodeOptions imageDecodeOptions)
        {
            ImageDecodeOptions = imageDecodeOptions;
            return this;
        }

        /// <summary>
        /// Sets the cache option. Pipeline might use different caches and eviction policies for each
        /// image type.
        /// <param name="cacheChoice">the cache choice to set</param>
        /// @return the modified builder instance
        /// </summary>
        public ImageRequestBuilder SetCacheChoice(CacheChoice cacheChoice)
        {
            CacheChoice = cacheChoice;
            return this;
        }

        /// <summary>
        /// Enables or disables progressive rendering.
        /// <param name="enabled"></param>
        /// @return the modified builder instance
        /// </summary>
        public ImageRequestBuilder SetProgressiveRenderingEnabled(bool enabled)
        {
            ProgressiveRenderingEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Enables or disables the use of local thumbnails as previews.
        /// <param name="enabled"></param>
        /// @return the modified builder instance
        /// </summary>
        public ImageRequestBuilder SetLocalThumbnailPreviewsEnabled(bool enabled)
        {
            LocalThumbnailPreviewsEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Disables disk cache for this request, regardless where the image will come from.
        /// </summary>
        /// <returns></returns>
        public ImageRequestBuilder DisableDiskCache()
        {
            _diskCacheEnabled = false;
            return this;
        }

        /// <summary>
        /// Set priority for the request.
        /// <param name="requestPriority"></param>
        /// @return the modified builder instance
        /// </summary>
        public ImageRequestBuilder SetRequestPriority(int requestPriority)
        {
            Priority = requestPriority;
            return this;
        }

        /// <summary>
        /// Sets the postprocessor.
        /// <param name="postprocessor">postprocessor to postprocess the output bitmap with.</param>
        /// @return the modified builder instance
        /// </summary>
        public ImageRequestBuilder SetPostprocessor(IPostprocessor postprocessor)
        {
            Postprocessor = postprocessor;
            return this;
        }

        /// <summary>
        /// Sets a request listener to use for just this image request
        /// </summary>
        public ImageRequestBuilder SetRequestListener(IRequestListener requestListener)
        {
            RequestListener = requestListener;
            return this;
        }

        /// <summary>
        /// Builds the Request.
        /// @return a valid image request
        /// </summary>
        public ImageRequest Build()
        {
            Validate();
            return new ImageRequest(this);
        }

        ///  An exception class for builder methods. 
        public class BuilderException : Exception
        {
            /// <summary>
            /// Instantiates the <see cref="BuilderException"/>
            /// </summary>
            /// <param name="message"></param>
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
