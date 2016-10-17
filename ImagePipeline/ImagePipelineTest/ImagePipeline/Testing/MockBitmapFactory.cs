using FBCore.Common.Internal;
using ImageUtils;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Testing
{
    /// <summary>
    /// Helper class for creating bitmap mocks in tests.
    /// </summary>
    class MockBitmapFactory
    {
        /// <summary>
        /// Mock bitmap width
        /// </summary>
        public const int DEFAULT_BITMAP_WIDTH = 3;

        /// <summary>
        /// Mock bitmap height
        /// </summary>
        public const int DEFAULT_BITMAP_HEIGHT = 4;

        /// <summary>
        /// Mock bitmap pixels
        /// </summary>
        public const int DEFAULT_BITMAP_PIXELS = DEFAULT_BITMAP_WIDTH * DEFAULT_BITMAP_HEIGHT;

        /// <summary>
        /// Mock bitmap size
        /// </summary>
        public static readonly int DEFAULT_BITMAP_SIZE = BitmapSize(
            DEFAULT_BITMAP_WIDTH,
            DEFAULT_BITMAP_HEIGHT,
            BitmapPixelFormat.Bgra8);

        /// <summary>
        /// Instantiates the <see cref="SoftwareBitmap"/>.
        /// </summary>
        /// <returns>SoftwareBitmap</returns>
        public static SoftwareBitmap Create()
        {
            return Create(DEFAULT_BITMAP_WIDTH, DEFAULT_BITMAP_HEIGHT, BitmapPixelFormat.Bgra8);
        }

        /// <summary>
        /// Instantiates the <see cref="SoftwareBitmap"/> with provided
        /// </summary>
        /// <param name="size">Size of the bitmap</param>
        /// <param name="config">Bitmap pixel format</param>
        /// <returns></returns>
        public static SoftwareBitmap CreateForSize(int size, BitmapPixelFormat config)
        {
            Preconditions.CheckArgument(size % BitmapUtil.GetPixelSizeForBitmapConfig(config) == 0);
            return Create(1, size / BitmapUtil.GetPixelSizeForBitmapConfig(config), config);
        }

        /// <summary>
        /// Instantiates the <see cref="SoftwareBitmap"/> with provided
        /// </summary>
        /// <param name="width">Width of the bitmap</param>
        /// <param name="height">Height of the bitmap</param>
        /// <param name="config">Bitmap pixel format</param>
        /// <returns></returns>
        public static SoftwareBitmap Create(int width, int height, BitmapPixelFormat config)
        {
            Preconditions.CheckArgument(width > 0);
            Preconditions.CheckArgument(height > 0);
            Preconditions.CheckNotNull(config);
            return new SoftwareBitmap(config, width, height);
        }

        /// <summary>
        /// Gets the bitmap size in bytes with provided
        /// </summary>
        /// <param name="width">Width of the bitmap</param>
        /// <param name="height">Height of the bitmap</param>
        /// <param name="config">Bitmap pixel format</param>
        /// <returns></returns>
        public static int BitmapSize(int width, int height, BitmapPixelFormat config)
        {
            return BitmapUtil.GetSizeInByteForBitmap(width, height, config);
        }
    }
}
