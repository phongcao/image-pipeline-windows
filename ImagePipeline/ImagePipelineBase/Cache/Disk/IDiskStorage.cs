using BinaryResource;
using System.Collections.Generic;
using System.IO;

namespace Cache.Disk
{
    /// <summary>
    /// Storage for files in the cache.
    /// Responsible for maintaining state
    /// (count, size, watch file existence, reachability).
    /// </summary>
    public interface IDiskStorage
    {
        /// <summary>
        /// Is this storage enabled?
        /// </summary>
        /// <returns>true, if enabled.</returns>
        bool IsEnabled { get; }

        /// <summary>
        /// Is this storage external?
        /// </summary>
        /// <returns>true, if external.</returns>
        bool IsExternal { get; }

        /// <summary>
        /// Get the resource with the specified name.
        /// </summary>
        /// <param name="resourceId">Id of the resource.</param>
        /// <param name="debugInfo">Helper object for debugging.</param>
        /// <returns>
        /// The resource with the specified name. NULL if not found.
        /// </returns>
        /// <exception cref="IOException">
        /// For unexpected behavior.
        /// </exception>
        IBinaryResource GetResource(string resourceId, object debugInfo);

        /// <summary>
        /// Does a resource with this name exist?
        /// </summary>
        /// <param name="resourceId">Id of the resource.</param>
        /// <param name="debugInfo">Helper object for debugging.</param>
        /// <returns>
        /// true, if the resource is present in the storage, false otherwise.
        /// </returns>
        bool Contains(string resourceId, object debugInfo);

        /// <summary>
        /// Does a resource with this name exist? If so, update the last-accessed
        /// time for the resource.
        /// </summary>
        /// <param name="resourceId">Id of the resource.</param>
        /// <param name="debugInfo">Helper object for debugging.</param>
        /// <returns>
        /// true, if the resource is present in the storage, false otherwise.
        /// </returns>
        bool Touch(string resourceId, object debugInfo);

        /// <summary>
        /// Purge unexpected resources.
        /// </summary>
        void PurgeUnexpectedResources();

        /// <summary>
        /// Creates a temporary resource for writing content. Split from Commit()
        /// in order to allow concurrent writing of cache entries.
        /// This entry will not be available to cache clients until Commit() is
        /// called passing in the resource returned from this method.
        /// </summary>
        /// <param name="resourceId">Id of the resource.</param>
        /// <param name="debugInfo">Helper object for debugging.</param>
        /// <returns>
        /// The Inserter object with methods to write data, commit or cancel the
        /// insertion.
        /// </returns>
        /// <exception cref="IOException">
        /// On errors during this operation.
        /// </exception>
        IInserter Insert(string resourceId, object debugInfo);

        /// <summary>
        /// Get all entries currently in the storage.
        /// </summary>
        /// <returns>A collection of entries in storage.</returns>
        ICollection<IEntry> GetEntries();

        /// <summary>
        /// Remove the resource represented by the entry.
        /// <param name="entry">Entry of the resource to delete.</param>
        /// </summary>
        /// <returns>
        /// Size of deleted file if successfully deleted, -1 otherwise.
        /// </returns>
        long Remove(IEntry entry);

        /// <summary>
        /// Remove the resource with specified id.
        /// </summary>
        /// <param name="resourceId">The resource Id.</param>
        /// <returns>
        /// Size of deleted file if successfully deleted, -1 otherwise.
        /// </returns>
        long Remove(string resourceId);

        /// <summary>
        /// Clear all contents of the storage.
        /// </summary>
        void ClearAll();

        /// <summary>
        /// Gets the disk dump info.
        /// </summary>
        DiskDumpInfo GetDumpInfo();

        /// <summary>
        /// Get the storage's name, which should be unique.
        /// </summary>
        /// <returns>Name of the this storage.</returns>
        string StorageName { get; }
    }
}
