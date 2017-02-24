using FBCore.Common.Internal;
using ImageFormatUtils;
using ImagePipeline.Common;
using ImagePipeline.Image;
using ImagePipeline.Request;
using System;
using System.Diagnostics;

namespace ImagePipeline.Producers
{
    /// <summary>
    /// Downsample util.
    /// </summary>
    public static class DownsampleUtil
    {
        private static readonly float MAX_BITMAP_SIZE = 2048f;
        private static readonly float INTERVAL_ROUNDING = 1.0f / 3;
        private static readonly int DEFAULT_SAMPLE_SIZE = 1;

        /// <summary>
        /// Get the factor between the dimensions of the encodedImage
        /// (actual image) and the ones of the imageRequest
        /// (requested size).
        /// </summary>
        /// <param name="imageRequest">
        /// The request containing the requested dimensions.
        /// </param>
        /// <param name="encodedImage">
        /// The encoded image with the actual dimensions.
        /// </param>
        public static int DetermineSampleSize(ImageRequest imageRequest, EncodedImage encodedImage)
        {
            if (!EncodedImage.IsMetaDataAvailable(encodedImage))
            {
                return DEFAULT_SAMPLE_SIZE;
            }

            float ratio = DetermineDownsampleRatio(imageRequest, encodedImage);
            int sampleSize;
            if (encodedImage.Format == ImageFormat.JPEG)
            {
                sampleSize = RatioToSampleSizeJPEG(ratio);
            }
            else
            {
                sampleSize = RatioToSampleSize(ratio);
            }

            // Check the case when the dimension of the downsampled image
            // is still larger than the max possible dimension for an image.
            int maxDimension = Math.Max(encodedImage.Height, encodedImage.Width);
            while (maxDimension / sampleSize > MAX_BITMAP_SIZE)
            {
                if (encodedImage.Format == ImageFormat.JPEG)
                {
                    sampleSize *= 2;
                }
                else
                {
                    sampleSize++;
                }
            }

            return sampleSize;
        }

        internal static float DetermineDownsampleRatio(
            ImageRequest imageRequest, EncodedImage encodedImage)
        {
            Preconditions.CheckArgument(EncodedImage.IsMetaDataAvailable(encodedImage));
            ResizeOptions resizeOptions = imageRequest.ResizeOptions;
            if (resizeOptions == null || resizeOptions.Height <= 0 || resizeOptions.Width <= 0 || 
                encodedImage.Width == 0 || encodedImage.Height == 0)
            {
                return 1.0f;
            }

            int rotationAngle = GetRotationAngle(imageRequest, encodedImage);
            bool swapDimensions = rotationAngle == 90 || rotationAngle == 270;
            int widthAfterRotation = swapDimensions ?
                    encodedImage.Height : encodedImage.Width;

            int heightAfterRotation = swapDimensions ?
                    encodedImage.Width : encodedImage.Height;

            float widthRatio = ((float)resizeOptions.Width) / widthAfterRotation;
            float heightRatio = ((float)resizeOptions.Height) / heightAfterRotation;
            float ratio = Math.Max(widthRatio, heightRatio);
            Debug.Write(string.Format(
                "Downsample - Specified size: {0}x{1}, image size: {2}x{3} ratio: {4} x {5}, ratio: {6} for {7}",
                resizeOptions.Width,
                resizeOptions.Height,
                widthAfterRotation,
                heightAfterRotation,
                widthRatio,
                heightRatio,
                ratio,
                imageRequest.SourceUri.ToString()));

            return ratio;
        }

        internal static int RatioToSampleSize(float ratio)
        {
            if (ratio > 0.5f + 0.5f * INTERVAL_ROUNDING)
            {
                return 1; // should have resized
            }

            int sampleSize = 2;
            while (true)
            {
                double intervalLength = 1.0 / (Math.Pow(sampleSize, 2) - sampleSize);
                double compare = (1.0 / sampleSize) + (intervalLength * INTERVAL_ROUNDING);
                if (compare <= ratio)
                {
                    return sampleSize - 1;
                }

                sampleSize++;
            }
        }

        internal static int RatioToSampleSizeJPEG(float ratio)
        {
            if (ratio > 0.5f + 0.5f * INTERVAL_ROUNDING)
            {
                return 1; // should have resized
            }

            int sampleSize = 2;

            while (true)
            {
                double intervalLength = 1.0 / (2 * sampleSize);
                double compare = (1.0 / (2 * sampleSize)) + (intervalLength * INTERVAL_ROUNDING);
                if (compare <= ratio)
                {
                    return sampleSize;
                }

                sampleSize *= 2;
            }
        }

        private static int GetRotationAngle(ImageRequest imageRequest, EncodedImage encodedImage)
        {
            if (!imageRequest.IsAutoRotateEnabled)
            {
                return 0;
            }

            int rotationAngle = encodedImage.RotationAngle;
            Preconditions.CheckArgument(rotationAngle == 0 || rotationAngle == 90 || 
                rotationAngle == 180 || rotationAngle == 270);

            return rotationAngle;
        }

        internal static int RoundToPowerOfTwo(int sampleSize)
        {
            int compare = 1;
            while (true)
            {
                if (compare >= sampleSize)
                {
                    return compare;
                }

                compare *= 2;
            }
        }
    }
}
