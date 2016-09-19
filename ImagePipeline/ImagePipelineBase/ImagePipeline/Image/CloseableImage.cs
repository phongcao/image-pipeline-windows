using System;
using System.Diagnostics;

namespace ImagePipeline.Image
{
    /// <summary>
    /// A simple wrapper around an image that implements <see cref="IDisposable"/>
    /// </summary>
    public abstract class CloseableImage : IDisposable, IImageInfo
    {
        /// <summary>
        /// Returns size in bytes of the bitmap(s)
        /// </summary>
        public abstract int SizeInBytes { get; }

        /// <summary>
        /// Returns width of the bitmap(s)
        /// </summary>
        public abstract int Width { get; }

        /// <summary>
        /// Returns height of the bitmap(s)
        /// </summary>
        public abstract int Height { get; }

        /// <summary>
        /// Closes this instance and releases the resources.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Returns whether this instance is closed.
        /// </summary>
        public abstract bool IsClosed { get; }

        /// <summary>
        /// Returns quality information for the image.
        /// <para /> Image classes that can contain intermediate results should override this as appropriate.
        /// </summary>
        public virtual IQualityInfo QualityInfo
        {
            get
            {
                return ImmutableQualityInfo.FULL_QUALITY;
            }
        }

        /// <summary>
        /// Whether or not this image contains state for a particular view of the image (for example,
        /// the image for an animated GIF might contain the current frame being viewed). This means
        /// that the image should not be stored in the bitmap cache.
        /// </summary>
        public bool IsStateful
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Ensures that the underlying resources are always properly released.
        /// </summary>
        ~CloseableImage()
        {
            if (IsClosed)
            {
                return;
            }

            Debug.WriteLine($"Finalize: { GetType().Name } { GetHashCode() } still open.");

            try
            {
                Dispose();
            }
            finally
            {
                // Do nothing
            }
        }
    }
}
