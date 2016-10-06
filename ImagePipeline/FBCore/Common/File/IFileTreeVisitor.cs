using System.IO;

namespace FBCore.Common.File
{
    /// <summary>
    /// An instance of this interface must be passed to FileTree.WalkFileTree method in order
    /// to execute some logic while iterating over the directory descendants.
    /// Java 7 provides a FileVisitor interface and a Files.WalkFileTree method that does this same
    /// thing (with more options).
    /// </summary>
    public interface IFileTreeVisitor
    {
        /// <summary>
        /// Called before iterating over a directory (including the root directory of the iteration)
        /// </summary>
        void PreVisitDirectory(FileSystemInfo directory);

        /// <summary>
        /// Called for each file contained in a directory (after preVisitDirectory)
        /// </summary>
        void VisitFile(FileSystemInfo file);

        /// <summary>
        /// Called after iterating over a directory (including the root directory of the iteration)
        /// </summary>
        void PostVisitDirectory(FileSystemInfo directory);
    }
}
