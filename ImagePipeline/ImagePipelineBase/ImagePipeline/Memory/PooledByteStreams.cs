using FBCore.Common.Internal;
using FBCore.Common.Util;
using System;
using System.IO;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// Helper class for interacting with streams.
    /// To prevent numerous allocations of temp buffers pool of byte arrays
    /// is used.
    /// </summary>
    public class PooledByteStreams
    {
        /// <summary>
        /// Size of temporary buffer to use for copying (16 kb).
        /// </summary>
        private const int DEFAULT_TEMP_BUF_SIZE = 16 * ByteConstants.KB;

        private readonly int _tempBufSize;
        private readonly IByteArrayPool _byteArrayPool;

        /// <summary>
        /// Instantiates the <see cref="PooledByteStreams"/>.
        /// </summary>
        public PooledByteStreams(IByteArrayPool byteArrayPool) : 
            this(byteArrayPool, DEFAULT_TEMP_BUF_SIZE)
        {
        }

        /// <summary>
        /// Instantiates the <see cref="PooledByteStreams"/>.
        /// </summary>
        public PooledByteStreams(IByteArrayPool byteArrayPool, int tempBufSize)
        {
            Preconditions.CheckArgument(tempBufSize > 0);
            _tempBufSize = tempBufSize;
            _byteArrayPool = byteArrayPool;
        }

        /// <summary>
        /// Copy all bytes from the input stream to the output stream.
        /// </summary>
        /// <param name="from">The intput stream.</param>
        /// <param name="to">The output stream.</param>
        /// <returns>Number of copied bytes.</returns>
        /// <exception cref="IOException">
        /// An I/O error occurs.
        /// </exception>
        public long Copy(Stream from, Stream to)
        {
            long count = 0;
            byte[] tmp = _byteArrayPool.Get(_tempBufSize);

            try
            {
                while (true)
                {
                    int read = from.Read(tmp, 0, _tempBufSize);
                    if (read == 0)
                    {
                        return count;
                    }

                    to.Write(tmp, 0, read);
                    count += read;
                }
            }
            finally
            {
                _byteArrayPool.Release(tmp);
            }
        }

        /// <summary>
        /// Copy at most number of bytes from the input stream to the
        /// output stream.
        /// </summary>
        /// <param name="from">The input stream.</param>
        /// <param name="to">The output stream.</param>
        /// <param name="bytesToCopy">Bytes to copy.</param>
        /// <returns>Number of copied bytes.</returns>
        /// <exception cref="IOException">
        /// An I/O error occurs.
        /// </exception>
        public long Copy(Stream from, Stream to, long bytesToCopy)
        {
            Preconditions.CheckState(bytesToCopy > 0);
            long copied = 0;
            byte[]
            tmp = _byteArrayPool.Get(_tempBufSize);

            try
            {
                while (copied < bytesToCopy)
                {
                    int read = from.Read(tmp, 0, (int)Math.Min(_tempBufSize, bytesToCopy - copied));
                    if (read == -1)
                    {
                        return copied;
                    }

                    to.Write(tmp, 0, read);
                    copied += read;
                }

                return copied;
            }
            finally
            {
                _byteArrayPool.Release(tmp);
            }
        }
    }
}
