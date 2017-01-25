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
        /// @return <see cref="ICacheKey"/> for doing bitmap cache lookups in the pipeline.
        /// </summary>
        ICacheKey GetBitmapCacheKey(ImageRequest request, object callerContext);

        /// <summary>
        /// @return <see cref="ICacheKey"/> for doing post-processed bitmap cache lookups in the pipeline.
        /// </summary>
        ICacheKey GetPostprocessedBitmapCacheKey(ImageRequest request, object callerContext);

        /// <summary>
        /// <param name="request">Image request</param>
        /// <param name="callerContext">included for optional debugging or logging purposes only</param>
        /// @return <see cref="ICacheKey"/> for doing encoded image lookups in the pipeline.
        /// </summary>
        ICacheKey GetEncodedCacheKey(ImageRequest request, object callerContext);
    }
}
