using System.IO;

namespace ImagePipelineBase.ImagePipeline.Memory
{
    /// <summary>
   /// A factory to create instances of IPooledByteBuffer and PooledByteBufferOutputStream
   /// </summary>
    public interface IPooledByteBufferFactory
    {
        /// <summary>
        /// Creates a new PooledByteBuffer instance of given size.
        /// <param name="size">in bytes</param>
        /// @return an instance of PooledByteBuffer
        /// </summary>
        IPooledByteBuffer NewByteBuffer(int size);

        /// <summary>
        /// Creates a new bytebuf instance by reading in the entire contents of the input stream
        /// <param name="inputStream">the input stream to read from</param>
        /// @return an instance of the PooledByteBuffer
        /// @throws IOException
        /// </summary>
        IPooledByteBuffer NewByteBuffer(Stream inputStream);

        /// <summary>
        /// Creates a new bytebuf instance by reading in the entire contents of the byte array
        /// <param name="bytes">the byte array to read from</param>
        /// @return an instance of the PooledByteBuffer
        /// </summary>
        IPooledByteBuffer NewByteBuffer(byte[] bytes);

        /// <summary>
        /// Creates a new PooledByteBuffer instance with an initial capacity, and reading the entire
        /// contents of the input stream
        /// <param name="inputStream">the input stream to read from</param>
        /// <param name="initialCapacity">initial allocation size for the bytebuf</param>
        /// @return an instance of PooledByteBuffer
        /// @throws IOException
        /// </summary>
        IPooledByteBuffer NewByteBuffer(Stream inputStream, int initialCapacity);

        /// <summary>
        /// Creates a new PooledByteBufferOutputStream instance with default initial capacity
        /// @return a new PooledByteBufferOutputStream
        /// </summary>
        PooledByteBufferOutputStream NewOutputStream();

        /// <summary>
        /// Creates a new PooledByteBufferOutputStream instance with the specified initial capacity
        /// <param name="initialCapacity">initial allocation size for the underlying output stream</param>
        /// @return a new PooledByteBufferOutputStream
        /// </summary>
        PooledByteBufferOutputStream NewOutputStream(int initialCapacity);
    }
}
