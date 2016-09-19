using System.IO;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// An OutputStream that produces a PooledByteBuffer.
    ///
    /// <para /> Expected use for such stream is to first write sequence of bytes to the stream and then call
    /// ToByteBuffer to produce PooledByteBuffer containing written data. After ToByteBuffer returns
    /// client can continue writing new data and call ToByteBuffer over and over again.
    ///
    /// <para /> Streams implementing this interface are closeable resources and need to be closed in order
    /// to release underlying resources. Dispose is idempotent operation and after stream was closed, no
    /// other method should be called. Streams subclassing PooledByteBufferOutputStream are not allowed
    /// to throw IOException from Dispose method.
    /// </summary>
    public abstract class PooledByteBufferOutputStream : Stream
    {
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
                return false;
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
                return true;
            }
        }

        /// <summary>
        /// Creates a PooledByteBuffer from the contents of the stream.
        /// @return
        /// </summary>
        public abstract IPooledByteBuffer ToByteBuffer();

        /// <summary>
        /// Returns the total number of bytes written to this stream so far.
        /// @return the number of bytes written to this stream.
        /// </summary>
        public abstract int Size { get;  }
    }
}
