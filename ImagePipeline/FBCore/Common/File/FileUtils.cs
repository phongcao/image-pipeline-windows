using FBCore.Common.File.Extensions;
using FBCore.Common.Internal;
using System;
using System.IO;

namespace FBCore.Common.File
{
    /// <summary>
    /// Static operations on <see cref="File"/>s.
    /// </summary>
    public class FileUtils
    {
        /// <summary>
        /// Creates the specified directory, along with all parent paths if necessary.
        /// </summary>
        /// <param name="directory">Directory to be created.</param>
        /// <exception cref="CreateDirectoryException">
        /// Could not create the directory.
        /// </exception>
        public static void Mkdirs(FileSystemInfo directory)
        {
            if (System.IO.File.Exists(directory.FullName))
            {
                // File exists, but is not a directory - delete it
                try
                {
                    System.IO.File.Delete(directory.FullName);
                }
                catch (Exception)
                {
                    throw new FileDeleteException(directory.FullName);
                }
            }

            try
            {
                // Doesn't exist. Create one
                ((DirectoryInfo)directory).Create();
            }
            catch (IOException)
            {
                throw new CreateDirectoryException(directory.FullName);
            }
        }

        /// <summary>
        /// Renames the source file to the target file. If the target file exists, then we 
        /// attempt to delete it. If the delete or the rename operation fails, then we raise 
        /// an exception.
        /// </summary>
        /// <param name="source">The source file.</param>
        /// <param name="target">The new 'name' for the source file.</param>
        /// <exception cref="RenameException">Could not rename file.</exception>
        public static void Rename(FileSystemInfo source, FileSystemInfo target)
        {
            Preconditions.CheckNotNull(source);
            Preconditions.CheckNotNull(target);

            // Delete the target first - but ignore the result
            try
            {
                target.Delete();
            }
            catch (Exception)
            {
            }

            if (source.RenameTo(target))
            {
                return;
            }

            throw new RenameException(
                "Unknown error renaming " + source.FullName + " to " + target.FullName);
        }
    }
}
