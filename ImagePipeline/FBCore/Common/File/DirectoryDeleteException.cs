using System.IO;

namespace FBCore.Common.File
{
    /// <summary>
    /// Represents an exception when the target directory cannot be deleted
    /// </summary>
    public class DirectoryDeleteException : IOException
    {
        /// <summary>
        /// Instantiates the <see cref="DirectoryDeleteException"/>
        /// </summary>
        /// <param name="message"></param>
        public DirectoryDeleteException(string message) : base(message)
        {
        }
    }
}
