using FBCore.Common.Internal;
using System.IO;

namespace BinaryResource
{
    /// <summary>
    /// Implementation of IBinaryResource based on a real file.
    /// See <see cref="IBinaryResource"/> for more details.
    /// </summary>
    public class FileBinaryResource : IBinaryResource
    {
        private readonly FileInfo _file;

        private FileBinaryResource(FileInfo file)
        {
            _file = Preconditions.CheckNotNull(file);
        }

        /// <summary>
        /// Gets file.
        /// </summary>
        public FileInfo File
        {
            get
            {
                return _file;
            }
        }

        /// <summary>
        /// Opens file stream.
        /// </summary>
        public Stream OpenStream()
        {
            return _file.OpenRead();
        }

        /// <summary>
        /// Returns file size.
        /// </summary>
        /// <returns></returns>
        public long GetSize()
        {
            return _file.Length; // 0L if file doesn't exist
        }

        /// <summary>
        /// Reads byte array from file and returns.
        /// </summary>
        public byte[] Read()
        {
            return Files.ToByteArray(_file);
        }

        /// <summary>
        /// Compares files.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj.GetType() == typeof(FileBinaryResource)))
            {
                return false;
            }

            FileBinaryResource that = (FileBinaryResource)obj;
            return _file.FullName.Equals(that._file.FullName);
        }

        /// <summary>
        /// Gets hash code of the file.
        /// </summary>
        public override int GetHashCode()
        {
            return _file.GetHashCode();
        }

        /// <summary>
        /// Factory method to create a wrapping IBinaryResource without
        /// explicitly taking care of null. If the supplied file is null,
        /// instead of IBinaryResource, null is returned.
        /// </summary>
        public static FileBinaryResource CreateOrNull(FileInfo file)
        {
            return (file != null) ? new FileBinaryResource(file) : null;
        }
    }
}
