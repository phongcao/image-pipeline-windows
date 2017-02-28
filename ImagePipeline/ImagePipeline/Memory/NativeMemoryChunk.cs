using FBCore.Common.Internal;
using ImagePipeline.NativeInterop;
using System;
using System.Diagnostics;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Wrapper around chunk of native memory.
    ///
    /// <para />This class uses Windows Runtime to obtain pointer to
    /// native memory and read/write data from/to it.
    /// </summary>
    public class NativeMemoryChunk : IDisposable
    {
        private readonly object _memoryChunkGate = new object();

        /// <summary>
        /// Address of memory chunk wrapped by this NativeMemoryChunk.
        /// </summary>
        private readonly long _nativePtr;

        /// <summary>
        /// Size of the memory region.
        /// </summary>
        public virtual int Size
        {
            get
            {
                return _size;
            }
        }

        /// <summary>
        /// Flag indicating if this object was closed.
        /// </summary>
        private bool _closed;

        private int _size;

        /// <summary>
        /// Instantiates the <see cref="NativeMemoryChunk"/>.
        /// </summary>
        /// <param name="size">The size of the chunk.</param>
        public NativeMemoryChunk(int size)
        {
            Preconditions.CheckArgument(size > 0);
            _size = size;
            _nativePtr = NativeMethods.NativeAllocate(size);
            _closed = false;
        }

        /// <summary>
        /// Instantiates the <see cref="NativeMemoryChunk"/>.
        /// </summary>
        public NativeMemoryChunk()
        {
            _size = 0;
            _nativePtr = 0;
            _closed = true;
        }

        /// <summary>
        /// This has to be called before we get rid of this object
        /// in order to release underlying memory.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// This has to be called before we get rid of this object
        /// in order to release underlying memory.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            lock (_memoryChunkGate)
            {
                if (!_closed)
                {
                    if (!disposing)
                    {
                        Debug.WriteLine($"finalize: Chunk { GetHashCode().ToString("X4") } still active. Underlying address = { _nativePtr.ToString("X4") }");
                    }

                    _closed = true;
                    NativeMethods.NativeFree(_nativePtr);
                }
            }
        }

        /// <summary>
        /// Is this chunk already closed (aka freed)?
        /// </summary>
        /// <returns>
        /// true, if this chunk has already been closed.
        /// </returns>
        public virtual bool Closed
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
        /// </summary>
        /// <param name="nativeMemoryOffset">
        /// Number of first byte to be written by copy operation.
        /// </param>
        /// <param name="byteArray">
        /// Byte array to copy from.
        /// </param>
        /// <param name="byteArrayOffset">
        /// Number of first byte in byteArray to copy.
        /// </param>
        /// <param name="count">
        /// Number of bytes to copy.
        /// </param>
        /// <returns>Number of bytes written.</returns>
        public virtual int Write(
            int nativeMemoryOffset,
            byte[] byteArray,
            int byteArrayOffset,
            int count)
        {
            lock (_memoryChunkGate)
            {
                Preconditions.CheckNotNull(byteArray);
                Preconditions.CheckState(!Closed);
                int actualCount = AdjustByteCount(nativeMemoryOffset, count);
                CheckBounds(nativeMemoryOffset, byteArray.Length, byteArrayOffset, actualCount);
                NativeMethods.NativeCopyFromByteArray(
                    _nativePtr + nativeMemoryOffset,
                    byteArray,
                    byteArrayOffset,
                    actualCount);

                return actualCount;
            }
        }

        /// <summary>
        /// Copy bytes from native memory to byte array.
        /// </summary>
        /// <param name="nativeMemoryOffset">
        /// Number of first byte to copy.
        /// </param>
        /// <param name="byteArray">
        /// Byte array to copy to.
        /// </param>
        /// <param name="byteArrayOffset">
        /// Number of first byte in byte array to be written.
        /// </param>
        /// <param name="count">
        /// Number of bytes to copy.
        /// </param>
        /// <returns>Number of bytes read.</returns>
        public virtual int Read(
            int nativeMemoryOffset,
            byte[] byteArray,
            int byteArrayOffset,
            int count)
        {
            lock (_memoryChunkGate)
            {
                Preconditions.CheckNotNull(byteArray);
                Preconditions.CheckState(!Closed);
                int actualCount = AdjustByteCount(nativeMemoryOffset, count);
                CheckBounds(nativeMemoryOffset, byteArray.Length, byteArrayOffset, actualCount);
                NativeMethods.NativeCopyToByteArray(
                    _nativePtr + nativeMemoryOffset, byteArray, byteArrayOffset, actualCount);

                return actualCount;
            }
        }

        /// <summary>
        /// Read byte at given offset.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>Byte at given offset.</returns>
        public virtual byte Read(int offset)
        {
            lock (_memoryChunkGate)
            {
                Preconditions.CheckState(!Closed);
                Preconditions.CheckArgument(offset >= 0);
                Preconditions.CheckArgument(offset < Size);
                return NativeMethods.NativeReadByte(_nativePtr + offset);
            }
        }

        /// <summary>
        /// Copy bytes from native memory wrapped by this
        /// NativeMemoryChunk instance to native memory wrapped
        /// by other NativeMemoryChunk.
        /// </summary>
        /// <param name="offset">
        /// Number of first byte to copy.
        /// </param>
        /// <param name="other">
        /// Other NativeMemoryChunk to copy to.
        /// </param>
        /// <param name="otherOffset">
        /// Number of first byte to write to.
        /// </param>
        /// <param name="count">Number of bytes to copy.</param>
        public virtual void Copy(
            int offset,
            NativeMemoryChunk other,
            int otherOffset,
            int count)
        {
            Preconditions.CheckNotNull(other);

            // This implementation acquires locks on this and other objects
            // and then delegates to DoCopy which does actual copy. In order
            // to avoid deadlocks we have to establish some linear order on
            // all NativeMemoryChunks and acquire locks according to this
            // order. Fortunately we can use _nativePtr for that purpose.
            // So we have to address 3 cases:

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
        /// Gets the native pointer.
        /// </summary>
        /// <returns>The native pointer.</returns>
        public long GetNativePtr()
        {
            return _nativePtr;
        }

        /// <summary>
        /// This does actual copy. It should be called only when we
        /// hold locks on both this and other objects.
        /// </summary>
        private void DoCopy(
            int offset,
            NativeMemoryChunk other,
            int otherOffset,
            int count)
        {
            Preconditions.CheckState(!Closed);
            Preconditions.CheckState(!other.Closed);
            CheckBounds(offset, other.Size, otherOffset, count);
            NativeMethods.NativeMemcpy(
                other._nativePtr + otherOffset, _nativePtr + offset, count);
        }

        /// <summary>
        /// Computes number of bytes that can be safely read/written
        /// starting at given offset, but no more than count.
        /// </summary>
        private int AdjustByteCount(int offset, int count)
        {
            int available = Math.Max(0, Size - offset);
            return Math.Min(available, count);
        }

        /// <summary>
        /// Check that copy/read/write operation won't access memory
        /// it should not.
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
