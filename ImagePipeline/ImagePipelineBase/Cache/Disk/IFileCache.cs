using BinaryResource;
using Cache.Common;
using FBCore.Common.Disk;

namespace Cache.Disk
{
    /// <summary>
    /// Interface that caches based on disk should implement.
    /// </summary>
    public interface IFileCache : IDiskTrimmable
    {
        /// <summary>
        /// Tells if this cache is enabled. It's important for some caches that can be
        /// disabled without further notice (like in removable/unmountable storage).
        /// Anyway a disabled cache should just ignore calls, not fail.
        /// </summary>
        /// <returns>
        /// true if this cache is usable, false otherwise.
        /// </returns>
        bool IsEnabled { get; }

        /// <summary>
        /// Returns the binary resource cached with key.
        /// </summary>
        IBinaryResource GetResource(ICacheKey key);

        /// <summary>
        /// Returns true if the key is in the in-memory key index.
        ///
        /// Not guaranteed to be correct. The cache may yet have this key even if
        /// this returns false. But if it returns true, it definitely has it.
        ///
        /// Avoids a disk read.
        /// </summary>
        bool HasKeySync(ICacheKey key);

        /// <summary>
        /// Returns true if the key is in the in-memory key index.
        /// </summary>
        bool HasKey(ICacheKey key);

        /// <summary>
        /// Probes whether the object corresponding to the key is in the cache.
        /// </summary>
        bool Probe(ICacheKey key);

        /// <summary>
        /// Inserts resource into file with key.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <param name="writer">
        /// Callback that writes to an output stream.
        /// </param>
        /// <returns>A sequence of bytes.</returns>
        IBinaryResource Insert(ICacheKey key, IWriterCallback writer);

        /// <summary>
        /// Removes a resource by key from cache.
        /// </summary>
        /// <param name="key">Cache key.</param>
        void Remove(ICacheKey key);

        /// <summary>
        /// Gets the in-use size of the cache.
        /// </summary>
        /// <returns>The in-use size of the cache.</returns>
        long Size { get; }

        /// <summary>
        /// Gets the count of pictures in the cache.
        /// </summary>
        /// <returns>The count of pictures in the cache.</returns>
        long Count { get; }

        /// <summary>
        /// Deletes old cache files.
        /// </summary>
        /// <param name="cacheExpirationMs">
        /// Files older than this will be deleted.
        /// </param>
        /// <returns>
        /// The age in ms of the oldest file remaining in the cache.
        /// </returns>
        long ClearOldEntries(long cacheExpirationMs);

        /// <summary>
        /// Clears the disk cache.
        /// </summary>
        void ClearAll();

        /// <summary>
        /// Gets the disk dump info.
        /// </summary>
        DiskDumpInfo GetDumpInfo();
    }
}
