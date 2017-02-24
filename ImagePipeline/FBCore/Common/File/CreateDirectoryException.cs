using System.IO;

namespace FBCore.Common.File
{
    /// <summary>
    /// Represents an exception during directory creation.
    /// </summary>
    public class CreateDirectoryException : IOException
    {
        /// <summary>
        /// Instantiates the <see cref="CreateDirectoryException"/>.
        /// </summary>
        public CreateDirectoryException(string message) : base(message)
        {
        }
    }
}
