using FBCore.Common.Internal;
using FBCore.Common.Memory;
using ImageUtils;
using System;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Memory
{
    /**
     * Manages a pool of bitmaps. This allows us to reuse bitmaps instead of constantly allocating
     * them (and pressuring the Java GC to garbage collect unused bitmaps).
     * <p>
     * The pool supports a get/release paradigm.
     * get() allows for a bitmap in the pool to be reused if it matches the desired
     * dimensions; if no such bitmap is found in the pool, a new one is allocated.
     * release() returns a bitmap to the pool.
     */
    public class BitmapPool : BasePool<SoftwareBitmap>
    {
        /**
        * Creates an instance of a bitmap pool.
        * @param memoryTrimmableRegistry the memory manager to register with
        * @param poolParams pool parameters
        */
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

        /**
         * Allocate a bitmap that has a backing memory allocacation of 'size' bytes.
         * This is configuration agnostic so the size is the actual size in bytes of the bitmap.
         * @param size the 'size' in bytes of the bitmap
         * @return a new bitmap with the specified size in memory
         */
        protected internal override SoftwareBitmap Alloc(int size)
        {
            return new SoftwareBitmap(
                BitmapPixelFormat.Bgra8,
                1,
                (int)Math.Ceiling(size / (double)BitmapUtil.BGRA8_BYTES_PER_PIXEL));
        }

        /**
         * Frees the bitmap
         * @param value the bitmap to free
         */
        protected internal override void Free(SoftwareBitmap value)
        {
            Preconditions.CheckNotNull(value);
            value.Dispose();
        }

        /**
         * Gets the bucketed size (typically something the same or larger than the requested size)
         * @param requestSize the logical request size
         * @return the 'bucketed' size
         */
        protected internal override int GetBucketedSize(int requestSize)
        {
            return requestSize;
        }

        /**
         * Gets the bucketed size of the value.
         * We don't check the 'validity' of the value (beyond the not-null check). That's handled
         * in {@link #isReusable(Bitmap)}
         * @param value the value
         * @return bucketed size of the value
         */
        protected internal override int GetBucketedSizeForValue(SoftwareBitmap value)
        {
            Preconditions.CheckNotNull(value);
            return (int)BitmapUtil.GetAllocationByteCount(value);
        }

        /**
         * Gets the size in bytes for the given bucketed size
         * @param bucketedSize the bucketed size
         * @return size in bytes
         */
        protected internal override int GetSizeInBytes(int bucketedSize)
        {
            return bucketedSize;
        }

        /**
         * Determine if this bitmap is reusable (i.e.) if subsequent {@link #get(int)} requests can
         * use this value.
         * The bitmap is reusable if
         *  - it has not already been recycled AND
         *  - it is mutable
         * @param value the value to test for reusability
         * @return true, if the bitmap can be reused
         */
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
                disposed = true;
            }

            return !disposed && !value.IsReadOnly;
        }
    }
}
