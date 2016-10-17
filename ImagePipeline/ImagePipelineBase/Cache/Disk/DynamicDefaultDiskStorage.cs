using BinaryResource;
using Cache.Common;
using Cache.Disk;
using FBCore.Common.File;
using FBCore.Common.Internal;
using FBCore.Common.Time;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ImagePipelineBase.Cache.Disk
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
        /// Instantiates the <see cref="DynamicDefaultDiskStorage"/>
        /// </summary>
        /// <param name="version"></param>
        /// <param name="baseDirectoryPathSupplier"></param>
        /// <param name="baseDirectoryName"></param>
        /// <param name="cacheErrorLogger"></param>
        /// <param name="clock"></param>
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
        /// @return true, if enabled
        /// </summary>
        public bool Enabled
        {
            get
            {
                try
                {
                    return Get().Enabled;
                }
                catch (IOException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Is this storage external?
        /// @return true, if external
        /// </summary>
        public bool External
        {
            get
            {
                try
                {
                    return Get().External;
                }
                catch (IOException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Get the storage's name, which should be unique
        /// @return name of the this storage
        /// </summary>
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
        /// Get the resource with the specified name
        /// <param name="resourceId">id of the resource</param>
        /// <param name="debugInfo">helper object for debugging</param>
        /// @return the resource with the specified name. NULL if not found
        /// @throws IOException for unexpected behavior.
        /// </summary>
        public virtual IBinaryResource GetResource(string resourceId, object debugInfo)
        {
            return Get().GetResource(resourceId, debugInfo);
        }

        /// <summary>
        /// Does a resource with this name exist?
        /// <param name="resourceId">id of the resource</param>
        /// <param name="debugInfo">helper object for debugging</param>
        /// @return true, if the resource is present in the storage, false otherwise
        /// @throws IOException
        /// </summary>
        public bool Contains(string resourceId, object debugInfo)
        {
            return Get().Contains(resourceId, debugInfo);
        }

        /// <summary>
        /// Does a resource with this name exist? If so, update the last-accessed time for the
        /// resource
        /// <param name="resourceId">id of the resource</param>
        /// <param name="debugInfo">helper object for debugging</param>
        /// @return true, if the resource is present in the storage, false otherwise
        /// @throws IOException
        /// </summary>
        public virtual bool Touch(string resourceId, object debugInfo)
        {
            return Get().Touch(resourceId, debugInfo);
        }

        /// <summary>
        /// Purge unexpected resources
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
        public IInserter Insert(string resourceId, object debugInfo)
        {
            return Get().Insert(resourceId, debugInfo);
        }

        /// <summary>
        /// Get all entries currently in the storage
        /// @return a collection of entries in storage
        /// @throws IOException
        /// </summary>
        public ICollection<IEntry> GetEntries()
        {
            return Get().GetEntries();
        }

        /// <summary>
        /// Remove the resource represented by the entry
        /// <param name="entry">entry of the resource to delete</param>
        /// @return size of deleted file if successfully deleted, -1 otherwise
        /// @throws IOException
        /// </summary>
        public long Remove(IEntry entry)
        {
            return Get().Remove(entry);
        }

        /// <summary>
        /// Remove the resource with specified id
        /// <param name="resourceId"></param>
        /// @return size of deleted file if successfully deleted, -1 otherwise
        /// @throws IOException
        /// </summary>
        public long Remove(string resourceId)
        {
            return Get().Remove(resourceId);
        }

        /// <summary>
        /// Clear all contents of the storage
        /// @exception IOException
        /// @throws IOException
        /// </summary>
        public void ClearAll()
        {
            Get().ClearAll();
        }

        /// <summary>
        /// Gets the disk dump info
        /// </summary>
        public DiskDumpInfo GetDumpInfo()
        {
            return Get().GetDumpInfo();
        }

        /// <summary>
        /// Gets a concrete disk-storage instance. If nothing has changed since the last call, then
        /// the last state is returned
        /// @return an instance of the appropriate DiskStorage class
        /// @throws IOException
        /// </summary>
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
            IDiskStorage storage = new DefaultDiskStorage(rootDirectory, _version, _cacheErrorLogger, _clock);
            _currentState = new State(rootDirectory, storage);
        }

        internal void CreateRootDirectoryIfNecessary(FileSystemInfo rootDirectory)
        {
            try
            {
                FileUtils.Mkdirs(rootDirectory);
            }
            catch (CreateDirectoryException cde)
            {
                _cacheErrorLogger.LogError(
                    CacheErrorCategory.WRITE_CREATE_DIR,
                    typeof(DynamicDefaultDiskStorage),
                    "createRootDirectoryIfNecessary");
                throw cde;
            }

            Debug.WriteLine($"Created cache directory { rootDirectory.FullName }");
        }
    }
}
