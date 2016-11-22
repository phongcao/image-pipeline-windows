using Cache.Common;
using FBCore.Common.Disk;
using FBCore.Common.Internal;
using FBCore.Common.Util;
using System.IO;
using Windows.Storage;

namespace Cache.Disk
{
    /// <summary>
    /// Configuration class for a <see cref="DiskStorageCache"/>.
    /// </summary>
    public class DiskCacheConfig
    {
        private readonly int _version;
        private readonly string _baseDirectoryName;
        private readonly ISupplier<FileSystemInfo> _baseDirectoryPathSupplier;
        private readonly long _defaultSizeLimit;
        private readonly long _lowDiskSpaceSizeLimit;
        private readonly long _minimumSizeLimit;
        private readonly IEntryEvictionComparatorSupplier _entryEvictionComparatorSupplier;
        private readonly ICacheErrorLogger _cacheErrorLogger;
        private readonly ICacheEventListener _cacheEventListener;
        private readonly IDiskTrimmableRegistry _diskTrimmableRegistry;
        private readonly bool _indexPopulateAtStartupEnabled;

        /// <summary>
        /// The cache lives in a subdirectory identified by this version.
        /// </summary>
        public int Version
        {
            get
            {
                return _version;
            }
        }

        /// <summary>
        /// The name of the directory where the cache will be located.
        /// </summary>
        public string BaseDirectoryName
        {
            get
            {
                return _baseDirectoryName;
            }
        }

        /// <summary>
        /// The path to the base directory. 
        /// </summary>
        public ISupplier<FileSystemInfo> BaseDirectoryPathSupplier
        {
            get
            {
                return _baseDirectoryPathSupplier;
            }
        }

        /// <summary>
        /// This is the default maximum size of the cache.
        /// </summary>
        public long DefaultSizeLimit
        {
            get
            {
                return _defaultSizeLimit;
            }
        }

        /// <summary>
        /// This is the maximum size of the cache that is used when 
        /// the device is low on disk space.
        /// </summary>
        public long LowDiskSpaceSizeLimit
        {
            get
            {
                return _lowDiskSpaceSizeLimit;
            }
        }

        /// <summary>
        /// This is the maximum size of the cache when the device is 
        /// extremely low on disk space.
        /// </summary>
        public long MinimumSizeLimit
        {
            get
            {
                return _minimumSizeLimit;
            }
        }

        /// <summary>
        /// Provides the logic to determine the eviction order based on 
        /// entry's access time and size
        /// </summary>
        public IEntryEvictionComparatorSupplier EntryEvictionComparatorSupplier
        {
            get
            {
                return _entryEvictionComparatorSupplier;
            }
        }

        /// <summary>
        /// The logger that is used to log errors made by the cache.
        /// </summary>
        public ICacheErrorLogger CacheErrorLogger
        {
            get
            {
                return _cacheErrorLogger;
            }
        }

        /// <summary>
        /// The listener for cache events.
        /// </summary>
        public ICacheEventListener CacheEventListener
        {
            get
            {
                return _cacheEventListener;
            }
        }

        /// <summary>
        /// The class that will contain a registry of caches to be 
        /// trimmed in low disk space conditions.
        /// </summary>
        public IDiskTrimmableRegistry DiskTrimmableRegistry
        {
            get
            {
                return _diskTrimmableRegistry;
            }
        }

        /// <summary>
        /// IndexPopulateAtStartupEnabled
        /// </summary>
        public bool IndexPopulateAtStartupEnabled
        {
            get
            {
                return _indexPopulateAtStartupEnabled;
            }
        }

        internal DiskCacheConfig(Builder builder)
        {
            _version = builder._version;
            _baseDirectoryName = Preconditions.CheckNotNull(builder._baseDirectoryName);
            _baseDirectoryPathSupplier = Preconditions.CheckNotNull(builder._baseDirectoryPathSupplier);
            _defaultSizeLimit = builder._maxCacheSize;
            _lowDiskSpaceSizeLimit = builder._maxCacheSizeOnLowDiskSpace;
            _minimumSizeLimit = builder._maxCacheSizeOnVeryLowDiskSpace;
            _entryEvictionComparatorSupplier = Preconditions.CheckNotNull(builder._entryEvictionComparatorSupplier);
            _cacheErrorLogger = builder._cacheErrorLogger ?? NoOpCacheErrorLogger.Instance;
            _cacheEventListener = builder._cacheEventListener ?? NoOpCacheEventListener.Instance;
            _diskTrimmableRegistry = builder._diskTrimmableRegistry ?? NoOpDiskTrimmableRegistry.Instance;
            _indexPopulateAtStartupEnabled = builder._indexPopulateAtStartupEnabled;
        }

        /// <summary>
        /// Create a new builder.
        ///   <see cref="Builder.SetBaseDirectoryPath"/> or
        ///   <see cref="Builder.SetBaseDirectoryPathSupplier"/>
        ///   or the config won't know where to physically locate the cache.
        /// </summary>
        public static Builder NewBuilder()
        {
            return new Builder();
        }

        /// <summary>
        /// Builder class for <see cref="DiskCacheConfig"/>
        /// </summary>
        public class Builder
        {
            internal int _version = 1;
            internal string _baseDirectoryName = "image_cache";
            internal ISupplier<FileSystemInfo> _baseDirectoryPathSupplier;
            internal long _maxCacheSize = 40 * ByteConstants.MB;
            internal long _maxCacheSizeOnLowDiskSpace = 10 * ByteConstants.MB;
            internal long _maxCacheSizeOnVeryLowDiskSpace = 2 * ByteConstants.MB;
            internal IEntryEvictionComparatorSupplier _entryEvictionComparatorSupplier =
                new DefaultEntryEvictionComparatorSupplier();
            internal ICacheErrorLogger _cacheErrorLogger;
            internal ICacheEventListener _cacheEventListener;
            internal IDiskTrimmableRegistry _diskTrimmableRegistry;
            internal bool _indexPopulateAtStartupEnabled;

            internal Builder()
            {
            }

            /// <summary>
            /// Sets the version.
            ///
            /// <para />The cache lives in a subdirectory identified by this version.
            /// </summary>
            public Builder SetVersion(int version)
            {
                _version = version;
                return this;
            }

            /// <summary>
            /// Sets the name of the directory where the cache will be located.
            /// </summary>
            public Builder SetBaseDirectoryName(string baseDirectoryName)
            {
                _baseDirectoryName = baseDirectoryName;
                return this;
            }

            /// <summary>
            /// Sets the path to the base directory.
            ///
            /// <para />A directory with the given base directory name (see <code> setBaseDirectoryName</code>) will be
            /// appended to this path.
            /// </summary>
            public Builder SetBaseDirectoryPath(FileSystemInfo baseDirectoryPath)
            {
                _baseDirectoryPathSupplier = Suppliers.of(baseDirectoryPath);
                return this;
            }

            /// <summary>
            /// Sets the path to the base directory.
            /// </summary>
            public Builder SetBaseDirectoryPathSupplier(ISupplier<FileSystemInfo> baseDirectoryPathSupplier)
            {
                _baseDirectoryPathSupplier = baseDirectoryPathSupplier;
                return this;
            }

            /// <summary>
            /// This is the default maximum size of the cache.
            /// </summary>
            public Builder SetMaxCacheSize(long maxCacheSize)
            {
                _maxCacheSize = maxCacheSize;
                return this;
            }

            /// <summary>
            /// This is the maximum size of the cache that is used when the device is low on disk space.
            ///
            /// See <see cref="IDiskTrimmable.TrimToMinimum"/>.
            /// </summary>
            public Builder SetMaxCacheSizeOnLowDiskSpace(long maxCacheSizeOnLowDiskSpace)
            {
                _maxCacheSizeOnLowDiskSpace = maxCacheSizeOnLowDiskSpace;
                return this;
            }

            /// <summary>
            /// This is the maximum size of the cache when the device is extremely low on disk space.
            ///
            /// See <see cref="IDiskTrimmable.TrimToNothing"/>.
            /// </summary>
            public Builder SetMaxCacheSizeOnVeryLowDiskSpace(long maxCacheSizeOnVeryLowDiskSpace)
            {
                _maxCacheSizeOnVeryLowDiskSpace = maxCacheSizeOnVeryLowDiskSpace;
                return this;
            }

            /// <summary>
            /// Provides the logic to determine the eviction order based on entry's access time and size
            /// </summary>
            public Builder SetEntryEvictionComparatorSupplier(IEntryEvictionComparatorSupplier supplier)
            {
                _entryEvictionComparatorSupplier = supplier;
                return this;
            }

            /// <summary>
            /// The logger that is used to log errors made by the cache.
            /// </summary>
            public Builder SetCacheErrorLogger(ICacheErrorLogger cacheErrorLogger)
            {
                _cacheErrorLogger = cacheErrorLogger;
                return this;
            }

            /// <summary>
            /// The listener for cache events.
            /// </summary>
            public Builder SetCacheEventListener(ICacheEventListener cacheEventListener)
            {
                _cacheEventListener = cacheEventListener;
                return this;
            }

            /// <summary>
            /// The class that will contain a registry of caches to be trimmed in low disk space conditions.
            ///
            /// <para />See <see cref="IDiskTrimmableRegistry"/>.
            /// </summary>
            public Builder SetDiskTrimmableRegistry(IDiskTrimmableRegistry diskTrimmableRegistry)
            {
                _diskTrimmableRegistry = diskTrimmableRegistry;
                return this;
            }

            /// <summary>
            /// Sets IndexPopulateAtStartupEnabled
            /// </summary>
            /// <param name="indexEnabled"></param>
            /// <returns></returns>
            public Builder SetIndexPopulateAtStartupEnabled(bool indexEnabled)
            {
                _indexPopulateAtStartupEnabled = indexEnabled;
                return this;
            }

            /// <summary>
            /// Builds the <see cref="DiskCacheConfig"/>
            /// </summary>
            public DiskCacheConfig Build()
            {
                if (_baseDirectoryPathSupplier == null)
                {
                    _baseDirectoryPathSupplier = new SupplierImpl<FileSystemInfo>(() =>
                    {
                        return new DirectoryInfo(ApplicationData.Current.LocalCacheFolder.Path);
                    });
                }

                return new DiskCacheConfig(this);
            }
        }
    }
}
