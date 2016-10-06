using System;
using System.Collections.Generic;
using System.IO;

namespace FBCore.Common.File.Extensions
{
    /// <summary>
    /// Extension methods of <see cref="Directory"/>
    /// </summary>
    public static class FileSystemInfoExtensions
    {
        /// <summary>
        /// Returns an array of files contained in the directory represented by this file
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static FileSystemInfo[] ListFiles(this FileSystemInfo directory)
        {
            List<FileSystemInfo> files = new List<FileSystemInfo>();

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

            return files.ToArray();
        }

        /// <summary>
        /// Check if file is a directory
        /// </summary>
        /// <param name="file">a file</param>
        /// <returns>true if file is a directory, false otherwise.</returns>
        public static bool IsDirectory(this FileSystemInfo file)
        {
            return (file.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
        }

        /// <summary>
        /// Renames this file to <code> newPath</code>. This operation is supported for both
        /// files and directories.
        /// </summary>
        /// <param name="currentFileInfo">the current path.</param>
        /// <param name="newFileInfo">the new path.</param>
        /// <returns>true on success.</returns>
        public static bool RenameTo(this FileSystemInfo currentFileInfo, FileSystemInfo newFileInfo)
        {
            try
            {
                if (currentFileInfo.IsDirectory())
                {
                    DirectoryInfo currentDir = (DirectoryInfo)currentFileInfo;
                    DirectoryInfo newDir = (DirectoryInfo)newFileInfo;
                    currentDir.MoveTo(newDir.FullName);
                }
                else
                {
                    FileInfo currentFile = (FileInfo)currentFileInfo;
                    FileInfo newFile = (FileInfo)newFileInfo;
                    currentFile.MoveTo(Path.Combine(currentFile.DirectoryName, newFile.Name));
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
