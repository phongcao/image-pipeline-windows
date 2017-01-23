using FBCore.Common.Internal;
using FBCore.Common.References;
using ImageFormatUtils;
using ImagePipeline.Memory;
using ImageUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ImagePipeline.Image
{
    /// <summary>
    /// Class that contains all the information for an encoded image, both the image bytes (held on
    /// a byte buffer or a supplier of input streams) and the extracted meta data that is useful for
    /// image transforms.
    ///
    /// <para />Only one of the input stream supplier or the byte buffer can be set. If using an input stream
    /// supplier, the methods that return a byte buffer will simply return null. However, GetInputStream
    /// will always be supported, either from the supplier or an input stream created from the byte
    /// buffer held.
    ///
    /// <para />Currently the data is useful for rotation and resize.
    /// </summary>
    public class EncodedImage : IDisposable
    {
        /// <summary>
        /// Rotation angle default value
        /// </summary>
        public const int UNKNOWN_ROTATION_ANGLE = -1;

        /// <summary>
        /// Width default value
        /// </summary>
        public const int UNKNOWN_WIDTH = -1;

        /// <summary>
        /// Height default value
        /// </summary>
        public const int UNKNOWN_HEIGHT = -1;

        /// <summary>
        /// Stream size default value
        /// </summary>
        public const int UNKNOWN_STREAM_SIZE = -1;

        /// <summary>
        /// Sample size default value
        /// </summary>
        public const int DEFAULT_SAMPLE_SIZE = 1;

        private readonly object _imageGate = new object();

        // Only one of this will be set. The EncodedImage can either be backed by a ByteBuffer or a
        // Supplier of InputStream, but not both.
        private readonly CloseableReference<IPooledByteBuffer> _pooledByteBufferRef;
        private readonly ISupplier<FileStream> _inputStreamSupplier;

        /// <summary>
        /// Instantiates the <see cref="EncodedImage"/> with provided params
        /// </summary>
        public EncodedImage(CloseableReference<IPooledByteBuffer> pooledByteBufferRef)
        {
            Preconditions.CheckArgument(CloseableReference<IPooledByteBuffer>.IsValid(pooledByteBufferRef));
            _pooledByteBufferRef = pooledByteBufferRef.Clone();
            _inputStreamSupplier = null;
        }

        /// <summary>
        /// Instantiates the <see cref="EncodedImage"/> with provided params
        /// </summary>
        public EncodedImage(ISupplier<FileStream> inputStreamSupplier)
        {
            Preconditions.CheckNotNull(inputStreamSupplier);
            _pooledByteBufferRef = null;
            _inputStreamSupplier = inputStreamSupplier;
        }

        /// <summary>
        /// Instantiates the <see cref="EncodedImage"/> with provided params
        /// </summary>
        public EncodedImage(ISupplier<FileStream> inputStreamSupplier, int streamSize) : this(inputStreamSupplier)
        {
            StreamSize = streamSize;
        }

        /// <summary>
         /// Returns the cloned encoded image if the parameter received is not null, null otherwise.
         ///
         /// <param name="encodedImage">the EncodedImage to clone</param>
         /// </summary>
        public static EncodedImage CloneOrNull(EncodedImage encodedImage)
        {
            return encodedImage != null ? encodedImage.CloneOrNull() : null;
        }

        /// <summary>
        /// Returns a new CloseableReference to the same underlying SharedReference 
        /// or null if invalid. The SharedReference ref-count is incremented.
        /// </summary>
        public EncodedImage CloneOrNull()
        {
            EncodedImage encodedImage;
            if (_inputStreamSupplier != null)
            {
                encodedImage = new EncodedImage(_inputStreamSupplier, StreamSize);
            }
            else
            {
                CloseableReference<IPooledByteBuffer> pooledByteBufferRef =
                        CloseableReference<IPooledByteBuffer>.CloneOrNull(_pooledByteBufferRef);
                try
                {
                    encodedImage = (pooledByteBufferRef == null) ? null : new EncodedImage(pooledByteBufferRef);
                }
                finally
                {
                    // Close the recently created reference since it will be cloned again in the constructor.
                    CloseableReference<IPooledByteBuffer>.CloseSafely(pooledByteBufferRef);
                }
            }

            if (encodedImage != null)
            {
                encodedImage.CopyMetaDataFrom(this);
            }

            return encodedImage;
        }

        /// <summary>
        /// Closes the buffer enclosed by this class.
        /// </summary>
        public void Dispose()
        {
            CloseableReference<IPooledByteBuffer>.CloseSafely(_pooledByteBufferRef);
        }

        /// <summary>
        /// Returns true if the internal buffer reference is valid or the InputStream Supplier is not null,
        /// false otherwise.
        /// </summary>
        public bool Valid
        {
            get
            {
                lock (_imageGate)
                {
                    return CloseableReference<IPooledByteBuffer>.IsValid(
                        _pooledByteBufferRef) || _inputStreamSupplier != null;
                }
            }
        }

        /// <summary>
        /// Returns a cloned reference to the stored encoded bytes.
        ///
        /// <para />The caller has to close the reference once it has finished using it.
        /// </summary>
        public CloseableReference<IPooledByteBuffer> GetByteBufferRef()
        {
            return CloseableReference<IPooledByteBuffer>.CloneOrNull(_pooledByteBufferRef);
        }

        /// <summary>
        /// Returns an InputStream from the internal InputStream Supplier if it's not null. Otherwise
        /// returns an InputStream for the internal buffer reference if valid and null otherwise.
        ///
        /// <para />The caller has to close the InputStream after using it.
        /// </summary>
        public Stream GetInputStream()
        {
            if (_inputStreamSupplier != null)
            {
                return _inputStreamSupplier.Get();
            }

            CloseableReference<IPooledByteBuffer> pooledByteBufferRef =
                CloseableReference<IPooledByteBuffer>.CloneOrNull(_pooledByteBufferRef);

            if (pooledByteBufferRef != null)
            {
                try
                {
                    return new PooledByteBufferInputStream(pooledByteBufferRef.Get());
                }
                finally
                {
                    CloseableReference<IPooledByteBuffer>.CloseSafely(pooledByteBufferRef);
                }
            }

            return null;
        }

        /// <summary>
        /// Image format
        /// </summary>
        public ImageFormat Format { get; set; } = ImageFormat.UNKNOWN;

        /// <summary>
        /// Image height
        /// </summary>
        public int Height { get; set; } = UNKNOWN_HEIGHT;

        /// <summary>
        /// Image width
        /// </summary>
        public int Width { get; set; } = UNKNOWN_WIDTH;

        /// <summary>
        /// Rotation angle
        /// </summary>
        public int RotationAngle { get; set; } = UNKNOWN_ROTATION_ANGLE;

        /// <summary>
        /// Sample size
        /// </summary>
        public int SampleSize { get; set; } = DEFAULT_SAMPLE_SIZE;

        /// <summary>
        /// Stream size
        /// </summary>
        public int StreamSize { get; set; } = UNKNOWN_STREAM_SIZE;

        /// <summary>
        /// Returns true if the image is a JPEG and its data is already complete at the specified length,
        /// false otherwise.
        /// </summary>
        public bool IsCompleteAt(int length)
        {
            if (Format != ImageFormat.JPEG)
            {
                return true;
            }

            // If the image is backed by FileStream return true since they will always be complete.
            if (_inputStreamSupplier != null)
            {
                return true;
            }

            // The image should be backed by a ByteBuffer
            Preconditions.CheckNotNull(_pooledByteBufferRef);
            IPooledByteBuffer buf = _pooledByteBufferRef.Get();
            return (buf.Read(length - 2) == JfifUtil.MARKER_FIRST_BYTE)
                && (buf.Read(length - 1) == JfifUtil.MARKER_EOI);
        }

        /// <summary>
        /// Returns the size of the backing structure.
        ///
        /// <para /> If it's a PooledByteBuffer returns its size if its not null, -1 otherwise. If it's an
        /// InputStream, return the size if it was set, -1 otherwise.
        /// </summary>
        public int Size
        {
            get
            {
                if (_pooledByteBufferRef != null && _pooledByteBufferRef.Get() != null)
                {
                    return _pooledByteBufferRef.Get().Size;
                }

                return StreamSize;
            }
        }

        /// <summary>
        /// Sets the encoded image meta data.
        /// </summary>
        public async Task ParseMetaDataAsync()
        {
            ImageFormat format = ImageFormatChecker.GetImageFormat_WrapIOException(
                GetInputStream());
            Format = format;

            // Dimensions decoding is not yet supported for WebP since BitmapUtil.decodeDimensions has a
            // bug where it will return 100x100 for some WebPs even though those are not its actual
            // dimensions
            if (!ImageFormatHelper.IsWebpFormat(Format))
            {
                KeyValuePair<int, int> dimensions = 
                    await BitmapUtil.DecodeDimensionsAsync(GetInputStream()).ConfigureAwait(false);
                if (!dimensions.Equals(default(KeyValuePair<int, int>)))
                {
                    Width = dimensions.Key;
                    Height = dimensions.Value;

                    // Load the rotation angle only if we have the dimensions
                    if (Format == ImageFormat.JPEG)
                    {
                        if (RotationAngle == UNKNOWN_ROTATION_ANGLE)
                        {
                            RotationAngle = JfifUtil.GetAutoRotateAngleFromOrientation(
                                JfifUtil.GetOrientation(GetInputStream()));
                        }
                    }
                    else
                    {
                        RotationAngle = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Copy the meta data from another EncodedImage.
        ///
        /// <param name="encodedImage">the EncodedImage to copy the meta data from.</param>
        /// </summary>
        public void CopyMetaDataFrom(EncodedImage encodedImage)
        {
            Format = encodedImage.Format;
            Width = encodedImage.Width;
            Height = encodedImage.Height;
            RotationAngle = encodedImage.RotationAngle;
            SampleSize = encodedImage.SampleSize;
            StreamSize = encodedImage.Size;
        }

        /// <summary>
        /// Returns true if all the image information has loaded, false otherwise.
        /// </summary>
        public static bool IsMetaDataAvailable(EncodedImage encodedImage)
        {
            return encodedImage.RotationAngle >= 0
                && encodedImage.Width >= 0
                && encodedImage.Height >= 0;
        }

        /// <summary>
        /// Closes the encoded image handling null.
        ///
        /// <param name="encodedImage">the encoded image to close.</param>
        /// </summary>
        public static void CloseSafely(EncodedImage encodedImage)
        {
            if (encodedImage != null)
            {
                encodedImage.Dispose();
            }
        }

        /// <summary>
        /// Checks if the encoded image is valid i.e. is not null, and is not closed.
        /// @return true if the encoded image is valid
        /// </summary>
        public static bool IsValid(EncodedImage encodedImage)
        {
            return encodedImage != null && encodedImage.Valid;
        }

        /// <summary>
        /// A test-only method to get the underlying references.
        ///
        /// <para /><b>DO NOT USE in application code.</b>
        /// </summary>
        public SharedReference<IPooledByteBuffer> GetUnderlyingReferenceTestOnly()
        {
            lock (_imageGate)
            {
                return (_pooledByteBufferRef != null) ?
                    _pooledByteBufferRef.GetUnderlyingReferenceTestOnly() : null;
            }
        }
    }
}
