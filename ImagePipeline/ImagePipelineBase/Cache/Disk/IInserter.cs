using BinaryResource;
using Cache.Common;

namespace Cache.Disk
{
    /// <summary>
    /// This is a builder-like interface returned when calling insert.
    /// It holds all the operations carried through an 
    /// <see cref="Cache.Disk.IDiskStorage.Insert"/> operation:
    /// - writing data
    /// - commiting
    /// - clean up
    /// </summary>
    public interface IInserter
    {
        /// <summary>
        /// Update the contents of the resource to be inserted. Executes outside the session lock.
        /// The writer callback will be provided with an OutputStream to write to.
        /// For high efficiency client should make sure that data is written in big chunks
        /// (for example by employing BufferedInputStream or writing all data at once).
        /// <param name="callback">the write callback</param>
        /// <param name="debugInfo">helper object for debugging</param>
        /// @throws IOException
        /// </summary>
        void WriteData(IWriterCallback callback, object debugInfo);

        /// <summary>
        /// Commits the insertion into the cache.
        /// Once this is called the entry will be available to clients of the cache.
        /// <param name="debugInfo">debug object for debugging</param>
        /// @return the final resource created
        /// @exception IOException on errors during the commit
        /// </summary>
        IBinaryResource Commit(object debugInfo);

        /// <summary>
        /// Discards the insertion process.
        /// If resource was already committed the call is ignored.
        /// @return true if cleanUp is successful (or noop), false if something couldn't be dealt with
        /// </summary>
        bool CleanUp();
    }
}
