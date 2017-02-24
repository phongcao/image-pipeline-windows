using System.IO;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// A factory to create instances of <see cref="IPooledByteBuffer"/>
    /// and <see cref="PooledByteBufferOutputStream"/>.
    /// </summary>
    public interface IPooledByteBufferFactory
    {
        /// <summary>
        /// Creates a new PooledByteBuffer instance of given size.
        /// </summary>
        /// <param name="size">Size in bytes.</param>
        /// <returns>An instance of IPooledByteBuffer.</returns>
        IPooledByteBuffer NewByteBuffer(int size);

        /// <summary>
        /// Creates a new bytebuf instance by reading in the entire contents
        /// of the input stream.
        /// </summary>
        /// <param name="inputStream">The input stream to read from.</param>
        /// <returns>An instance of the IPooledByteBuffer.</returns>
        IPooledByteBuffer NewByteBuffer(Stream inputStream);

        /// <summary>
        /// Creates a new bytebuf instance by reading in the entire contents
        /// of the byte array.
        /// <param name="bytes">The byte array to read from.</param>
        /// <returns>an instance of the IPooledByteBuffer.</returns>
        /// </summary>
        IPooledByteBuffer NewByteBuffer(byte[] bytes);

        /// <summary>
        /// Creates a new PooledByteBuffer instance with an initial capacity,
        /// and reading the entire contents of the input stream.
        /// </summary>
        /// <param name="inputStream">The input stream to read from.</param>
        /// <param name="initialCapacity">
        /// Initial allocation size for the bytebuf.
        /// </param>
        /// <returns>An instance of IPooledByteBuffer.</returns>
        IPooledByteBuffer NewByteBuffer(Stream inputStream, int initialCapacity);

        /// <summary>
        /// Creates a new PooledByteBufferOutputStream instance with
        /// default initial capacity.
        /// </summary>
        /// <returns>A new PooledByteBufferOutputStream.</returns>
        PooledByteBufferOutputStream NewOutputStream();

        /// <summary>
        /// Creates a new PooledByteBufferOutputStream instance with the
        /// specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">
        /// Initial allocation size for the underlying output stream.
        /// </param>
        /// <returns>A new PooledByteBufferOutputStream.</returns>
        PooledByteBufferOutputStream NewOutputStream(int initialCapacity);
    }
}
