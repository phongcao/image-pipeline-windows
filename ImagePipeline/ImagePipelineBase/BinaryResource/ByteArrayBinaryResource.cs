using FBCore.Common.Internal;
using System.IO;

namespace BinaryResource
{
    /// <summary>
    /// A trivial implementation of IBinaryResource that wraps a byte array.
    /// </summary>
    public class ByteArrayBinaryResource : IBinaryResource
    {
        private readonly byte[] _bytes;

        /// <summary>
        /// Instantiates the <see cref="ByteArrayBinaryResource"/>.
        /// </summary>
        public ByteArrayBinaryResource(byte[] bytes)
        {
            _bytes = Preconditions.CheckNotNull(bytes);
        }

        /// <summary>
        /// Returns the size of the byte array.
        /// </summary>
        public long GetSize()
        {
            return _bytes.Length;
        }

        /// <summary>
        /// Opens the byte array stream.
        /// </summary>
        public Stream OpenStream()
        {
            return new MemoryStream(_bytes);
        }

        /// <summary>
        /// Get the underlying byte array.
        /// </summary>
        /// <returns>The underlying byte array of this resource.</returns>
        public byte[] Read()
        {
            return _bytes;
        }
    }
}
