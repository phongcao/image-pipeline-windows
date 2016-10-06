using FBCore.Common.File.Extensions;
using System;
using System.IO;

namespace FBCore.Common.File
{
    /// <summary>
    /// Utility class to visit a file tree.
    /// There's similar functionality in Java 7's Files.WalkFileTree method.
    /// Its methods could be merge into FileUtil (although it seems to have a lot of
    /// crazy redundant methods, maybe for testing, but crazy anyway).
    /// </summary>
    public class FileTree
    {
        /// <summary>
        /// Iterates over the file tree of a directory. It receives a visitor and will call its methods
        /// for each file in the directory.
        /// PreVisitDirectory (directory)
        /// VisitFile (file)
        /// - recursively the same for every subdirectory
        /// PostVisitDirectory (directory)
        /// <param name="directory">the directory to iterate</param>
        /// <param name="visitor">the visitor that will be invoked for each directory/file in the tree</param>
        /// </summary>
        public static void WalkFileTree(FileSystemInfo directory, IFileTreeVisitor visitor)
        {
            visitor.PreVisitDirectory(directory);
            FileSystemInfo[] files = directory.ListFiles();
            foreach (var file in files)
            {
                if (file.IsDirectory())
                {
                    WalkFileTree(file, visitor);
                }
                else
                {
                    visitor.VisitFile(file);
                }
            }

            visitor.PostVisitDirectory(directory);
        }

        /// <summary>
        /// Deletes all files and subdirectories in directory (doesn't delete the directory
        /// passed as parameter).
        /// </summary>
        public static bool DeleteContents(FileSystemInfo directory)
        {
            FileSystemInfo[] files = directory.ListFiles();
            bool success = true;
            foreach (var file in files)
            {
                success &= DeleteRecursively(file);
            }

            return success;
        }

        /// <summary>
        /// Deletes the file and if it's a directory deletes also any content in it
        /// <param name="file">a file or directory</param>
        /// @return true if the file/directory could be deleted
        /// </summary>
        public static bool DeleteRecursively(FileSystemInfo file)
        {
            if (file.IsDirectory())
            {
                DeleteContents(file);
            }

            try
            {
                // If I can delete directory then I know everything was deleted
                file.Delete();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
