using Cache.Common;
using ImagePipeline.Request;
using System;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// Default implementation of <see cref="ICacheKeyFactory"/>.
    /// </summary>
    public class DefaultCacheKeyFactory : ICacheKeyFactory
    {
        private static readonly object _instanceGate = new object();
        private static DefaultCacheKeyFactory _instance = null;

        /// <summary>
        /// Instantiates the <see cref="DefaultCacheKeyFactory"/>
        /// </summary>
        protected DefaultCacheKeyFactory()
        {
        }

        /// <summary>
        /// Singleton
        /// </summary>
        /// <returns></returns>
        public static DefaultCacheKeyFactory Instance
        {
            get
            {
                lock (_instanceGate)
                {
                    if (_instance == null)
                    {
                        _instance = new DefaultCacheKeyFactory();
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// @return <see cref="ICacheKey"/> for doing bitmap cache lookups in the pipeline.
        /// </summary>
        public ICacheKey GetBitmapCacheKey(ImageRequest request, object callerContext)
        {
            return new BitmapMemoryCacheKey(
                GetCacheKeySourceUri(request.SourceUri).ToString(),
                request.ResizeOptions,
                request.IsAutoRotateEnabled,
                request.ImageDecodeOptions,
                null,
                null,
                callerContext);
        }

        /// <summary>
        /// @return <see cref="ICacheKey"/> for doing post-processed bitmap cache lookups in the pipeline.
        /// </summary>
        public ICacheKey GetPostprocessedBitmapCacheKey(ImageRequest request, object callerContext)
        {
            IPostprocessor postprocessor = request.Postprocessor;
            ICacheKey postprocessorCacheKey;
            string postprocessorName;
            if (postprocessor != null)
            {
                postprocessorCacheKey = postprocessor.PostprocessorCacheKey;
                postprocessorName = postprocessor.GetType().ToString();
            }
            else
            {
                postprocessorCacheKey = null;
                postprocessorName = null;
            }
            return new BitmapMemoryCacheKey(
                GetCacheKeySourceUri(request.SourceUri).ToString(),
                request.ResizeOptions,
                request.IsAutoRotateEnabled,
                request.ImageDecodeOptions,
                postprocessorCacheKey,
                postprocessorName,
                callerContext);
        }

        /// <summary>
        /// <param name="request">Image request</param>
        /// <param name="callerContext">included for optional debugging or logging purposes only</param>
        /// @return <see cref="ICacheKey"/> for doing encoded image lookups in the pipeline.
        /// </summary>
        public ICacheKey GetEncodedCacheKey(ImageRequest request, object callerContext)
        {
            return new SimpleCacheKey(GetCacheKeySourceUri(request.SourceUri).ToString());
        }

        /// <summary>
        /// @return a string that unambiguously indicates the source of the image.
        /// </summary>
        protected Uri GetCacheKeySourceUri(Uri sourceUri)
        {
            return sourceUri;
        }
    }
}
