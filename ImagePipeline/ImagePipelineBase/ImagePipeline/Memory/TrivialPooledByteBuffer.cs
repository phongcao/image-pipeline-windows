using System;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// A trivial implementation of <see cref="IPooledByteBuffer"/>
    /// </summary>
    public class TrivialPooledByteBuffer : IPooledByteBuffer
    {
        private byte[] _buf;
        private long _nativePtr;

        /// <summary>
        /// Instantiates the <see cref="TrivialPooledByteBuffer"/>.
        /// </summary>
        /// <param name="buf"></param>
        public TrivialPooledByteBuffer(byte[] buf) : this(buf, 0L)
        {
        }

        /// <summary>
        /// Instantiates the <see cref="TrivialPooledByteBuffer"/>.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="nativePtr"></param>
        public TrivialPooledByteBuffer(byte[] buf, long nativePtr)
        {
            _buf = buf;
            _nativePtr = nativePtr;
        }

        /// <summary>
        /// Get the size of the byte buffer
        /// @return the size of the byte buffer
        /// </summary>
        public int Size
        {
            get
            {
                return IsClosed() ? -1 : _buf.Length;
            }
        }

        /// <summary>
        /// Read byte at given offset
        /// <param name="offset"></param>
        /// @return byte at given offset
        /// </summary>
        public byte Read(int offset)
        {
            return _buf[offset];
        }

        /// <summary>
        /// Read consecutive bytes.
        ///
        /// <param name="offset">the position in the PooledByteBuffer of the first byte to read</param>
        /// <param name="buffer">the byte array where read bytes will be copied to</param>
        /// <param name="bufferOffset">the position within the buffer of the first copied byte</param>
        /// <param name="length">number of bytes to copy</param>
        /// @return number of bytes copied
        /// </summary>
        public void Read(int offset, byte[] buffer, int bufferOffset, int length)
        {
            Array.Copy(_buf, offset, buffer, bufferOffset, length);
        }

        /// <summary>
        /// @return pointer to native memory backing this buffer
        /// </summary>
        public long GetNativePtr()
        {
            return _nativePtr;
        }

        /// <summary>
        /// Check if this instance has already been closed
        /// @return true, if the instance has been closed
        /// </summary>
        public bool IsClosed()
        {
            return _buf == null;
        }

        /// <summary>
        /// Release all resources
        /// </summary>
        public void Dispose()
        {
            _buf = null;
        }
    }
}
