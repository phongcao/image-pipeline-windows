using Cache.Common;
using FBCore.Common.References;
using ImagePipeline.Bitmaps;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Request
{
    /// <summary>
    /// Use an instance of this class to perform post-process operations on a bitmap.
    /// </summary>
    public interface IPostprocessor
    {
        /// <summary>
        /// Called by the pipeline after completing other steps.
        ///
        /// <param name="sourceBitmap">The source bitmap.</param>
        /// <param name="bitmapFactory">The factory to create a destination bitmap.</param>
        ///
        /// <para /> The Postprocessor must not modify the source bitmap as it may be shared by the other
        /// clients. The implementation must create a new bitmap that is safe to be modified and return a
        /// reference to it. To create a bitmap, use the provided <code>bitmapFactory</code>.
        /// </summary>
        CloseableReference<SoftwareBitmap> Process(
            SoftwareBitmap sourceBitmap, 
            PlatformBitmapFactory bitmapFactory);

        /// <summary>
        /// Returns the name of this postprocessor.
        ///
        /// <para />Used for logging and analytics.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Implement this method in order to cache the result of a postprocessor in the bitmap cache
        /// along with the unmodified image.
        /// <para />When reading from memory cache, there will be a hit only if the cache's value for this key
        /// matches that of the request.
        /// <para />Each postprocessor class is only allowed one entry in the cache. When <i>writing</i> to
        /// memory cache, this key is not considered and any image for this request with the same
        /// postprocessor class will be overwritten.
        /// @return The CacheKey to use for the result of this postprocessor
        /// </summary>
        ICacheKey GetPostprocessorCacheKey();
    }
}
