using ImagePipeline.Common;
using ImagePipeline.Image;
using ImageUtils;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Utility class to consistently check whether a given thumbnail size will
    /// be sufficient for a given request with ResizeOptions.
    /// </summary>
    public static class ThumbnailSizeChecker
    {
        /// <summary>
        /// The ratio between the requested size and the minimum thumbnail size 
        /// which will be considered big enough. This will allow a thumbnail
        /// which is actually 75% of the requested size to be used and scaled up.
        /// </summary>
        public const float ACCEPTABLE_REQUESTED_TO_ACTUAL_SIZE_RATIO = 4.0f / 3;

        private const int ROTATED_90_DEGREES_CLOCKWISE = 90;
        private const int ROTATED_90_DEGREES_COUNTER_CLOCKWISE = 270;

        /// <summary>
        /// Checks whether the producer may be able to produce images of the 
        /// specified size. This makes no promise about being able to produce 
        /// images for a particular source, only generally being able to produce 
        /// output of the desired resolution.
        /// </summary>
        /// <param name="width">The desired width.</param>
        /// <param name="height">The desired height.</param>
        /// <param name="resizeOptions">The resize options.</param>
        /// <returns>true if the producer can meet these needs.</returns>
        public static bool IsImageBigEnough(int width, int height, ResizeOptions resizeOptions)
        {
            if (resizeOptions == null)
            {
                return GetAcceptableSize(width) >= BitmapUtil.MAX_BITMAP_SIZE &&
                       GetAcceptableSize(height) >= (int)BitmapUtil.MAX_BITMAP_SIZE;
            }
            else
            {
                return GetAcceptableSize(width) >= resizeOptions.Width &&
                       GetAcceptableSize(height) >= resizeOptions.Height;
            }
        }

        /// <summary>
        /// Checks if the image is big enough.
        /// </summary>
        public static bool IsImageBigEnough(EncodedImage encodedImage, ResizeOptions resizeOptions)
        {
            if (encodedImage == null)
            {
                return false;
            }

            switch (encodedImage.RotationAngle)
            {
                case ROTATED_90_DEGREES_CLOCKWISE:
                case ROTATED_90_DEGREES_COUNTER_CLOCKWISE:
                    // Swap width and height when checking size as this will be rotated
                    return IsImageBigEnough(encodedImage.Height, encodedImage.Width, resizeOptions);
                default:
                    return IsImageBigEnough(encodedImage.Width, encodedImage.Height, resizeOptions);
            }
        }

        /// <summary>
        /// Calculates the accepted size.
        /// </summary>
        public static int GetAcceptableSize(int size)
        {
            return (int)(size * ACCEPTABLE_REQUESTED_TO_ACTUAL_SIZE_RATIO);
        }
    }
}
