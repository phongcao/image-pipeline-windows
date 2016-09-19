using FBCore.Common.Internal;
using FBCore.Common.References;
using System;
using System.Diagnostics;
using System.IO;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// InputStream that wraps another input stream and buffers all reads.
    ///
    /// <para /> For purpose of buffering a byte array is used. It is provided during construction time
    /// together with ResourceReleaser responsible for releasing it when the stream is closed.
    /// </summary>
    public class PooledByteArrayBufferedInputStream : Stream
    {
        private readonly Stream _inputStream;
        private byte[] _byteArray;
        private readonly IResourceReleaser<byte[]> _resourceReleaser;

        /// <summary>
        /// How many bytes in mByteArray were set by last call to _inputStream.Read
        /// </summary>
        private int _bufferedSize;

        /// <summary>
        /// Position of next buffered byte in mByteArray to be read
        ///
        /// <![CDATA[ invariant: 0 <= _bufferOffset <= _bufferedSize ]]>
        /// </summary>
        private int _bufferOffset;

        private bool _closed;

        /// <summary>
        /// Instantiates the <see cref="PooledByteArrayBufferedInputStream"/>.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="byteArray"></param>
        /// <param name="resourceReleaser"></param>
        public PooledByteArrayBufferedInputStream(
            Stream inputStream,
            byte[] byteArray,
            IResourceReleaser<byte[]> resourceReleaser)
        {
            _inputStream = Preconditions.CheckNotNull(inputStream);
            _byteArray = Preconditions.CheckNotNull(byteArray);
            _resourceReleaser = Preconditions.CheckNotNull(resourceReleaser);
            _bufferedSize = 0;
            _bufferOffset = 0;
            _closed = false;
        }

        /// <summary>
        /// Releases all resources
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!_closed)
            {
                _closed = true;
                _resourceReleaser.Release(_byteArray);
            }
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        ~PooledByteArrayBufferedInputStream()
        {
            if (!_closed)
            {
                Debug.WriteLine("Finalized without closing");
                Dispose(false);
            }
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
                Preconditions.CheckState(_bufferOffset <= _bufferedSize);
                EnsureNotClosed();
                return _bufferedSize - _bufferOffset + _inputStream.Length;
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
                return _inputStream.Position - _bufferedSize + _bufferOffset;
            }

            set
            {
                _inputStream.Position = value;
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
            Preconditions.CheckState(_bufferOffset <= _bufferedSize);
            EnsureNotClosed();
            if (!EnsureDataInBuffer())
            {
                return 0;
            }

            int bytesToRead = Math.Min(_bufferedSize - _bufferOffset, count);
            Array.Copy(_byteArray, _bufferOffset, buffer, offset, bytesToRead);
            _bufferOffset += bytesToRead;
            return bytesToRead;
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

            Preconditions.CheckState(_bufferOffset <= _bufferedSize);
            EnsureNotClosed();
            long originOffset = Position;
            if (origin == SeekOrigin.Begin)
            {
                originOffset = 0;
            }
            else if (origin == SeekOrigin.End)
            {
                originOffset = Length;
            }

            long newOffset = originOffset + offset;
            newOffset = Math.Min(newOffset, Length);
            newOffset = Math.Max(newOffset, 0);
            if ((newOffset < _inputStream.Position - _bufferedSize) || (newOffset > _inputStream.Position))
            {
                _bufferOffset = _bufferedSize;
                Position = newOffset;
            }
            else
            {
                _bufferOffset += (int)(newOffset - originOffset);
            }

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

        /// <summary>
        /// Checks if there is some data left in the buffer. If not but buffered stream still has some
        /// data to be read, then more data is buffered.
        ///
        /// @return false if and only if there is no more data and underlying input stream has no more data
        ///   to be read
        /// @throws IOException
        /// </summary>
        private bool EnsureDataInBuffer()
        {
            if (_bufferOffset < _bufferedSize)
            {
                return true;
            }

            int readData = _inputStream.Read(_byteArray, 0, _byteArray.Length);
            if (readData <= 0)
            {
                return false;
            }

            _bufferedSize = readData;
            _bufferOffset = 0;
            return true;
        }

        private void EnsureNotClosed()
        {
            if (_closed)
            {
                throw new IOException("stream already closed");
            }
        }
    }
}
