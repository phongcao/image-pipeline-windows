using Cache.Common;
using FBCore.Common.Internal;
using FBCore.Common.References;
using ImagePipeline.Bitmaps;
using ImagePipeline.Memory;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Request
{
    /// <summary>
    /// Base implementation of <see cref="IPostprocessor"/> interface.
    ///
    /// <para /> Clients should override exactly one of the three provided Process methods.
    /// </summary>
    public abstract class BasePostprocessor : IPostprocessor
    {
        /// <summary>
        /// Returns the name of this postprocessor.
        ///
        /// <para />Used for logging and analytics.
        /// </summary>
        public virtual string Name
        {
            get
            {
                return "Unknown postprocessor";
            }
        }

        /// <summary>
        /// Clients should override this method only if the post-processed bitmap has to be of a 
        /// different size than the source bitmap. If the post-processed bitmap is of the same size, 
        /// clients should override one of the other two methods.
        ///
        /// <para /> The source bitmap must not be modified as it may be shared by the other clients. 
        /// The implementation must create a new bitmap that is safe to be modified and return a 
        /// reference to it. Clients should use <code>bitmapFactory</code> to create a new bitmap.
        ///
        /// <param name="sourceBitmap">The source bitmap.</param>
        /// <param name="bitmapFactory">The factory to create a destination bitmap.</param>
        /// <param name="flexByteArrayPool">The memory pool used for post process.</param>
        /// @return a reference to the newly created bitmap.
        /// </summary>
        public CloseableReference<SoftwareBitmap> Process(
            SoftwareBitmap sourceBitmap,
            PlatformBitmapFactory bitmapFactory,
            FlexByteArrayPool flexByteArrayPool)
        {
            CloseableReference<SoftwareBitmap> destBitmapRef =
                bitmapFactory.CreateBitmapInternal(
                    sourceBitmap.PixelWidth,
                    sourceBitmap.PixelHeight,
                    sourceBitmap.BitmapPixelFormat);

            try
            {
                Process(destBitmapRef.Get(), sourceBitmap, flexByteArrayPool);
                return CloseableReference<SoftwareBitmap>.CloneOrNull(destBitmapRef);
            }
            finally
            {
                CloseableReference<SoftwareBitmap>.CloseSafely(destBitmapRef);
            }
        }

        /// <summary>
        /// Clients should override this method if the post-processing cannot be done in place. If the
        /// post-processing can be done in place, clients should override the 
        /// Process(byte[] data, int width, int height, BitmapPixelFormat format, BitmapAlphaMode alpha) 
        /// method.
        ///
        /// <para /> The provided destination bitmap is of the same size as the source bitmap. There 
        /// are no guarantees on the initial content of the destination bitmap, so the implementation 
        /// has to make sure that it properly populates it.
        ///
        /// <para /> The source bitmap must not be modified as it may be shared by the other clients.
        /// The implementation must use the provided destination bitmap as its output.
        ///
        /// <param name="destBitmap">The destination bitmap to be used as output.</param>
        /// <param name="sourceBitmap">The source bitmap to be used as input.</param>
        /// <param name="flexByteArrayPool">The memory pool used for post process.</param>
        /// </summary>
        public unsafe virtual void Process(
            SoftwareBitmap destBitmap, 
            SoftwareBitmap sourceBitmap,
            FlexByteArrayPool flexByteArrayPool)
        {
            Preconditions.CheckArgument(sourceBitmap.BitmapPixelFormat == destBitmap.BitmapPixelFormat);
            Preconditions.CheckArgument(!destBitmap.IsReadOnly);
            Preconditions.CheckArgument(destBitmap.PixelWidth == sourceBitmap.PixelWidth);
            Preconditions.CheckArgument(destBitmap.PixelHeight == sourceBitmap.PixelHeight);
            sourceBitmap.CopyTo(destBitmap);

            using (var buffer = destBitmap.LockBuffer(BitmapBufferAccessMode.Write))
            using (var reference = buffer.CreateReference())
            {
                // Get input data
                byte* srcData;
                uint capacity;
                ((IMemoryBufferByteAccess)reference).GetBuffer(out srcData, out capacity);

                // Allocate temp buffer for processing
                byte[] desData = default(byte[]);
                CloseableReference<byte[]> bytesArrayRef = default(CloseableReference<byte[]>);

                try
                {
                    bytesArrayRef = flexByteArrayPool.Get((int)capacity);
                    desData = bytesArrayRef.Get();
                }
                catch (Exception)
                {
                    // Allocates the byte array since the pool couldn't provide one
                    desData = new byte[capacity];
                }

                try
                {
                    // Process output data
                    Marshal.Copy((IntPtr)srcData, desData, 0, (int)capacity);
                    Process(desData, destBitmap.PixelWidth, destBitmap.PixelHeight,
                        destBitmap.BitmapPixelFormat, destBitmap.BitmapAlphaMode);

                    Marshal.Copy(desData, 0, (IntPtr)srcData, (int)capacity);
                }
                finally
                {
                    CloseableReference<byte[]>.CloseSafely(bytesArrayRef);
                }
            }
        }

        /// <summary>
        /// Clients should override this method if the post-processing can be done in place.
        ///
        /// <para /> The provided bitmap is a copy of the source bitmap and the implementation is 
        /// free to modify it.
        ///
        /// <param name="data">The bitmap pixel data.</param>
        /// <param name="width">The width of the new software bitmap, in pixels.</param>
        /// <param name="height">The height of the new software bitmap, in pixels.</param>
        /// <param name="format">The pixel format of the new software bitmap.</param>
        /// <param name="alpha">The alpha mode of the new software bitmap.</param>
        /// </summary>
        public virtual void Process(
            byte[] data,
            int width, 
            int height,
            BitmapPixelFormat format,
            BitmapAlphaMode alpha)
        {
        }

        /// <summary>
        /// The default implementation of the CacheKey for a Postprocessor is null.
        /// @return The CacheKey to use for caching. Not used if null.
        /// </summary>
        public virtual ICacheKey PostprocessorCacheKey
        {
            get
            {
                return null;
            }
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
