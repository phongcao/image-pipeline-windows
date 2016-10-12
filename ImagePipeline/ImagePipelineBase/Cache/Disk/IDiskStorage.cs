using BinaryResource;
using System.Collections.Generic;

namespace Cache.Disk
{
    /// <summary>
    /// Storage for files in the cache.
    /// Responsible for maintaining state (count, size, watch file existence, reachability)
    /// </summary>
    public interface IDiskStorage
    {
        /// <summary>
        /// Is this storage enabled?
        /// @return true, if enabled
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Is this storage external?
        /// @return true, if external
        /// </summary>
        bool External { get; }

        /// <summary>
        /// Get the resource with the specified name
        /// <param name="resourceId">id of the resource</param>
        /// <param name="debugInfo">helper object for debugging</param>
        /// @return the resource with the specified name. NULL if not found
        /// @throws IOException for unexpected behavior.
        /// </summary>
        IBinaryResource GetResource(string resourceId, object debugInfo);

        /// <summary>
        /// Does a resource with this name exist?
        /// <param name="resourceId">id of the resource</param>
        /// <param name="debugInfo">helper object for debugging</param>
        /// @return true, if the resource is present in the storage, false otherwise
        /// @throws IOException
        /// </summary>
        bool Contains(string resourceId, object debugInfo);

        /// <summary>
        /// Does a resource with this name exist? If so, update the last-accessed time for the
        /// resource
        /// <param name="resourceId">id of the resource</param>
        /// <param name="debugInfo">helper object for debugging</param>
        /// @return true, if the resource is present in the storage, false otherwise
        /// @throws IOException
        /// </summary>
        bool Touch(string resourceId, object debugInfo);

        /// <summary>
        /// Purge unexpected resources
        /// </summary>
        void PurgeUnexpectedResources();

        /// <summary>
        /// Creates a temporary resource for writing content. Split from commit()
        /// in order to allow concurrent writing of cache entries.
        /// This entry will not be available to cache clients until
        /// commit() is called passing in the resource returned
        /// from this method.
        /// <param name="resourceId">id of the resource</param>
        /// <param name="debugInfo">helper object for debugging</param>
        /// @return the Inserter object with methods to write data, commit or cancel the insertion
        /// @exception IOException on errors during this operation
        /// </summary>
        IInserter Insert(string resourceId, object debugInfo);

        /// <summary>
        /// Get all entries currently in the storage
        /// @return a collection of entries in storage
        /// @throws IOException
        /// </summary>
        ICollection<IEntry> GetEntries();

        /// <summary>
        /// Remove the resource represented by the entry
        /// <param name="entry">entry of the resource to delete</param>
        /// @return size of deleted file if successfully deleted, -1 otherwise
        /// @throws IOException
        /// </summary>
        long Remove(IEntry entry);

        /// <summary>
        /// Remove the resource with specified id
        /// <param name="resourceId"></param>
        /// @return size of deleted file if successfully deleted, -1 otherwise
        /// @throws IOException
        /// </summary>
        long Remove(string resourceId);

        /// <summary>
        /// Clear all contents of the storage
        /// @exception IOException
        /// @throws IOException
        /// </summary>
        void ClearAll();

        /// <summary>
        /// Gets the disk dump info
        /// </summary>
        DiskDumpInfo GetDumpInfo();

        /// <summary>
        /// Get the storage's name, which should be unique
        /// @return name of the this storage
        /// </summary>
        string StorageName { get; }
    }
}
