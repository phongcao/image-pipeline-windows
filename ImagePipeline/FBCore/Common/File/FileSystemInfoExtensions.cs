using System;
using System.Collections.Generic;
using System.IO;

namespace FBCore.Common.File.Extensions
{
    /// <summary>
    /// Extension methods of <see cref="Directory"/>.
    /// </summary>
    public static class FileSystemInfoExtensions
    {
        /// <summary>
        /// Returns an array of files contained in the directory represented by this file.
        /// </summary>
        public static FileSystemInfo[] ListFiles(this FileSystemInfo directory)
        {
            List<FileSystemInfo> files = new List<FileSystemInfo>();

            if (directory.Exists)
            {
                //  Loop through all the immediate subdirectories
                foreach (var entry in Directory.GetDirectories(directory.FullName))
                {
                    files.Add(new DirectoryInfo(entry));
                }

                //  Loop through all the files
                foreach (var entry in Directory.GetFiles(directory.FullName))
                {
                    files.Add(new FileInfo(entry));
                }
            }

            return files.ToArray();
        }

        /// <summary>
        /// Check if file is a directory.
        /// </summary>
        /// <param name="file">A file.</param>
        /// <returns>true if file is a directory, false otherwise.</returns>
        public static bool IsDirectory(this FileSystemInfo file)
        {
            return (file.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
        }

        /// <summary>
        /// Renames this file to <code>newFileInfo</code>. This operation is supported 
        /// for both files and directories.
        /// </summary>
        /// <param name="currentFileInfo">The current file.</param>
        /// <param name="newFileInfo">The new file.</param>
        /// <returns>true on success.</returns>
        public static bool RenameTo(this FileSystemInfo currentFileInfo, FileSystemInfo newFileInfo)
        {
            try
            {
                if (currentFileInfo.IsDirectory())
                {
                    Directory.Move(currentFileInfo.FullName, newFileInfo.FullName);
                }
                else
                {
                    System.IO.File.Move(currentFileInfo.FullName, newFileInfo.FullName);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a new file made from the pathname of the parent of this file.
        /// This is the path up to but not including the last name. <code>null</code> is
        /// returned when there is no parent.
        /// </summary>
        public static DirectoryInfo GetParent(this FileSystemInfo file)
        {
            try
            {
                return Directory.GetParent(file.FullName);
            }
            catch (Exception)
            {
            }

            return null;
        }

        /// <summary>
        /// Creates an empty directory or file and close it.
        /// </summary>
        /// <param name="file">A file.</param>
        /// <returns>true if the creation is successful, false otherwise.</returns>
        public static bool CreateEmpty(this FileSystemInfo file)
        {
            try
            {
                if (file.GetType() == typeof(DirectoryInfo))
                {
                    ((DirectoryInfo)file).Create();
                }
                else
                {
                    ((FileInfo)file).Create().Dispose();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
