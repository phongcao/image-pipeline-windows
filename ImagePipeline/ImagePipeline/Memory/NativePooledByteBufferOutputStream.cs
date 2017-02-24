using FBCore.Common.Internal;
using FBCore.Common.References;
using System;
using System.IO;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// An implementation of <see cref="PooledByteBufferOutputStream"/>
    /// that produces a <see cref="NativePooledByteBuffer"/>.
    /// </summary>
    public class NativePooledByteBufferOutputStream : PooledByteBufferOutputStream
    {
        /// <summary>
        /// The pool to allocate memory chunks from.
        /// </summary>
        private readonly NativeMemoryChunkPool _pool;

        /// <summary>
        /// The current chunk that we're writing to.
        /// </summary>
        private CloseableReference<NativeMemoryChunk> _bufRef;

        /// <summary>
        /// Number of bytes 'used' in the current chunk.
        /// </summary>
        private int _count;

        /// <summary>
        /// Construct a new instance of this outputstream.
        /// </summary>
        /// <param name="pool">The pool to use.</param>
        public NativePooledByteBufferOutputStream(NativeMemoryChunkPool pool) : 
            this(pool, pool.GetMinBufferSize())
        {
        }

        /// <summary>
        /// Construct a new instance of this output stream with this
        /// initial capacity.
        /// It is not an error to have this initial capacity be inaccurate.
        /// If the actual contents end up being larger than the
        /// initialCapacity, then we will reallocate memory if needed.
        /// If the actual contents are smaller, then we'll end up wasting 
        /// some memory.
        /// </summary>
        /// <param name="pool">The pool to use.</param>
        /// <param name="initialCapacity">
        /// Initial capacity to allocate for this stream.
        /// </param>
        public NativePooledByteBufferOutputStream(
            NativeMemoryChunkPool pool, 
            int initialCapacity)
        {
            Preconditions.CheckArgument(initialCapacity > 0);
            _pool = Preconditions.CheckNotNull(pool);
            _count = 0;
            _bufRef = CloseableReference<NativeMemoryChunk>.of(_pool.Get(initialCapacity), _pool);
        }

        /// <summary>
        /// Gets a IPooledByteBuffer from the current contents.
        /// If the stream has already been closed, then an
        /// <see cref="InvalidStreamException"/> is thrown.
        /// </summary>
        /// <returns>
        /// An IPooledByteBuffer instance for the contents of
        /// the stream.
        /// </returns>
        /// <exception cref="InvalidStreamException">
        /// If the stream is invalid.
        /// </exception>
        public override IPooledByteBuffer ToByteBuffer()
        {
            EnsureValid();
            return new NativePooledByteBuffer(_bufRef, _count);
        }

        /// <summary>
        /// Returns the total number of bytes written to this stream so far.
        /// </summary>
        /// <returns>The number of bytes written to this stream.</returns>
        public override int Size
        {
            get
            {
                return _count;
            }
        }

        /// <summary>
        /// Not supported in the output stream.
        /// </summary>
        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Not supported in the output stream.
        /// </summary>
        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Writes <code>count</code> bytes from the byte array
        /// <code>buffer</code> starting  at position <code>offset</code>
        /// to this stream.
        /// The underlying stream MUST be valid.
        /// </summary>
        /// <param name="buffer">
        /// The source buffer to read from.
        /// </param>
        /// <param name="offset">
        /// The start position in <code>buffer</code> from where to
        /// get bytes.
        /// </param>
        /// <param name="count">
        /// The number of bytes from <code>buffer</code> to write to 
        /// this stream.
        /// </param>
        /// <exception cref="IOException">
        /// If an error occurs while writing to this stream.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// if <code>offset &lt; 0</code> or <code>count &lt; 0</code>,
        /// or if <code>offset + count</code> is bigger than the length
        /// of <code>buffer</code>.
        /// </exception>
        /// <exception cref="InvalidStreamException">
        /// If the stream is invalid.
        /// </exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(
                    $"length={ buffer.Length }; regionStart={ offset }; regionLength={ count }");
            }

            EnsureValid();
            Realloc(_count + count);
            _bufRef.Get().Write(_count, buffer, offset, count);
            _count += count;
        }

        /// <summary>
        /// Closes the stream. Owned resources are released back to the pool.
        /// It is not allowed to call ToByteBuffer after call to this method.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CloseableReference<NativeMemoryChunk>.CloseSafely(_bufRef);
            _bufRef = null;
            _count = -1;
        }

        /// <summary>
        /// Reallocate the local buffer to hold the new length specified.
        /// Also copy over existing data to this new buffer.
        /// </summary>
        /// <param name="newLength">New length of buffer.</param>
        /// <exception cref="InvalidStreamException">
        /// If the stream is invalid.
        /// </exception>
        /// <exception cref="SizeTooLargeException">
        /// If the allocation from the pool fails.
        /// </exception>
        internal void Realloc(int newLength)
        {
            EnsureValid();
            /* Can the buffer handle @i more bytes, if not expand it */
            if (newLength <= _bufRef.Get().Size)
            {
                return;
            }

            NativeMemoryChunk newbuf = _pool.Get(newLength);
            _bufRef.Get().Copy(0, newbuf, 0, _count);
            _bufRef.Dispose();
            _bufRef = CloseableReference<NativeMemoryChunk>.of(newbuf, _pool);
        }

        /// <summary>
        /// Ensure that the current stream is valid, that is underlying
        /// closeable reference is not null and is valid.
        /// </summary>
        /// <exception cref="InvalidStreamException">
        /// If the stream is invalid.
        /// </exception>
        private void EnsureValid()
        {
            if (!CloseableReference<NativeMemoryChunk>.IsValid(_bufRef))
            {
                throw new InvalidStreamException();
            }
        }

        /// <summary>
        /// Not supported in the output stream.
        /// </summary>
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported in the output stream.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported in the output stream.
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported in the output stream.
        /// </summary>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }
}
