using System;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// A 'pooled' byte-buffer abstraction. Represents an immutable sequence of bytes stored 
    /// off the memory.
    /// </summary>
    public interface IPooledByteBuffer : IDisposable
    {
        /// <summary>
        /// Get the size of the byte buffer.
        /// @return the size of the byte buffer.
        /// </summary>
        int Size { get;  }

        /// <summary>
        /// Read byte at given offset.
        /// <param name="offset"></param>
        /// @return byte at given offset.
        /// </summary>
        byte Read(int offset);

        /// <summary>
        /// Read consecutive bytes.
        ///
        /// <param name="offset">The position in the IPooledByteBuffer of the 
        /// first byte to read.</param>
        /// <param name="buffer">The byte array where read bytes will be copied to.</param>
        /// <param name="bufferOffset">The position within the buffer of the first 
        /// copied byte.</param>
        /// <param name="length">Number of bytes to copy.</param>
        /// @return number of bytes copied.
        /// </summary>
        void Read(int offset, byte[] buffer, int bufferOffset, int length);

        /// <summary>
        /// @return pointer to native memory backing this buffer.
        /// </summary>
        long GetNativePtr();

        /// <summary>
        /// Check if this instance has already been closed.
        /// @return true, if the instance has been closed.
        /// </summary>
        bool IsClosed { get; }
    }
}
