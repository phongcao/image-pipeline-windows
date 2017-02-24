using BinaryResource;
using Cache.Common;
using System.IO;

namespace Cache.Disk
{
    /// <summary>
    /// This is a builder-like interface returned when calling insert.
    /// It holds all the operations carried through an 
    /// <see cref="Cache.Disk.IDiskStorage.Insert"/> operation:
    /// - Writing data
    /// - Commiting
    /// - Clean up
    /// </summary>
    public interface IInserter
    {
        /// <summary>
        /// Update the contents of the resource to be inserted. Executes outside
        /// the session lock. The writer callback will be provided with an output 
        /// Stream to write to. For high efficiency client should make sure that
        /// data is written in big chunks (for example by employing
        /// BufferedInputStream or writing all data at once).
        /// </summary>
        /// <param name="callback">The write callback.</param>
        /// <param name="debugInfo">Helper object for debugging.</param>
        void WriteData(IWriterCallback callback, object debugInfo);

        /// <summary>
        /// Commits the insertion into the cache. Once this is called the entry
        /// will be available to clients of the cache.
        /// </summary>
        /// <param name="debugInfo">Debug object for debugging.</param>
        /// <returns>The final resource created.</returns>
        /// <exception cref="IOException">
        /// On errors during the commit.
        /// </exception>
        IBinaryResource Commit(object debugInfo);

        /// <summary>
        /// Discards the insertion process.
        /// If resource was already committed the call is ignored.
        /// </summary>
        /// <returns>
        /// true if cleanUp is successful (or noop), false if something couldn't
        /// be dealt with.
        /// </returns>
        bool CleanUp();
    }
}
