using FBCore.Common.Internal;
using FBCore.Common.Memory;
using ImageUtils;
using System;
using System.Diagnostics;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Manages a pool of bitmaps. This allows us to reuse bitmaps instead of
    /// constantly allocating them (and pressuring the GC to garbage collect
    /// unused bitmaps).
    /// <para />
    /// The pool supports a Get/Release paradigm.
    /// Get() allows for a bitmap in the pool to be reused if it matches the
    /// desired dimensions; if no such bitmap is found in the pool, a new one
    /// is allocated.
    /// Release() returns a bitmap to the pool.
    /// </summary>
    public class BitmapPool : BasePool<SoftwareBitmap>
    {
        /// <summary>
        /// Creates an instance of a bitmap pool.
        /// </summary>
        /// <param name="memoryTrimmableRegistry">
        /// The memory manager to register with.
        /// </param>
        /// <param name="poolParams">Pool parameters.</param>
        /// <param name="poolStatsTracker">
        /// Listener that logs pool statistics.
        /// </param>
        public BitmapPool(
            IMemoryTrimmableRegistry memoryTrimmableRegistry,
            PoolParams poolParams,
            PoolStatsTracker poolStatsTracker) : base(
                memoryTrimmableRegistry,
                poolParams,
                poolStatsTracker)
        {
            Initialize();
        }

        /// <summary>
        /// Allocate a bitmap that has a backing memory allocacation of
        /// 'size' bytes.
        /// This is configuration agnostic so the size is the actual
        /// size in bytes of the bitmap.
        /// </summary>
        /// <param name="size">
        /// The 'size' in bytes of the bitmap.
        /// </param>
        /// <returns>
        /// A new bitmap with the specified size in memory.
        /// </returns>
        protected internal override SoftwareBitmap Alloc(int size)
        {
            return new SoftwareBitmap(
                BitmapPixelFormat.Bgra8,
                1,
                (int)Math.Ceiling(size / (double)BitmapUtil.BGRA8_BYTES_PER_PIXEL));
        }

        /// <summary>
        /// Frees the bitmap.
        /// </summary>
        /// <param name="value">The bitmap to free.</param>
        protected internal override void Free(SoftwareBitmap value)
        {
            Preconditions.CheckNotNull(value);
            value.Dispose();
        }

        /// <summary>
        /// Gets the bucketed size (typically something the same or larger
        /// than the requested size).
        /// </summary>
        /// <param name="requestSize">The logical request size.</param>
        /// <returns>The 'bucketed' size.</returns>
        protected internal override int GetBucketedSize(int requestSize)
        {
            return requestSize;
        }

        /// <summary>
        /// Gets the bucketed size of the value.
        /// We don't check the 'validity' of the value (beyond the not-null
        /// check). That's handled in <see cref="IsReusable(SoftwareBitmap)"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Bucketed size of the value.</returns>
        protected internal override int GetBucketedSizeForValue(SoftwareBitmap value)
        {
            Preconditions.CheckNotNull(value);
            return (int)BitmapUtil.GetAllocationByteCount(value);
        }

        /// <summary>
        /// Determine if this bitmap is reusable (i.e.) if subsequent
        /// Get(int) requests can use this value.
        /// The bitmap is reusable if
        ///  - It has not already been recycled AND
        ///  - It is mutable
        /// </summary>
        /// <param name="value">
        /// The value to test for reusability.
        /// </param>
        /// <returns>
        /// true, if the bitmap can be reused.
        /// </returns>
        protected internal override bool IsReusable(SoftwareBitmap value)
        {
            Preconditions.CheckNotNull(value);
            bool disposed = false;

            // Check if object has been disposed
            try
            {
                disposed = (value.PixelWidth == 0);
            }
            catch (ObjectDisposedException e)
            {
                Debug.WriteLine($"{e.Message} is expected");
                disposed = true;
            }

            return !disposed && !value.IsReadOnly;
        }
    }
}
