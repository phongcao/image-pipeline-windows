using FBCore.Common.Internal;
using ImageUtils;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Testing
{
    /**
     * Helper class for creating bitmap mocks in tests.
     */
    public class MockBitmapFactory
    {
        public const int DEFAULT_BITMAP_WIDTH = 3;
        public const int DEFAULT_BITMAP_HEIGHT = 4;
        public const int DEFAULT_BITMAP_PIXELS = DEFAULT_BITMAP_WIDTH * DEFAULT_BITMAP_HEIGHT;
        public static readonly int DEFAULT_BITMAP_SIZE = BitmapSize(
            DEFAULT_BITMAP_WIDTH,
            DEFAULT_BITMAP_HEIGHT,
            BitmapPixelFormat.Bgra8);

        public static SoftwareBitmap Create()
        {
            return Create(DEFAULT_BITMAP_WIDTH, DEFAULT_BITMAP_HEIGHT, BitmapPixelFormat.Bgra8);
        }

        public static SoftwareBitmap CreateForSize(int size, BitmapPixelFormat config)
        {
            Preconditions.CheckArgument(size % BitmapUtil.GetPixelSizeForBitmapConfig(config) == 0);
            return Create(1, size / BitmapUtil.GetPixelSizeForBitmapConfig(config), config);
        }

        public static SoftwareBitmap Create(int width, int height, BitmapPixelFormat config)
        {
            Preconditions.CheckArgument(width > 0);
            Preconditions.CheckArgument(height > 0);
            Preconditions.CheckNotNull(config);
            return new SoftwareBitmap(config, width, height);
        }

        public static int BitmapSize(int width, int height, BitmapPixelFormat config)
        {
            return BitmapUtil.GetSizeInByteForBitmap(width, height, config);
        }
    }
}
