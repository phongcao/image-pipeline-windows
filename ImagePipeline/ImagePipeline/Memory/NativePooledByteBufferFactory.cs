using FBCore.Common.Internal;
using FBCore.Common.References;
using ImagePipelineBase.ImagePipeline.Memory;
using System.IO;

namespace ImagePipeline.Memory
{
    /// <summary>
   /// A factory to provide instances of <see cref="NativePooledByteBuffer"/> and
   /// <see cref="NativePooledByteBufferOutputStream"/>
   /// </summary>
    public class NativePooledByteBufferFactory : IPooledByteBufferFactory
    {
        private readonly PooledByteStreams _pooledByteStreams;

        /// <summary>
        /// Native memory pool
        /// </summary>
        private readonly NativeMemoryChunkPool _pool;

        /// <summary>
        /// Instantiates the <see cref="NativePooledByteBufferFactory"/>.
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="pooledByteStreams"></param>
        public NativePooledByteBufferFactory(
            NativeMemoryChunkPool pool,
            PooledByteStreams pooledByteStreams)
        {
            _pool = pool;
            _pooledByteStreams = pooledByteStreams;
        }

        /// <summary>
        /// Creates a new PooledByteBuffer instance of given size.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
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
        /// Creates a new NativePooledByteBuffer instance by reading in the entire contents of the
        /// input stream
        /// <param name="inputStream">the input stream to read from</param>
        /// @return an instance of the NativePooledByteBuffer
        /// @throws IOException
        /// </summary>
        public IPooledByteBuffer NewByteBuffer(Stream inputStream)
        {
            NativePooledByteBufferOutputStream outputStream = new NativePooledByteBufferOutputStream(_pool);
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
       /// Creates a new NativePooledByteBuffer instance by reading in the entire contents of the
       /// byte array
       /// <param name="bytes">the byte array to read from</param>
       /// @return an instance of the NativePooledByteBuffer
       /// </summary>
        public IPooledByteBuffer NewByteBuffer(byte[] bytes)
        {
            NativePooledByteBufferOutputStream outputStream = new NativePooledByteBufferOutputStream(
                _pool, bytes.Length);

            try
            {
                outputStream.Write(bytes, 0, bytes.Length);
                return outputStream.ToByteBuffer();
            }
            catch (IOException ioe)
            {
                throw ioe;
            }
            finally
            {
                outputStream.Dispose();
            }
        }

        /// <summary>
        /// Creates a new NativePooledByteBuffer instance with an initial capacity, and reading the entire
        /// contents of the input stream
        /// <param name="inputStream">the input stream to read from</param>
        /// <param name="initialCapacity">initial allocation size for the PooledByteBuffer</param>
        /// @return an instance of NativePooledByteBuffer
        /// @throws IOException
        /// </summary>
        public IPooledByteBuffer NewByteBuffer(Stream inputStream, int initialCapacity)
        {
            NativePooledByteBufferOutputStream outputStream = new NativePooledByteBufferOutputStream(
                _pool, initialCapacity);
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
       /// Reads all bytes from inputStream and writes them to outputStream. When all bytes
       /// are read outputStream.toByteBuffer is called and obtained NativePooledByteBuffer is returned
       /// <param name="inputStream">the input stream to read from</param>
       /// <param name="outputStream">output stream used to transform content of input stream to</param>
       ///   NativePooledByteBuffer
       /// @return an instance of NativePooledByteBuffer
       /// @throws IOException
       /// </summary>
        internal IPooledByteBuffer NewByteBuf(
            Stream inputStream,
            NativePooledByteBufferOutputStream outputStream)
        {
            _pooledByteStreams.Copy(inputStream, outputStream);
            return outputStream.ToByteBuffer();
        }

        /// <summary>
        /// Creates a new NativePooledByteBufferOutputStream instance with default initial capacity
        /// @return a new NativePooledByteBufferOutputStream
        /// </summary>
        public PooledByteBufferOutputStream NewOutputStream()
        {
            return new NativePooledByteBufferOutputStream(_pool);
        }

        /// <summary>
        /// Creates a new NativePooledByteBufferOutputStream instance with the specified initial capacity
        /// <param name="initialCapacity">initial allocation size for the underlying output stream</param>
        /// @return a new NativePooledByteBufferOutputStream
        /// </summary>
        public PooledByteBufferOutputStream NewOutputStream(int initialCapacity)
        {
            return new NativePooledByteBufferOutputStream(_pool, initialCapacity);
        }
    }
}
