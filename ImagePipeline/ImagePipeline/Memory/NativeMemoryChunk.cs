using FBCore.Common.Internal;
using System;
using System.Diagnostics;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Wrapper around chunk of native memory.
    ///
    /// <para /> This class uses JNI to obtain pointer to native memory and read/write data from/to it.
    ///
    /// <para /> Native code used by this class is shipped as part of libimagepipeline.so
    ///
    /// </summary>
    public class NativeMemoryChunk : IDisposable
    {
        private readonly object _memoryChunkGate = new object();

        /// <summary>
        /// Address of memory chunk wrapped by this NativeMemoryChunk
        /// </summary>
        private readonly long _nativePtr;

        /// <summary>
        /// Size of the memory region
        /// </summary>
        public virtual int Size { get; }

        /// <summary>
        /// Flag indicating if this object was closed
        /// </summary>
        private bool _closed;

        /// <summary>
        /// Instantiates the <see cref="NativeMemoryChunk"/>.
        /// </summary>
        /// <param name="size"></param>
        public NativeMemoryChunk(int size)
        {
            Preconditions.CheckArgument(size > 0);
            Size = size;
            _nativePtr = ImagePipelineNative.NativeMemoryChunk.NativeAllocate(Size);
            _closed = false;
        }

        /// <summary>
        /// Instantiates the <see cref="NativeMemoryChunk"/>.
        /// </summary>
        public NativeMemoryChunk()
        {
            Size = 0;
            _nativePtr = 0;
            _closed = true;
        }

        /// <summary>
        /// A finalizer, just in case. Just delegates to <see cref="Dispose()"/>
        /// @throws Throwable
        /// </summary>
        ~NativeMemoryChunk()
        {
            if (IsClosed)
            {
                return;
            }

            Debug.WriteLine($"finalize: Chunk { GetHashCode().ToString("X4") } still active. Underlying address = { _nativePtr.ToString("X4") }");

            // Do the actual clearing
            Dispose(false);
        }

        /// <summary>
        /// This has to be called before we get rid of this object in order to release underlying memory
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// This has to be called before we get rid of this object in order to release underlying memory
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            lock (_memoryChunkGate)
            {
                if (!_closed)
                {
                    _closed = true;
                    ImagePipelineNative.NativeMemoryChunk.NativeFree(_nativePtr);
                }
            }
        }

        /// <summary>
        /// Is this chunk already closed (aka freed) ?
        /// @return true, if this chunk has already been closed
        /// </summary>
        public virtual bool IsClosed
        {
            get
            {
                lock (_memoryChunkGate)
                {
                    return _closed;
                }
            }
        }

        /// <summary>
        /// Copy bytes from byte array to native memory.
        /// <param name="nativeMemoryOffset">number of first byte to be written by copy operation</param>
        /// <param name="byteArray">byte array to copy from</param>
        /// <param name="byteArrayOffset">number of first byte in byteArray to copy</param>
        /// <param name="count">number of bytes to copy</param>
        /// @return number of bytes written
        /// </summary>
        public virtual int Write(
            int nativeMemoryOffset,
            byte[] byteArray,
            int byteArrayOffset,
            int count)
        {
            lock (_memoryChunkGate)
            {
                Preconditions.CheckNotNull(byteArray);
                Preconditions.CheckState(!IsClosed);
                int actualCount = AdjustByteCount(nativeMemoryOffset, count);
                CheckBounds(nativeMemoryOffset, byteArray.Length, byteArrayOffset, actualCount);
                ImagePipelineNative.NativeMemoryChunk.NativeCopyFromByteArray(
                    _nativePtr + nativeMemoryOffset,
                    byteArray,
                    byteArrayOffset,
                    actualCount);
                return actualCount;
            }
        }

        /// <summary>
        /// Copy bytes from native memory to byte array.
        /// <param name="nativeMemoryOffset">number of first byte to copy</param>
        /// <param name="byteArray">byte array to copy to</param>
        /// <param name="byteArrayOffset">number of first byte in byte array to be written</param>
        /// <param name="count">number of bytes to copy</param>
        /// @return number of bytes read
        /// </summary>
        public virtual int Read(
            int nativeMemoryOffset,
            byte[] byteArray,
            int byteArrayOffset,
            int count)
        {
            lock (_memoryChunkGate)
            {
                Preconditions.CheckNotNull(byteArray);
                Preconditions.CheckState(!IsClosed);
                int actualCount = AdjustByteCount(nativeMemoryOffset, count);
                CheckBounds(nativeMemoryOffset, byteArray.Length, byteArrayOffset, actualCount);
                ImagePipelineNative.NativeMemoryChunk.NativeCopyToByteArray(
                    _nativePtr + nativeMemoryOffset, byteArray, byteArrayOffset, actualCount);
                return actualCount;
            }
        }

        /// <summary>
        /// Read byte at given offset.
        /// <param name="offset"></param>
        /// @return byte at given offset
        /// </summary>
        public virtual byte Read(int offset)
        {
            lock (_memoryChunkGate)
            {
                Preconditions.CheckState(!IsClosed);
                Preconditions.CheckArgument(offset >= 0);
                Preconditions.CheckArgument(offset < Size);
                return ImagePipelineNative.NativeMemoryChunk.NativeReadByte(_nativePtr + offset);
            }
        }

        /// <summary>
        /// Copy bytes from native memory wrapped by this NativeMemoryChunk instance to
        /// native memory wrapped by other NativeMemoryChunk
        /// <param name="offset">number of first byte to copy</param>
        /// <param name="other">other NativeMemoryChunk to copy to</param>
        /// <param name="otherOffset">number of first byte to write to</param>
        /// <param name="count">number of bytes to copy</param>
        /// </summary>
        public virtual void Copy(
            int offset,
            NativeMemoryChunk other,
            int otherOffset,
            int count)
        {
            Preconditions.CheckNotNull(other);

            // This implementation acquires locks on this and other objects and then delegates to
            // doCopy which does actual copy. In order to avoid deadlocks we have to establish some linear
            // order on all NativeMemoryChunks and acquire locks according to this order. Fortunately
            // we can use mNativePtr for that purpose. So we have to address 3 cases:

            // Case 1: other memory chunk == this memory chunk
            if (other._nativePtr == _nativePtr)
            {
                // we do not allow copying to the same address
                // lets log warning and not copy
                Debug.WriteLine($"Copying from NativeMemoryChunk { GetHashCode().ToString("X4") } to NativeMemoryChunk { other.GetHashCode().ToString("X4") } which share the same address { _nativePtr.GetHashCode().ToString("X4") }");
                Preconditions.CheckArgument(false);
            }

            // Case 2: other memory chunk < this memory chunk
            if (other._nativePtr < _nativePtr)
            {
                lock (_memoryChunkGate)
                {
                    DoCopy(offset, other, otherOffset, count);
                }

                return;
            }

            // Case 3: other memory chunk > this memory chunk
            lock (_memoryChunkGate)
            {
                DoCopy(offset, other, otherOffset, count);
            }
        }

        /// <summary>
        /// Gets the native pointer
        /// </summary>
        /// <returns>The native pointer</returns>
        public long GetNativePtr()
        {
            return _nativePtr;
        }

        /// <summary>
        /// This does actual copy. It should be called only when we hold locks on both this and
        /// other objects
        /// </summary>
        private void DoCopy(
            int offset,
            NativeMemoryChunk other,
            int otherOffset,
            int count)
        {
            Preconditions.CheckState(!IsClosed);
            Preconditions.CheckState(!other.IsClosed);
            CheckBounds(offset, other.Size, otherOffset, count);
            ImagePipelineNative.NativeMemoryChunk.NativeMemcpy(
                other._nativePtr + otherOffset, _nativePtr + offset, count);
        }

        /// <summary>
        /// Computes number of bytes that can be safely read/written starting at given offset, but no more
        /// than count.
        /// </summary>
        private int AdjustByteCount(int offset, int count)
        {
            int available = Math.Max(0, Size - offset);
            return Math.Min(available, count);
        }

        /// <summary>
        /// Check that copy/read/write operation won't access memory it should not
        /// </summary>
        private void CheckBounds(
            int myOffset,
            int otherLength,
            int otherOffset,
            int count)
        {
            Preconditions.CheckArgument(count >= 0);
            Preconditions.CheckArgument(myOffset >= 0);
            Preconditions.CheckArgument(otherOffset >= 0);
            Preconditions.CheckArgument(myOffset + count <= Size);
            Preconditions.CheckArgument(otherOffset + count <= otherLength);
        }
    }
}
