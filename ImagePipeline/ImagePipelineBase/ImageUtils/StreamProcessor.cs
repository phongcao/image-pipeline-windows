using System.IO;

namespace ImageUtils
{
    /// <summary>
    /// Util for processing Stream.
    /// </summary>
    class StreamProcessor
    {
        /// <summary>
        /// Consumes up to 4 bytes and returns them as int
        /// (taking into account endianess).
        /// Throws exception if specified number of bytes cannot be consumed.
        /// </summary>
        /// <param name="inputStream">
        /// The input stream to read bytes from.
        /// </param>
        /// <param name="numBytes">
        /// The number of bytes to read.
        /// </param>
        /// <param name="isLittleEndian">
        /// Whether the bytes should be interpreted in little or big
        /// endian format.
        /// </param>
        /// <returns>
        /// Packed int read from input stream and constructed according
        /// to endianness.
        /// </returns>
        public static int ReadPackedInt(Stream inputStream, int numBytes, bool isLittleEndian)
        {
            int value = 0;
            for (int i = 0; i < numBytes; i++)
            {
                int result = inputStream.ReadByte();
                if (result == -1)
                {
                    throw new IOException("no more bytes");
                }

                if (isLittleEndian)
                {
                    value |= (result & 0xFF) << (i * 8);
                }
                else
                {
                    value = (value << 8) | (result & 0xFF);
                }
            }

            return value;
        }
    }
}
