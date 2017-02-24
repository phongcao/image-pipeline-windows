using BinaryResource;
using Cache.Common;
using FBCore.Common.File;
using FBCore.Common.Internal;
using FBCore.Common.Time;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Cache.Disk
{
    /// <summary>
    /// A supplier of a DiskStorage concrete implementation.
    /// </summary>
    public class DynamicDefaultDiskStorage : IDiskStorage
    {
        private static readonly object _diskInstanceGate = new object();
        private readonly int _version;
        private readonly ISupplier<FileSystemInfo> _baseDirectoryPathSupplier;
        private readonly string _baseDirectoryName;
        private readonly ICacheErrorLogger _cacheErrorLogger;

        internal volatile State _currentState;

        // For unit tests
        private readonly Clock _clock;

        /// <summary>
        /// Represents the current 'cached' state.
        /// </summary>
        internal class State
        {
            public IDiskStorage DiskStorageDelegate { get; }
            public FileSystemInfo RootDirectory { get; }

            internal State(FileSystemInfo rootDirectory, IDiskStorage diskStorageDelegate) 
            {
                DiskStorageDelegate = diskStorageDelegate;
                RootDirectory = rootDirectory;
            }
        }

        /// <summary>
        /// Instantiates the <see cref="DynamicDefaultDiskStorage"/>.
        /// </summary>
        public DynamicDefaultDiskStorage(
            int version,
            ISupplier<FileSystemInfo> baseDirectoryPathSupplier,
            string baseDirectoryName,
            ICacheErrorLogger cacheErrorLogger,
            Clock clock = null)
        {
            _version = version;
            _cacheErrorLogger = cacheErrorLogger;
            _baseDirectoryPathSupplier = baseDirectoryPathSupplier;
            _baseDirectoryName = baseDirectoryName;
            _currentState = new State(null, null);
            _clock = clock;
        }

        /// <summary>
        /// Is this storage enabled?
        /// </summary>
        /// <returns>true, if enabled.</returns>
        public bool IsEnabled
        {
            get
            {
                try
                {
                    return Get().IsEnabled;
                }
                catch (IOException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Is this storage external?
        /// </summary>
        /// <returns>true, if external.</returns>
        public bool IsExternal
        {
            get
            {
                try
                {
                    return Get().IsExternal;
                }
                catch (IOException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Get the storage's name, which should be unique.
        /// </summary>
        /// <returns>Name of the this storage.</returns>
        public string StorageName
        {
            get
            {
                try
                {
                    return Get().StorageName;
                }
                catch (IOException)
                {
                    return "";
                }
            }
        }

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
        public virtual IBinaryResource GetResource(string resourceId, object debugInfo)
        {
            return Get().GetResource(resourceId, debugInfo);
        }

        /// <summary>
        /// Does a resource with this name exist?
        /// </summary>
        /// <param name="resourceId">Id of the resource.</param>
        /// <param name="debugInfo">Helper object for debugging.</param>
        /// <returns>
        /// true, if the resource is present in the storage, false otherwise.
        /// </returns>
        public bool Contains(string resourceId, object debugInfo)
        {
            return Get().Contains(resourceId, debugInfo);
        }

        /// <summary>
        /// Does a resource with this name exist? If so, update the last-accessed
        /// time for the resource.
        /// </summary>
        /// <param name="resourceId">Id of the resource.</param>
        /// <param name="debugInfo">Helper object for debugging.</param>
        /// <returns>
        /// true, if the resource is present in the storage, false otherwise.
        /// </returns>
        public virtual bool Touch(string resourceId, object debugInfo)
        {
            return Get().Touch(resourceId, debugInfo);
        }

        /// <summary>
        /// Purge unexpected resources.
        /// </summary>
        public void PurgeUnexpectedResources()
        {
            try
            {
                Get().PurgeUnexpectedResources();
            }
            catch (IOException)
            {
                // this method in fact should throu IOException
                // for now we will swallow the exception as it's done in DefaultDiskStorage
                Debug.WriteLine("purgeUnexpectedResources");
            }
        }

        /// <summary>
        /// Creates a temporary resource for writing content. Split from Commit()
        /// in order to allow concurrent writing of cache entries.
        /// This entry will not be available to cache clients until Commit() is
        /// called passing in the resource returned from this method.
        /// </summary>
        /// <param name="resourceId">Id of the resource.</param>
        /// <param name="debugInfo">Helper object for debugging.</param>
        /// <returns>
        /// The Inserter object with methods to write data, commit or cancel
        /// the insertion.
        /// </returns>
        /// <exception cref="IOException">
        /// On errors during this operation.
        /// </exception>
        public IInserter Insert(string resourceId, object debugInfo)
        {
            return Get().Insert(resourceId, debugInfo);
        }

        /// <summary>
        /// Get all entries currently in the storage.
        /// </summary>
        /// <returns>A collection of entries in storage.</returns>
        public ICollection<IEntry> GetEntries()
        {
            return Get().GetEntries();
        }

        /// <summary>
        /// Remove the resource represented by the entry.
        /// </summary>
        /// <param name="entry">Entry of the resource to delete.</param>
        /// <returns>
        /// Size of deleted file if successfully deleted, -1 otherwise.
        /// </returns>
        public long Remove(IEntry entry)
        {
            return Get().Remove(entry);
        }

        /// <summary>
        /// Remove the resource with specified id.
        /// </summary>
        /// <param name="resourceId">Resource Id.</param>
        /// <returns>
        /// Size of deleted file if successfully deleted, -1 otherwise.
        /// </returns>
        public long Remove(string resourceId)
        {
            return Get().Remove(resourceId);
        }

        /// <summary>
        /// Clear all contents of the storage.
        /// </summary>
        public void ClearAll()
        {
            Get().ClearAll();
        }

        /// <summary>
        /// Gets the disk dump info.
        /// </summary>
        public DiskDumpInfo GetDumpInfo()
        {
            return Get().GetDumpInfo();
        }

        /// <summary>
        /// Gets a concrete disk-storage instance. If nothing has changed since
        /// the last call, then the last state is returned.
        /// </summary>
        /// <returns>
        /// An instance of the appropriate IDiskStorage class.
        /// </returns>
        internal IDiskStorage Get()
        {
            lock (_diskInstanceGate)
            {
                if (ShouldCreateNewStorage())
                {
                    // Discard anything we created
                    DeleteOldStorageIfNecessary();
                    CreateStorage();
                }

                return Preconditions.CheckNotNull(_currentState.DiskStorageDelegate);
            }
        }

        private bool ShouldCreateNewStorage()
        {
            State currentState = _currentState;

            // Refresh the root directory
            if (currentState.RootDirectory != null)
            {
                currentState.RootDirectory.Refresh();
            }

            return (currentState.DiskStorageDelegate == null ||
                currentState.RootDirectory == null ||
                !currentState.RootDirectory.Exists);
        }

        internal void DeleteOldStorageIfNecessary()
        {
            if (_currentState.DiskStorageDelegate != null && _currentState.RootDirectory != null) 
            {
                // LATER: Actually delegate this call to the storage. We shouldn't be
                // making an end-run around it
                FileTree.DeleteRecursively(_currentState.RootDirectory);
            }
        }

        private void CreateStorage()
        {
            DirectoryInfo rootDirectory = new DirectoryInfo(
                Path.Combine(_baseDirectoryPathSupplier.Get().FullName, _baseDirectoryName));

            CreateRootDirectoryIfNecessary(rootDirectory);
            IDiskStorage storage = new DefaultDiskStorage(
                rootDirectory, _version, _cacheErrorLogger, _clock);

            _currentState = new State(rootDirectory, storage);
        }

        internal void CreateRootDirectoryIfNecessary(FileSystemInfo rootDirectory)
        {
            try
            {
                FileUtils.Mkdirs(rootDirectory);
            }
            catch (CreateDirectoryException)
            {
                _cacheErrorLogger.LogError(
                    CacheErrorCategory.WRITE_CREATE_DIR,
                    typeof(DynamicDefaultDiskStorage),
                    "createRootDirectoryIfNecessary");

                throw;
            }

            Debug.WriteLine($"Created cache directory { rootDirectory.FullName }");
        }
    }
}
