using FBCore.Common.Internal;
using System.Runtime.InteropServices.ComTypes;

namespace ImagePipeline.NativeCode
{
    /// <summary>
    /// Helper methods for modifying jpeg images.
    /// </summary>
    public class JpegTranscoder
    {
        /// <summary>
        /// Min quality.
        /// </summary>
        public const int MIN_QUALITY = 0;

        /// <summary>
        /// Max quality.
        /// </summary>
        public const int MAX_QUALITY = 100;

        /// <summary>
        /// Min scale numerator.
        /// </summary>
        public const int MIN_SCALE_NUMERATOR = 1;

        /// <summary>
        /// Max scale numerator.
        /// </summary>
        public const int MAX_SCALE_NUMERATOR = 16;

        /// <summary>
        /// Scale denominator.
        /// </summary>
        public const int SCALE_DENOMINATOR = 8;

        /// <summary>
        /// Checks if the rotation angle is allowed to transcode.
        /// </summary>
        /// <param name="degrees">The angle degrees.</param>
        /// <returns>
        /// true if and only if given number of degrees is allowed rotation angle,
        /// that is it is equal to 0, 90, 180 or 270.
        /// </returns>
        public static bool IsRotationAngleAllowed(int degrees)
        {
            return (degrees >= 0) && (degrees <= 270) && (degrees % 90 == 0);
        }

        /// <summary>
        /// Downscales and rotates jpeg image.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <param name="outputStream">The output stream.</param>
        /// <param name="rotationAngle">0, 90, 180 or 270.</param>
        /// <param name="scaleNumerator">
        /// 1 - 16, image will be scaled using scaleNumerator/8 factor.
        /// </param>
        /// <param name="quality">1 - 100.</param>
        public static void TranscodeJpeg(
            IStream inputStream,
            IStream outputStream,
            int rotationAngle,
            int scaleNumerator,
            int quality)
        {
            Preconditions.CheckArgument(scaleNumerator >= MIN_SCALE_NUMERATOR);
            Preconditions.CheckArgument(scaleNumerator <= MAX_SCALE_NUMERATOR);
            Preconditions.CheckArgument(quality >= MIN_QUALITY);
            Preconditions.CheckArgument(quality <= MAX_QUALITY);
            Preconditions.CheckArgument(IsRotationAngleAllowed(rotationAngle));
            Preconditions.CheckArgument(
                scaleNumerator != SCALE_DENOMINATOR || rotationAngle != 0,
                "no transformation requested");

            NativeMethods.NativeTranscodeJpeg(
                Preconditions.CheckNotNull(inputStream),
                Preconditions.CheckNotNull(outputStream),
                rotationAngle,
                scaleNumerator,
                quality);
        }
    }
}
