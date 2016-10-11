using System.IO;

namespace BinaryResource
{
    /// <summary>
    /// Interface representing a sequence of bytes that abstracts underlying source (e.g. file).
    ///
    /// <para />It represents a non-volatile resource whenever it exists and it can be read multiple times.
    /// Since it is stream based, performing transformations like encryption can be done by a
    /// simple wrapper, instead of writing the decrypted content of the original file into a new file.
    ///
    /// <para />Inspired partly by Guava's ByteSource class, but does not use its implementation.
    /// </summary>
    public interface IBinaryResource
    {
        /// <summary>
        /// Opens a new <see cref="Stream"/> for reading from this source. This method should return a new,
        /// independent stream each time it is called.
        ///
        /// <para />The caller is responsible for ensuring that the returned stream is closed.
        ///
        /// @throws IOException if an I/O error occurs in the process of opening the stream
        /// </summary>
        Stream OpenStream();

        /// <summary>
        /// Reads the full contents of this byte source as a byte array.
        ///
        /// @throws IOException if an I/O error occurs in the process of reading from this source
        /// </summary>
        byte[] Read();

        /// <summary>
        /// Returns the size of this source in bytes. This may be a heavyweight
        /// operation that will open a stream, read (or <see cref="Stream.Seek"/>, if possible)
        /// to the end of the stream and return the total number of bytes that were read.
        ///
        /// <para />For some sources, such as a file, this method may use a more efficient implementation. Note
        /// that in such cases, it is <i>possible</i> that this method will return a different number of
        /// bytes than would be returned by reading all of the bytes (for example, some special files may
        /// return a size of 0 despite actually having content when read).
        ///
        /// <para />In either case, if this is a mutable source such as a file, the size it returns may not be
        /// the same number of bytes a subsequent read would return.
        ///
        /// @throws IOException if an I/O error occurs in the process of reading the size of this source
        /// </summary>
        long GetSize();
    }
}
