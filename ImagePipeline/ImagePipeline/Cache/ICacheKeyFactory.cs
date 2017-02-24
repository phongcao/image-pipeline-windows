using ImagePipeline.Request;
using Cache.Common;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// Factory methods for creating cache keys for the pipeline.
    /// </summary>
    public interface ICacheKeyFactory
    {
        /// <summary>
        /// Gets the bitmap cache key.
        /// </summary>
        /// <returns>
        /// <see cref="ICacheKey" /> for doing bitmap cache lookups
        /// in the pipeline.
        /// </returns>
        ICacheKey GetBitmapCacheKey(ImageRequest request, object callerContext);

        /// <summary>
        /// Gets the post-processed bitmap cache key.
        /// </summary>
        /// <returns>
        /// <see cref="ICacheKey" /> for doing post-processed bitmap cache
        /// lookups in the pipeline.
        /// </returns>
        ICacheKey GetPostprocessedBitmapCacheKey(ImageRequest request, object callerContext);

        /// <summary>
        /// Gets the encoded cache key.
        /// </summary>
        /// <param name="request">Image request.</param>
        /// <param name="callerContext">
        /// Included for optional debugging or logging purposes only.
        /// </param>
        /// <returns>
        /// <see cref="ICacheKey"/> for doing encoded image lookups
        /// in the pipeline.
        /// </returns>
        ICacheKey GetEncodedCacheKey(ImageRequest request, object callerContext);
    }
}
