using System;
using System.IO;

namespace FBCore.Common.Internal
{
    /// <summary>
    /// Byte stream helper class.
    /// </summary>
    public sealed class ByteStreams
    {
        private const int BUF_SIZE = 0x1000; // 4K

        private ByteStreams()
        {
        }

        /// <summary>
        /// Copies all bytes from the input stream to the output stream. 
        /// Does not close or flush either stream.
        /// </summary>
        /// <param name="from">The input stream to read from.</param>
        /// <param name="to">The output stream to write to.</param>
        /// <returns>The number of bytes copied.</returns>
        /// <exception cref="IOException">If an I/O error occurs.</exception>
        public static long Copy(Stream from, Stream to)
        {
            Preconditions.CheckNotNull(from);
            Preconditions.CheckNotNull(to);
            byte[] buf = new byte[BUF_SIZE];
            long total = 0;
            while (true)
            {
                int r = from.Read(buf, 0, BUF_SIZE);
                if (r == 0)
                {
                    break;
                }

                to.Write(buf, 0, r);
                total += r;
            }

            return total;
        }

        /// <summary>
        /// Reads some bytes from an input stream and stores them into the buffer array
        /// <code>b</code>. This method blocks until <code>len</code> bytes of input data 
        /// have been read into the array, or end of file is detected. The number of bytes
        /// read is returned, possibly zero. Does not close the stream.
        ///
        /// <para />A caller can detect EOF if the number of bytes read is less than
        /// <code>len</code>. All subsequent calls on the same stream will return zero.
        ///
        /// <para />If <code>b</code> is null, a <code>NullReferenceException</code> is thrown.
        /// If <code>off</code> is negative, or <code>len</code> is negative, or 
        /// <code>off+len</code> is greater than the length of the array <code>b</code>, 
        /// then an <code>ArgumentOutOfRangeException</code> is thrown. 
        /// If <code>len</code> is zero, then no bytes are read. 
        /// Otherwise, the first byte read is stored into element <code> b[off]</code>, 
        /// the next one into <code>b[off+1]</code>, and so on. The number of bytes read is, 
        /// at most, equal to <code>len</code>.
        /// </summary>
        /// <param name="inputStream">The input stream to read from.</param>
        /// <param name="b">The buffer into which the data is read.</param>
        /// <param name="off">An int specifying the offset into the data.</param>
        /// <param name="len">An int specifying the number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
        /// <exception cref="IOException">If an I/O error occurs.</exception>
        public static int Read(Stream inputStream, byte[] b, int off, int len)
        {
            Preconditions.CheckNotNull(inputStream);
            Preconditions.CheckNotNull(b);
            if (len < 0)
            {
                throw new ArgumentOutOfRangeException("len is negative");
            }

            int total = 0;
            while (total<len)
            {
                int result = inputStream.Read(b, off + total, len - total);
                if (result == 0)
                {
                    break;
                }

                total += result;
            }

            return total;
        }

        /// <summary>
        /// Reads all bytes from an input stream into a byte array.
        /// Does not close the stream.
        /// </summary>
        /// <param name="inputStream">The input stream to read from.</param>
        /// <returns>A byte array containing all the bytes from the stream.</returns>
        /// <exception cref="IOException">If an I/O error occurs.</exception>
        public static byte[] ToByteArray(Stream inputStream)
        {
            using (MemoryStream outputStream = new MemoryStream())
            {
                Copy(inputStream, outputStream);
                return outputStream.ToArray();
            }
        }

        /// <summary>
        /// Reads all bytes from an input stream into a byte array. The given
        /// expected size is used to create an initial byte array, but if the actual
        /// number of bytes read from the stream differs, the correct result will be
        /// returned anyway.
        /// </summary>
        public static byte[] ToByteArray(
            Stream inputStream,
            int expectedSize)
        {
            byte[] bytes = new byte[expectedSize];
            int remaining = expectedSize;

            while (remaining > 0)
            {
                int off = expectedSize - remaining;
                int read = inputStream.Read(bytes, off, remaining);
                if (read == 0)
                {
                    // end of stream before reading expectedSize bytes
                    // just return the bytes read so far
                    byte[] des = new byte[bytes.Length];
                    Array.Copy(bytes, des, bytes.Length);
                    return des;
                }

                remaining -= read;
            }

            // bytes is now full
            int b = inputStream.ReadByte();
            if (b == -1)
            {
                return bytes;
            }

            // The stream was longer, so read the rest normally
            FastByteArrayOutputStream outputStream = new FastByteArrayOutputStream();
            outputStream.WriteByte((byte)b); // write the byte we read when testing for end of stream
            Copy(inputStream, outputStream);

            byte[] result = new byte[bytes.Length + outputStream.Length];
            Array.Copy(bytes, 0, result, 0, bytes.Length);
            outputStream.WriteTo(result, bytes.Length);
            return result;
        }

        /// <summary>
        /// ByteArrayOutputStream that provides limited access to its internal byte array.
        /// </summary>
        private sealed class FastByteArrayOutputStream : MemoryStream
        {
            /// <summary>
            /// Writes the contents of the internal buffer to the given array starting
            /// at the given offset. Assumes the array has space to hold count bytes.
            /// </summary>
            public void WriteTo(byte[] b, int off)
            {
                byte[] buf = ToArray();
                Array.Copy(buf, 0, b, off, buf.Length);
            }
        }

        /// <summary>
        /// Attempts to read <code>len</code> bytes from the stream into the given array
        /// starting at <code>off</code>, with the same behavior as Stream.Read.
        /// Does not close the stream.
        /// </summary>
        /// <param name="inputStream">The input stream to read from.</param>
        /// <param name="b">The buffer into which the data is read.</param>
        /// <param name="off">An int specifying the offset into the data.</param>
        /// <param name="len">An int specifying the number of bytes to read.</param>
        /// <exception cref="EndOfStreamException">
        /// If this stream reaches the end before reading all the bytes.
        /// </exception>
        /// <exception cref="IOException">If an I/O error occurs.</exception>
        public static void ReadFully(Stream inputStream, byte[] b, int off, int len)
        {
            int read = Read(inputStream, b, off, len);
            if (read != len)
            {
                throw new EndOfStreamException("reached end of stream after reading "
                    + read + " bytes; " + len + " bytes expected");
            }
        }
    }
}
