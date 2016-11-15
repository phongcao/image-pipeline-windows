using FBCore.Common.Internal;
using FBCore.Common.Util;

namespace ImagePipeline.Common
{
    /// <summary>
    /// Options for resizing.
    ///
    /// <para /> Describes the target bounds for the image (width, height) in pixels, as well as the
    /// downscaling policy to employ.
    /// </summary>
    public class ResizeOptions
    {
        /// <summary>
        /// Target width (in pixels)
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Target height (in pixels)
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Instantiates the <see cref="ResizeOptions"/>
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
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
        /// Gets the hash code for width, height
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return HashCodeUtil.HashCode(Width, Height);
        }
    }
}
