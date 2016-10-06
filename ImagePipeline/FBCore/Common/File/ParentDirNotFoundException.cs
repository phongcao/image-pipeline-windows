using System.IO;

namespace FBCore.Common.File
{
    /// <summary>
    /// A specialization of DirectoryNotFoundException when the parent-dir doesn't exist
    /// </summary>
    public class ParentDirNotFoundException : DirectoryNotFoundException
    {
        /// <summary>
        /// Instantiates the <see cref="ParentDirNotFoundException"/>
        /// </summary>
        /// <param name="message"></param>
        public ParentDirNotFoundException(string message) : base(message)
        {
        }
    }
}
