using FBCore.Common.Internal;
using FBCore.Common.References;
using ImagePipeline.Common;
using ImageUtils;
using System;
using System.Collections.Generic;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Counts bitmaps - keeps track of both, count and total size in bytes.
    /// </summary>
    public class BitmapCounter
    {
        private readonly object _bitmapGate = new object();

        private int _count;

        private long _size;

        private readonly int _maxCount;
        private readonly int _maxSize;
        private readonly IResourceReleaser<SoftwareBitmap> _unpooledBitmapsReleaser;

        /// <summary>
        /// Instantiates the <see cref="BitmapCounter"/>.
        /// </summary>
        /// <param name="maxCount"></param>
        /// <param name="maxSize"></param>
        public BitmapCounter(int maxCount, int maxSize)
        {
            Preconditions.CheckArgument(maxCount > 0);
            Preconditions.CheckArgument(maxSize > 0);
            _maxCount = maxCount;
            _maxSize = maxSize;
            _unpooledBitmapsReleaser = new ResourceReleaserImpl<SoftwareBitmap>(value =>
            {
                try
                {
                    Decrease(value);
                }
                finally
                {
                    value.Dispose();
                }
            });
        }

        /// <summary>
        /// Includes given bitmap in the bitmap count. The bitmap is included only if doing so
        /// does not violate configured limit
        ///
        /// <param name="bitmap">To include in the count</param>
        /// @return true if and only if bitmap is successfully included in the count
        /// </summary>
        public bool Increase(SoftwareBitmap bitmap)
        {
            lock (_bitmapGate)
            {
                uint bitmapSize = BitmapUtil.GetSizeInBytes(bitmap);
                if (_count >= _maxCount || _size + bitmapSize > _maxSize)
                {
                    return false;
                }

                _count++;
                _size += bitmapSize;
                return true;
            }
        }

        /// <summary>
        /// Excludes given bitmap from the count.
        ///
        /// <param name="bitmap">to be excluded from the count</param>
        /// </summary>
        public void Decrease(SoftwareBitmap bitmap)
        {
            lock (_bitmapGate)
            {
                uint bitmapSize = BitmapUtil.GetSizeInBytes(bitmap);
                Preconditions.CheckArgument(_count > 0, "No bitmaps registered.");
                Preconditions.CheckArgument(
                    bitmapSize <= _size,
                    "Bitmap size bigger than the total registered size: %d, %d",
                    bitmapSize,
                    _size);
                _size -= bitmapSize;
                _count--;
            }
        }

        /// <summary>
        /// @return number of counted bitmaps
        /// </summary>
        public int GetCount()
        {
            lock (_bitmapGate)
            {
                return _count;
            }
        }

        /// <summary>
        /// @return total size in bytes of counted bitmaps
        /// </summary>
        public long GetSize()
        {
            lock (_bitmapGate)
            {
                return _size;
            }
        }

        /// <summary>
        /// Gets the bitmap pool releaser
        /// </summary>
        /// <returns>The bitmap pool releaser</returns>
        public IResourceReleaser<SoftwareBitmap> GetReleaser()
        {
            return _unpooledBitmapsReleaser;
        }

        /// <summary>
        /// Associates bitmaps with the bitmap counter. <p/> <para />If this method throws
        /// TooManyBitmapsException, the code will have called<see cref="SoftwareBitmap.Dispose"/> on the
        /// bitmaps.
        ///
        /// <param name = "bitmaps" > the bitmaps to associate</param>
        /// @return the references to the bitmaps that are now tied to the bitmap pool
        /// @throws TooManyBitmapsException if the pool is full
        /// </summary>
        public IList<CloseableReference<SoftwareBitmap>> AssociateBitmapsWithBitmapCounter(IList<SoftwareBitmap> bitmaps)
        {
            int countedBitmaps = 0;

            try
            {
                for (; countedBitmaps < bitmaps.Count; ++countedBitmaps)
                {
                    SoftwareBitmap bitmap = bitmaps[countedBitmaps];

                    if (!Increase(bitmap))
                    {
                        throw new TooManyBitmapsException();
                    }
                }

                List<CloseableReference<SoftwareBitmap>> ret = new List<CloseableReference<SoftwareBitmap>>(bitmaps.Count);
                foreach (var bitmap in bitmaps)
                {
                    ret.Add(CloseableReference<SoftwareBitmap>.of(bitmap, _unpooledBitmapsReleaser));
                }

                return ret;
            }
            catch (Exception exception)
            {
                if (bitmaps != null)
                {
                    foreach (var bitmap in bitmaps)
                    {
                        if (countedBitmaps-- > 0)
                        {
                            Decrease(bitmap);
                        }

                        bitmap.Dispose();
                    }
                }

                throw exception;
            }
        }
    }
}
