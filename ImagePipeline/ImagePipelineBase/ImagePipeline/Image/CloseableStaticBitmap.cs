using FBCore.Common.Internal;
using FBCore.Common.References;
using ImageUtils;
using System;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Image
{
    /// <summary>
    /// CloseableImage that contains one SoftwareBitmap.
    /// </summary>
    public sealed class CloseableStaticBitmap : CloseableBitmap
    {
        private readonly object _bitmapGate = new object();

        private CloseableReference<SoftwareBitmap> _bitmapReference;

        private volatile SoftwareBitmap _bitmap;

        /// <summary>
        /// Quality info
        /// </summary>
        private readonly IQualityInfo _qualityInfo;

        private readonly int _rotationAngle;

        /// <summary>
        /// Creates a new instance of a CloseableStaticBitmap.
        /// </summary>
        public CloseableStaticBitmap(
            SoftwareBitmap bitmap,
            IResourceReleaser<SoftwareBitmap> resourceReleaser,
            IQualityInfo qualityInfo,
            int rotationAngle)
        {
            _bitmap = Preconditions.CheckNotNull(bitmap);
            _bitmapReference = CloseableReference<SoftwareBitmap>.of(
                _bitmap,
                Preconditions.CheckNotNull(resourceReleaser));
            _qualityInfo = qualityInfo;
            _rotationAngle = rotationAngle;
        }

        /// <summary>
        /// Creates a new instance of a CloseableStaticBitmap from an existing CloseableReference. The
        /// CloseableStaticBitmap will hold a reference to the Bitmap until it's closed.
        /// </summary>
        public CloseableStaticBitmap(
            CloseableReference<SoftwareBitmap> bitmapReference,
            IQualityInfo qualityInfo,
            int rotationAngle)
        {
            _bitmapReference = Preconditions.CheckNotNull(bitmapReference.CloneOrNull());
            _bitmap = _bitmapReference.Get();
            _qualityInfo = qualityInfo;
            _rotationAngle = rotationAngle;
        }

        /// <summary>
        /// This has to be called before we get rid of this object in order to release underlying memory
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CloseableReference<SoftwareBitmap> reference = DetachBitmapReference();
            if (reference != null)
            {
                reference.Dispose();
            }
        }

        private CloseableReference<SoftwareBitmap> DetachBitmapReference()
        {
            lock (_bitmapGate)
            {
                CloseableReference<SoftwareBitmap> reference = _bitmapReference;
                _bitmapReference = null;
                _bitmap = null;
                return reference;
            }
        }

        /// <summary>
        /// Convert this object to a CloseableReference{Bitmap}.
        /// <para />You cannot call this method on an object that has already been closed.
        /// <para />The reference count of the bitmap is preserved. After calling this method, this object
        /// can no longer be used and no longer points to the bitmap.
        /// @throws IllegalArgumentException if this object has already been closed.
        /// </summary>
        public CloseableReference<SoftwareBitmap> ConvertToBitmapReference()
        {
            lock (_bitmapGate)
            {
                Preconditions.CheckNotNull(_bitmapReference, "Cannot convert a closed static bitmap");
                return DetachBitmapReference();
            }
        }

        /// <summary>
        /// Returns whether this instance is closed.
        /// </summary>
        public override bool IsClosed
        {
            get
            {
                lock (_bitmapGate)
                {
                    return _bitmapReference == null;
                }
            }
        }

        /// <summary>
        /// Gets the underlying bitmap.
        ///
        /// @return the underlying bitmap
        /// </summary>
        public override SoftwareBitmap UnderlyingBitmap
        {
            get
            {
                return _bitmap;
            }
        }

        /// <summary>
        /// Returns size in bytes of the underlying bitmap
        /// </summary>
        public override int SizeInBytes
        {
            get
            {
                return (int)BitmapUtil.GetSizeInBytes(_bitmap);
            }
        }

        /// <summary>
        /// Returns width of the image
        /// </summary>
        public override int Width
        {
            get
            {
                SoftwareBitmap bitmap = _bitmap;
                return (bitmap == null) ? 0 : bitmap.PixelWidth;
            }
        }

        /// <summary>
        /// Returns width of the image
        /// </summary>
        public override int Height
        {
            get
            {
                SoftwareBitmap bitmap = _bitmap;
                return (bitmap == null) ? 0 : bitmap.PixelHeight;
            }
        }

        /// <summary>
        /// Returns the rotation angle of the image
        /// </summary>
        public int RotationAngle
        {
            get
            {
                return _rotationAngle;
            }
        }

        /// <summary>
        /// Returns quality information for the image.
        /// </summary>
        public override IQualityInfo QualityInfo
        {
            get
            {
                return _qualityInfo;
            }
        }
    }
}
