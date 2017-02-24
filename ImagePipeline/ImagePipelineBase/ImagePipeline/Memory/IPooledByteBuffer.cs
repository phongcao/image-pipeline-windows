using System;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// A 'pooled' byte-buffer abstraction. Represents an immutable sequence
    /// of bytes stored off the memory.
    /// </summary>
    public interface IPooledByteBuffer : IDisposable
    {
        /// <summary>
        /// Get the size of the byte buffer.
        /// </summary>
        /// <returns>The size of the byte buffer.</returns>
        int Size { get;  }

        /// <summary>
        /// Read byte at given offset.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>Byte at given offset.</returns>
        byte Read(int offset);

        /// <summary>
        /// Read consecutive bytes.
        /// </summary>
        /// <param name="offset">
        /// The position in the IPooledByteBuffer of the first byte to read.
        /// </param>
        /// <param name="buffer">
        /// The byte array where read bytes will be copied to.
        /// </param>
        /// <param name="bufferOffset">
        /// The position within the buffer of the first copied byte.
        /// </param>
        /// <param name="length">Number of bytes to copy.</param>
        /// <returns>Number of bytes copied.</returns>
        void Read(int offset, byte[] buffer, int bufferOffset, int length);

        /// <summary>
        /// Gets the pointer to native memory backing this buffer.
        /// </summary>
        /// <returns>
        /// Pointer to native memory backing this buffer.
        /// </returns>
        long GetNativePtr();

        /// <summary>
        /// Check if this instance has already been closed.
        /// </summary>
        /// <returns>
        /// true, if the instance has been closed.
        /// </returns>
        bool IsClosed { get; }
    }
}
