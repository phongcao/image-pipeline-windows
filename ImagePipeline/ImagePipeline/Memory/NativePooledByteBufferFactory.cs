using FBCore.Common.Internal;
using FBCore.Common.References;
using System.IO;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// A factory to provide instances of <see cref="NativePooledByteBuffer"/>
    /// and <see cref="NativePooledByteBufferOutputStream"/>.
    /// </summary>
    public class NativePooledByteBufferFactory : IPooledByteBufferFactory
    {
        private readonly PooledByteStreams _pooledByteStreams;

        /// <summary>
        /// Native memory pool.
        /// </summary>
        private readonly NativeMemoryChunkPool _pool;

        /// <summary>
        /// Instantiates the <see cref="NativePooledByteBufferFactory"/>.
        /// </summary>
        public NativePooledByteBufferFactory(
            NativeMemoryChunkPool pool,
            PooledByteStreams pooledByteStreams)
        {
            _pool = pool;
            _pooledByteStreams = pooledByteStreams;
        }

        /// <summary>
        /// Creates a new IPooledByteBuffer instance of given size.
        /// </summary>
        public IPooledByteBuffer NewByteBuffer(int size)
        {
            Preconditions.CheckArgument(size > 0);
            var chunkRef = CloseableReference<NativeMemoryChunk>.of(_pool.Get(size), _pool);
            try
            {
                return new NativePooledByteBuffer(chunkRef, size);
            }
            finally
            {
                chunkRef.Dispose();
            }
        }

        /// <summary>
        /// Creates a new NativePooledByteBuffer instance by reading
        /// in the entire contents of the input stream.
        /// </summary>
        /// <param name="inputStream">
        /// The input stream to read from.
        /// </param>
        /// <returns>
        /// An instance of the NativePooledByteBuffer.
        /// </returns>
        /// <exception cref="IOException">
        /// An I/O error occurs.
        /// </exception>
        public IPooledByteBuffer NewByteBuffer(Stream inputStream)
        {
            NativePooledByteBufferOutputStream outputStream = 
                new NativePooledByteBufferOutputStream(_pool);

            try
            {
                return NewByteBuf(inputStream, outputStream);
            }
            finally
            {
                outputStream.Dispose();
            }
        }

        /// <summary>
        /// Creates a new NativePooledByteBuffer instance by reading
        /// in the entire contents of the byte array.
        /// </summary>
        /// <param name="bytes">
        /// The byte array to read from.
        /// </param>
        /// <returns>
        /// An instance of the NativePooledByteBuffer.
        /// </returns>
        public IPooledByteBuffer NewByteBuffer(byte[] bytes)
        {
            NativePooledByteBufferOutputStream outputStream = 
                new NativePooledByteBufferOutputStream(_pool, bytes.Length);

            try
            {
                outputStream.Write(bytes, 0, bytes.Length);
                return outputStream.ToByteBuffer();
            }
            catch (IOException)
            {
                throw;
            }
            finally
            {
                outputStream.Dispose();
            }
        }

        /// <summary>
        /// Creates a new NativePooledByteBuffer instance with an
        /// initial capacity, and reading the entire contents of
        /// the input stream.
        /// </summary>
        /// <param name="inputStream">
        /// The input stream to read from.
        /// </param>
        /// <param name="initialCapacity">
        /// Initial allocation size for the IPooledByteBuffer.
        /// </param>
        /// <returns>
        /// An instance of NativePooledByteBuffer.
        /// </returns>
        /// <exception cref="IOException">
        /// An I/O error occurs.
        /// </exception>
        public IPooledByteBuffer NewByteBuffer(Stream inputStream, int initialCapacity)
        {
            NativePooledByteBufferOutputStream outputStream = 
                new NativePooledByteBufferOutputStream(_pool, initialCapacity);

            try
            {
                return NewByteBuf(inputStream, outputStream);
            }
            finally
            {
                outputStream.Dispose();
            }
        }

        /// <summary>
        /// Reads all bytes from inputStream and writes them to
        /// outputStream. When all bytes are read, 
        /// outputStream.ToByteBuffer is called and obtained
        /// NativePooledByteBuffer is returned.
        /// </summary>
        /// <param name="inputStream">
        /// The input stream to read from.
        /// </param>
        /// <param name="outputStream">
        /// The output stream used to transform content of input 
        /// stream to NativePooledByteBuffer.
        /// </param>
        /// <returns>
        /// An instance of NativePooledByteBuffer.
        /// </returns>
        /// <exception cref="IOException">
        /// An I/O error occurs.
        /// </exception>
        internal IPooledByteBuffer NewByteBuf(
            Stream inputStream,
            NativePooledByteBufferOutputStream outputStream)
        {
            _pooledByteStreams.Copy(inputStream, outputStream);
            return outputStream.ToByteBuffer();
        }

        /// <summary>
        /// Creates a new NativePooledByteBufferOutputStream instance
        /// with default initial capacity.
        /// </summary>
        /// <returns>
        /// A new NativePooledByteBufferOutputStream.
        /// </returns>
        public PooledByteBufferOutputStream NewOutputStream()
        {
            return new NativePooledByteBufferOutputStream(_pool);
        }

        /// <summary>
        /// Creates a new NativePooledByteBufferOutputStream instance
        /// with the specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">
        /// Initial allocation size for the underlying output stream.
        /// </param>
        /// <returns>
        /// A new NativePooledByteBufferOutputStream.
        /// </returns>
        public PooledByteBufferOutputStream NewOutputStream(int initialCapacity)
        {
            return new NativePooledByteBufferOutputStream(_pool, initialCapacity);
        }
    }
}
