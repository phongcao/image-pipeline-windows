using FBCore.Common.Internal;
using FBCore.Common.Memory;
using ImageUtils;
using System;
using System.Diagnostics;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Manages a pool of bitmaps. This allows us to reuse bitmaps instead of constantly allocating
    /// them (and pressuring the Java GC to garbage collect unused bitmaps).
    /// <para />
    /// The pool supports a get/release paradigm.
    /// get() allows for a bitmap in the pool to be reused if it matches the desired
    /// dimensions; if no such bitmap is found in the pool, a new one is allocated.
    /// release() returns a bitmap to the pool.
    /// </summary>
    public class BitmapPool : BasePool<SoftwareBitmap>
    {
        /// <summary>
        /// Creates an instance of a bitmap pool.
        /// <param name="memoryTrimmableRegistry">The memory manager to register with</param>
        /// <param name="poolParams">Pool parameters</param>
        /// <param name="poolStatsTracker">Listener that logs pool statistics</param>
        /// </summary>
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
        /// Allocate a bitmap that has a backing memory allocacation of 'size' bytes.
        /// This is configuration agnostic so the size is the actual size in bytes of the bitmap.
        /// <param name="size">The 'size' in bytes of the bitmap</param>
        /// @return a new bitmap with the specified size in memory
        /// </summary>
        protected internal override SoftwareBitmap Alloc(int size)
        {
            return new SoftwareBitmap(
                BitmapPixelFormat.Bgra8,
                1,
                (int)Math.Ceiling(size / (double)BitmapUtil.BGRA8_BYTES_PER_PIXEL));
        }

        /// <summary>
        /// Frees the bitmap
        /// <param name="value">The bitmap to free</param>
        /// </summary>
        protected internal override void Free(SoftwareBitmap value)
        {
            Preconditions.CheckNotNull(value);
            value.Dispose();
        }

        /// <summary>
        /// Gets the bucketed size (typically something the same or larger than the requested size)
        /// <param name="requestSize">The logical request size</param>
        /// @return the 'bucketed' size
        /// </summary>
        protected internal override int GetBucketedSize(int requestSize)
        {
            return requestSize;
        }

        /// <summary>
        /// Gets the bucketed size of the value.
        /// We don't check the 'validity' of the value (beyond the not-null check). That's handled
        /// in <see cref="IsReusable(SoftwareBitmap)"/>
        /// <param name="value">The value</param>
        /// @return bucketed size of the value
        /// </summary>
        protected internal override int GetBucketedSizeForValue(SoftwareBitmap value)
        {
            Preconditions.CheckNotNull(value);
            return (int)BitmapUtil.GetAllocationByteCount(value);
        }

        /// <summary>
        /// Gets the size in bytes for the given bucketed size
        /// <param name="bucketedSize">The bucketed size</param>
        /// @return size in bytes
        /// </summary>
        protected internal override int GetSizeInBytes(int bucketedSize)
        {
            return bucketedSize;
        }

        /// <summary>
        /// Determine if this bitmap is reusable (i.e.) if subsequent Get(int) requests can
        /// use this value.
        /// The bitmap is reusable if
        ///  - it has not already been recycled AND
        ///  - it is mutable
        /// <param name="value">The value to test for reusability</param>
        /// @return true, if the bitmap can be reused
        /// </summary>
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
