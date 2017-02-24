using System.IO;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// An Stream that produces a IPooledByteBuffer.
    ///
    /// <para />Expected use for such stream is to first write sequence
    /// of bytes to the stream and  then call ToByteBuffer to produce
    /// IPooledByteBuffer containing written data. After ToByteBuffer
    /// returns client can continue writing new data and call
    /// ToByteBuffer over and over again.
    ///
    /// <para />Streams implementing this interface are IDisposable
    /// resources and need to be closed in order to release underlying
    /// resources. Dispose is idempotent operation and after stream was
    /// closed, no other method should be called. Streams subclassing
    /// PooledByteBufferOutputStream are not allowed to throw IOException
    /// from Dispose method.
    /// </summary>
    public abstract class PooledByteBufferOutputStream : Stream
    {
        /// <summary>
        /// When overridden in a derived class, gets a value indicating
        /// whether the current stream supports reading.
        /// </summary>
        /// <returns>
        /// true if the stream supports reading; otherwise, false.
        /// </returns>
        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating
        /// whether the current stream supports seeking.
        /// </summary>
        /// <returns>
        /// true if the stream supports seeking; otherwise, false.
        /// </returns>
        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating
        /// whether the current stream supports writing.
        /// </summary>
        /// <returns>
        /// true if the stream supports writing; otherwise, false.
        /// </returns>
        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Creates a IPooledByteBuffer from the contents of the stream.
        /// </summary>
        public abstract IPooledByteBuffer ToByteBuffer();

        /// <summary>
        /// Returns the total number of bytes written to this stream so far.
        /// </summary>
        /// <returns>The number of bytes written to this stream.</returns>
        public abstract int Size { get;  }
    }
}
