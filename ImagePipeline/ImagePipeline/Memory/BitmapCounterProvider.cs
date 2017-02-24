using System;
using Windows.System;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Allocates the bitmap counter basing on the memory constraints.
    /// </summary>
    public class BitmapCounterProvider
    {
        private const long KB = 1024;
        private const long MB = 1024 * KB;

        /// <summary>
        /// Our bitmaps live in native memory.
        ///
        /// <para />Therefore, we are not constrained by the max size
        /// of the managed memory, but we want to make sure we don't
        /// use too much memory on low end devices, so that we don't
        /// force other background process to be killed.
        /// </summary>
        public static readonly int MAX_BITMAP_TOTAL_SIZE = GetMaxSizeHardCap();

        /// <summary>
        /// The maximum number of bitmaps.
        /// </summary>
        public const int MAX_BITMAP_COUNT = 384;

        private static BitmapCounter _bitmapCounter;

        private static int GetMaxSizeHardCap()
        {
            ulong maxMemory = Math.Min(MemoryManager.AppMemoryUsageLimit, int.MaxValue);
            if (maxMemory > 16 * MB)
            {
                return (int)(maxMemory / 4 * 3);
            }
            else
            {
                return (int)(maxMemory / 2);
            }
        }

        /// <summary>
        /// Gets the bitmap counter.
        /// </summary>
        /// <returns>The bitmap counter.</returns>
        public static BitmapCounter Get()
        {
            if (_bitmapCounter == null)
            {
                _bitmapCounter = new BitmapCounter(MAX_BITMAP_COUNT, MAX_BITMAP_TOTAL_SIZE);
            }

            return _bitmapCounter;
        }
    }
}
