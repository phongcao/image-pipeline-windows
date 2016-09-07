using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Imaging;

namespace ImageUtils
{
    public static class BitmapUtil
    {
        private const int DECODE_BUFFER_SIZE = 16 * 1024;
        private const int POOL_SIZE = 12;
        //private static final Pools.SynchronizedPool<ByteBuffer> DECODE_BUFFERS = new Pools.SynchronizedPool<>(POOL_SIZE);

        /**
         * Bytes per pixel definitions
         */
        public const int RGBA16_BYTES_PER_PIXEL = 8;
        public const int RGBA8_BYTES_PER_PIXEL = 4;
        public const int BGRA8_BYTES_PER_PIXEL = 4;
        public const int GRAY16_BYTES_PER_PIXEL = 2;
        public const int GRAY8_BYTES_PER_PIXEL = 1;

        public const float MAX_BITMAP_SIZE = 2048f;

        public static unsafe uint GetAllocationByteCount(SoftwareBitmap bitmap)
        {
            uint capacity = default(int);

            using (BitmapBuffer buffer = bitmap.LockBuffer(BitmapBufferAccessMode.Read))
            {
                using (var reference = buffer.CreateReference())
                {
                    byte* dataInBytes;
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);
                }
            }

            return capacity;
        }

        /**
         * Returns the amount of bytes used by a pixel in a specific
         * {@link android.graphics.Bitmap.Config}
         * @param bitmapConfig the {@link android.graphics.Bitmap.Config} for which the size in byte
         * will be returned
         * @return
         */
        public static int GetPixelSizeForBitmapConfig(BitmapPixelFormat bitmapConfig)
        {
            switch (bitmapConfig)
            {
                case BitmapPixelFormat.Rgba16:
                    return RGBA16_BYTES_PER_PIXEL;

                case BitmapPixelFormat.Rgba8:
                case BitmapPixelFormat.Bgra8:
                    return RGBA8_BYTES_PER_PIXEL;

                case BitmapPixelFormat.Gray16:
                    return GRAY16_BYTES_PER_PIXEL;

                case BitmapPixelFormat.Gray8:
                    return GRAY8_BYTES_PER_PIXEL;
            }

            throw new InvalidOperationException("The provided Bitmap.Config is not supported");
        }

        /**
         * Returns the size in byte of an image with specific size
         * and {@link android.graphics.Bitmap.Config}
         * @param width the width of the image
         * @param height the height of the image
         * @param bitmapConfig the {@link android.graphics.Bitmap.Config} for which the size in byte
         * will be returned
         * @return
         */
        public static int GetSizeInByteForBitmap(int width, int height, BitmapPixelFormat bitmapConfig)
        {
            return width * height * GetPixelSizeForBitmapConfig(bitmapConfig);
        }
    }

    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}
