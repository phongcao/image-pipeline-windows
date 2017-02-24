using System;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// A trivial implementation of <see cref="IPooledByteBuffer"/>.
    /// </summary>
    public sealed class TrivialPooledByteBuffer : IPooledByteBuffer
    {
        private byte[] _buf;
        private long _nativePtr;

        /// <summary>
        /// Instantiates the <see cref="TrivialPooledByteBuffer"/>.
        /// </summary>
        public TrivialPooledByteBuffer(byte[] buf) : this(buf, 0L)
        {
        }

        /// <summary>
        /// Instantiates the <see cref="TrivialPooledByteBuffer"/>.
        /// </summary>
        public TrivialPooledByteBuffer(byte[] buf, long nativePtr)
        {
            _buf = buf;
            _nativePtr = nativePtr;
        }

        /// <summary>
        /// Get the size of the byte buffer.
        /// </summary>
        /// <returns>The size of the byte buffer.</returns>
        public int Size
        {
            get
            {
                return IsClosed ? -1 : _buf.Length;
            }
        }

        /// <summary>
        /// Read byte at given offset.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>Byte at given offset.</returns>
        public byte Read(int offset)
        {
            return _buf[offset];
        }

        /// <summary>
        /// Read consecutive bytes.
        /// </summary>
        /// <param name="offset">
        /// The position in the PooledByteBuffer of the first byte to read.
        /// </param>
        /// <param name="buffer">
        /// The byte array where read bytes will be copied to.
        /// </param>
        /// <param name="bufferOffset">
        /// The position within the buffer of the first copied byte.
        /// </param>
        /// <param name="length">Number of bytes to copy.</param>
        /// <returns>Number of bytes copied.</returns>
        public void Read(int offset, byte[] buffer, int bufferOffset, int length)
        {
            Array.Copy(_buf, offset, buffer, bufferOffset, length);
        }

        /// <summary>
        /// Gets the pointer to native memory backing this buffer.
        /// </summary>
        /// <returns>
        /// Pointer to native memory backing this buffer.
        /// </returns>
        public long GetNativePtr()
        {
            return _nativePtr;
        }

        /// <summary>
        /// Check if this instance has already been closed.
        /// </summary>
        /// <returns>true, if the instance has been closed.</returns>
        public bool IsClosed
        {
            get
            {
                return _buf == null;
            }
        }

        /// <summary>
        /// Release all resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Release all resources.
        /// </summary>
        private void Dispose(bool disposing)
        {
            _buf = null;
        }
    }
}
