using Cache.Common;
using FBCore.Common.Internal;
using FBCore.Common.References;
using ImagePipeline.Bitmaps;
using System;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Request
{
    /// <summary>
    /// Base implementation of <see cref="IPostprocessor"/> interface.
    ///
    /// <para /> Clients should override exactly one of the three provided Process methods.
    /// </summary>
    public abstract class BasePostprocessor : IPostprocessor
    {
        /// <summary>
        /// Returns the name of this postprocessor.
        ///
        /// <para />Used for logging and analytics.
        /// </summary>
        public string Name
        {
            get
            {
                return "Unknown postprocessor";
            }
        }

        /// <summary>
        /// Clients should override this method only if the post-processed bitmap has to be of a different
        /// size than the source bitmap. If the post-processed bitmap is of the same size, clients should
        /// override one of the other two methods.
        ///
        /// <para /> The source bitmap must not be modified as it may be shared by the other clients. The
        /// implementation must create a new bitmap that is safe to be modified and return a reference
        /// to it. Clients should use <code>bitmapFactory</code> to create a new bitmap.
        ///
        /// <param name="sourceBitmap">The source bitmap.</param>
        /// <param name="bitmapFactory">The factory to create a destination bitmap.</param>
        /// @return a reference to the newly created bitmap
        /// </summary>
        public CloseableReference<SoftwareBitmap> Process(
            SoftwareBitmap sourceBitmap,
            PlatformBitmapFactory bitmapFactory)
        {
            CloseableReference<SoftwareBitmap> destBitmapRef =
                bitmapFactory.CreateBitmapInternal(
                    sourceBitmap.PixelWidth,
                    sourceBitmap.PixelHeight,
                    sourceBitmap.BitmapPixelFormat);
            try
            {
                Process(destBitmapRef.Get(), sourceBitmap);
                return CloseableReference<SoftwareBitmap>.CloneOrNull(destBitmapRef);
            }
            finally
            {
                CloseableReference<SoftwareBitmap>.CloseSafely(destBitmapRef);
            }
        }

        /// <summary>
        /// Clients should override this method if the post-processing cannot be done in place. If the
        /// post-processing can be done in place, clients should override the <see cref="Process(SoftwareBitmap)"/>
        /// method.
        ///
        /// <para /> The provided destination bitmap is of the same size as the source bitmap. There are no
        /// guarantees on the initial content of the destination bitmap, so the implementation has to make
        /// sure that it properly populates it.
        ///
        /// <para /> The source bitmap must not be modified as it may be shared by the other clients.
        /// The implementation must use the provided destination bitmap as its output.
        ///
        /// <param name="destBitmap">the destination bitmap to be used as output</param>
        /// <param name="sourceBitmap">the source bitmap to be used as input</param>
        /// </summary>
        public virtual void Process(SoftwareBitmap destBitmap, SoftwareBitmap sourceBitmap)
        {
            Preconditions.CheckArgument(sourceBitmap.BitmapPixelFormat == destBitmap.BitmapPixelFormat);
            Preconditions.CheckArgument(!destBitmap.IsReadOnly);
            Preconditions.CheckArgument(destBitmap.PixelWidth == sourceBitmap.PixelWidth);
            Preconditions.CheckArgument(destBitmap.PixelHeight == sourceBitmap.PixelHeight);
            sourceBitmap.CopyTo(destBitmap);
            Process(destBitmap);
        }

        /// <summary>
        /// Clients should override this method if the post-processing can be done in place.
        ///
        /// <para /> The provided bitmap is a copy of the source bitmap and the implementation is free to
        /// modify it.
        ///
        /// <param name="bitmap">the bitmap to be used both as input and as output</param>
        /// </summary>
        public virtual void Process(SoftwareBitmap bitmap)
        {
        }

        /// <summary>
        /// The default implementation of the CacheKey for a Postprocessor is null
        /// @return The CacheKey to use for caching. Not used if null
        /// </summary>
        public virtual ICacheKey PostprocessorCacheKey
        {
            get
            {
                return null;
            }
        }
    }
}
