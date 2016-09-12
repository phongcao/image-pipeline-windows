using FBCore.Common.Internal;
using System;
using System.IO;

namespace ImagePipelineBase.ImagePipeline.Memory
{
    /// <summary>
    /// An InputStream implementation over a <see cref="IPooledByteBuffer"/> instance
    /// </summary>
    public class PooledByteBufferInputStream : Stream
    {
        internal readonly IPooledByteBuffer _pooledByteBuffer;

        /// <summary>
        /// Current offset in the chunk
        /// </summary>
        internal int _currentOffset;

        /// <summary>
        /// Creates a new inputstream instance over the specific buffer.
        /// <param name="pooledByteBuffer">the buffer to read from</param>
        /// </summary>
        public PooledByteBufferInputStream(IPooledByteBuffer pooledByteBuffer)
        {
            Preconditions.CheckArgument(!pooledByteBuffer.IsClosed());
            _pooledByteBuffer = Preconditions.CheckNotNull(pooledByteBuffer);
            _currentOffset = 0;
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current
        /// stream supports reading.
        /// 
        /// Returns:
        ///     true if the stream supports reading; otherwise, false.
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current
        /// stream supports seeking.
        /// 
        /// Returns:
        ///     true if the stream supports seeking; otherwise, false.
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current
        ///     stream supports writing.
        ///     
        /// Returns:
        ///     true if the stream supports writing; otherwise, false.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// 
        /// Returns:
        ///     A long value representing the length of the stream in bytes.
        ///     
        /// Exceptions:
        ///     T:System.NotSupportedException:
        ///         A class derived from Stream does not support seeking.
        ///         
        ///     T:System.ObjectDisposedException:
        ///         Methods were called after the stream was closed.
        /// </summary>
        public override long Length
        {
            get
            {
                return _pooledByteBuffer.Size;
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets or sets the position within the current
        /// stream.
        /// 
        /// Returns:
        ///     The current position within the stream.
        ///     
        /// Exceptions:
        ///     T:System.IO.IOException:
        ///         An I/O error occurs.
        ///         
        ///     T:System.NotSupportedException:
        ///         The stream does not support seeking.
        ///         
        ///     T:System.ObjectDisposedException:
        ///         Methods were called after the stream was closed.
        /// </summary>
        public override long Position
        {
            get
            {
                return _currentOffset;
            }

            set
            {
                _currentOffset = (int)value;
            }
        }

        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and causes
        /// any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// When overridden in a derived class, reads a sequence of bytes from the current
        /// stream and advances the position within the stream by the number of bytes read.
        /// 
        /// Exceptions:
        ///     T:System.ArgumentException:
        ///         The sum of offset and count is larger than the buffer length.
        ///       
        ///     T:System.ArgumentNullException:
        ///         buffer is null.
        ///     
        ///     T:System.ArgumentOutOfRangeException:
        ///         offset or count is negative.
        ///         
        ///     T:System.IO.IOException:
        ///         An I/O error occurs.
        ///         
        ///     T:System.NotSupportedException:
        ///         The stream does not support reading.
        ///         
        ///     T:System.ObjectDisposedException:
        ///         Methods were called after the stream was closed.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer 
        /// contains the specified byte array with the values between offset and 
        /// (offset + count - 1) replaced by the bytes read
        ///  from the current source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read
        /// from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number
        /// of bytes requested if that many bytes are not currently available, or zero (0)
        /// if the end of the stream has been reached.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException();
            }

            if (offset < 0 || count < 0 || offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException($"length={ buffer.Length }; regionStart={ offset }; regionLength={ count }");
            }

            int available = _pooledByteBuffer.Size - _currentOffset;
            if (available <= 0 || count <= 0)
            {
                return 0;
            }

            int numToRead = Math.Min(available, count);
            _pooledByteBuffer.Read(_currentOffset, buffer, offset, numToRead);
            _currentOffset += numToRead;
            return numToRead;
        }

        /// <summary>
        /// When overridden in a derived class, sets the position within the current stream.
        /// 
        /// Exceptions:
        ///   T:System.ArgumentOutOfRangeException:
        ///     Offset is greater than MaxValue.
        ///
        ///   T:System.NotSupportedException:
        ///     The stream does not support seeking, such as if the stream is constructed from
        ///     a pipe or console output.
        ///
        ///   T:System.ObjectDisposedException:
        ///     Methods were called after the stream was closed.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type System.IO.SeekOrigin indicating the reference point used to 
        /// obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (offset > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException();
            }

            int size = _pooledByteBuffer.Size;
            int originOffset = _currentOffset;
            if (origin == SeekOrigin.Begin)
            {
                originOffset = 0;
            }
            else if (origin == SeekOrigin.End)
            {
                originOffset = _pooledByteBuffer.Size;
            }

            long newOffset = originOffset + offset;
            newOffset = Math.Min(newOffset, _pooledByteBuffer.Size);
            newOffset = Math.Max(newOffset, 0);
            Position = newOffset;

            return newOffset;
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// 
        /// Exceptions:
        ///   T:System.IO.IOException:
        ///     An I/O error occurs.
        ///
        ///   T:System.NotSupportedException:
        ///     The stream does not support both writing and seeking, such as if the stream is
        ///     constructed from a pipe or console output.
        ///
        ///   T:System.ObjectDisposedException:
        ///     Methods were called after the stream was closed.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the current
        /// stream and advances the current position within this stream by the number of
        /// bytes written.
        /// 
        /// Exceptions:
        ///   T:System.ArgumentException:
        ///     The sum of offset and count is greater than the buffer length.
        ///
        ///   T:System.ArgumentNullException:
        ///     buffer is null.
        ///
        ///   T:System.ArgumentOutOfRangeException:
        ///     offset or count is negative.
        ///
        ///   T:System.IO.IOException:
        ///     An I/O error occured, such as the specified file cannot be found.
        ///
        ///   T:System.NotSupportedException:
        ///     The stream does not support writing.
        ///
        ///   T:System.ObjectDisposedException:
        ///     System.IO.Stream.Write(System.Byte[],System.Int32,System.Int32) was called after
        ///     the stream was closed.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from 
        /// buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin 
        /// copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
