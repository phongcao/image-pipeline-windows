using FBCore.Common.Internal;
using FBCore.Common.Util;

namespace ImagePipeline.Common
{
    /// <summary>
    /// Options for resizing.
    ///
    /// <para />Describes the target bounds for the image (width, height)
    /// in pixels, as well as the downscaling policy to employ.
    /// </summary>
    public class ResizeOptions
    {
        /// <summary>
        /// Target width (in pixels).
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Target height (in pixels).
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Instantiates the <see cref="ResizeOptions"/>.
        /// </summary>
        public ResizeOptions(
            int width,
            int height)
        {
            Preconditions.CheckArgument(width > 0);
            Preconditions.CheckArgument(height > 0);
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Calculates the hash code basing on width and height.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCodeUtil.HashCode(Width, Height);
        }

        /// <summary>
        /// Compares with other ResizeOptions objects.
        /// </summary>
        public override bool Equals(object other)
        {
            if (other == this)
            {
                return true;
            }

            if (other.GetType() != typeof(ResizeOptions))
            {
                return false;
            }

            ResizeOptions that = (ResizeOptions)other;
            return Width == that.Width && Height == that.Height;
        }

        /// <summary>
        /// Provides the custom ToString method.
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}x{1}", Width, Height);
        }
    }
}
