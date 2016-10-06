using System.IO;

namespace FBCore.Common.File
{
    /// <summary>
    /// Represents an exception when the target file cannot be deleted
    /// </summary>
    public class FileDeleteException : IOException
    {
        /// <summary>
        /// Instantiates the <see cref="FileDeleteException"/>
        /// </summary>
        /// <param name="message"></param>
        public FileDeleteException(string message) : base(message)
        {
        }
    }
}
