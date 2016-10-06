using System;
using System.IO;

namespace FBCore.Common.Internal
{
    /// <summary>
    /// Provides utility methods for working with files.
    ///
    /// <para />All method parameters must be non-null unless documented otherwise.
    ///
    /// @author Chris Nokleberg
    /// @author Colin Decker
    /// @since 1.0
    /// </summary>
    public class Files
    {
        private Files() { }

        /// <summary>
        /// Reads a file of the given expected size from the given input stream, if
        /// it will fit into a byte array. This method handles the case where the file
        /// size changes between when the size is read and when the contents are read
        /// from the stream.
        /// </summary>
        static byte[] ReadFile(Stream inputStream, long expectedSize)
        {
            if (expectedSize > int.MaxValue)
            {
                throw new OutOfMemoryException("file is too large to fit in a byte array: "
                    + expectedSize + " bytes");
            }

            // some special files may return size 0 but have content, so read
            // the file normally in that case
            return expectedSize == 0
                ? ByteStreams.ToByteArray(inputStream)
                : ByteStreams.ToByteArray(inputStream, (int)expectedSize);
        }

        /// <summary>
        /// Reads all bytes from a file into a byte array.
        ///
        /// <param name="file">the file to read from</param>
        /// @return a byte array containing all the bytes from file
        /// @throws IllegalArgumentException if the file is bigger than the largest
        ///     possible byte array (2^31 - 1)
        /// @throws IOException if an I/O error occurs
        /// </summary>
        public static byte[] ToByteArray(FileInfo file)
        {
            using (FileStream inputStream = file.OpenRead())
            {
                return ReadFile(inputStream, inputStream.Length);
            }
        }
    }
}
