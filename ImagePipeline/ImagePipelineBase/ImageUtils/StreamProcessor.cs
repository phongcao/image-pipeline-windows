using System.IO;

namespace ImageUtils
{
    /// <summary>
    /// Util for processing Stream.
    /// </summary>
    class StreamProcessor
    {
        /// <summary>
        ///  Consumes up to 4 bytes and returns them as int (taking into account endianess).
        ///  Throws exception if specified number of bytes cannot be consumed.
        ///  <param name="inputStream">the input stream to read bytes from</param>
        ///  <param name="numBytes">the number of bytes to read</param>
        ///  <param name="isLittleEndian">whether the bytes should be interpreted in little or big endian format</param>
        ///  @return packed int read from input stream and constructed according to endianness
        /// </summary>
        public static int ReadPackedInt(Stream inputStream, int numBytes, bool isLittleEndian)
        {
            int value = 0;
            byte[] buf = new byte[1];
            for (int i = 0; i < numBytes; i++)
            {
                int result = inputStream.Read(buf, 0, 1);
                if (result == 0)
                {
                    throw new IOException("no more bytes");
                }

                if (isLittleEndian)
                {
                    value |= (buf[0] & 0xFF) << (i * 8);
                }
                else
                {
                    value = (value << 8) | (buf[0] & 0xFF);
                }
            }

            return value;
        }
    }
}
