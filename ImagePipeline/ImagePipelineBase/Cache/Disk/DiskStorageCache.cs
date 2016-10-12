using Cache.Common;
using Cache.Disk;
using FBCore.Common.Disk;
using FBCore.Common.Statfs;
using FBCore.Common.Time;
using System;
using System.Collections.Generic;

namespace ImagePipelineBase.Cache.Disk
{
    /// <summary>
    /// Cache that manages disk storage.
    /// </summary>
    public class DiskStorageCache //: IFileCache, IDiskTrimmable
    {
        /// Any subclass that uses MediaCache/DiskCache's versioning system should use this
        /// constant as the very first entry in their list of versions. When all
        /// subclasses of MediaCache have moved on to subsequent versions and are
        /// no longer using this constant, it can be removed.
        public const int START_OF_VERSIONING = 1;

        private static readonly long FUTURE_TIMESTAMP_THRESHOLD_MS = (long)TimeSpan.FromHours(2).TotalMilliseconds;

        /// <summary>
        /// Force recalculation of the ground truth for filecache size at this interval
        /// </summary>
        private static readonly long FILECACHE_SIZE_UPDATE_PERIOD_MS = (long)TimeSpan.FromMinutes(30).TotalMilliseconds;

        private const double TRIMMING_LOWER_BOUND = 0.02;
        private const long UNINITIALIZED = -1;
        private const string SHARED_PREFS_FILENAME_PREFIX = "disk_entries_list";

        private readonly long _lowDiskSpaceCacheSizeLimit;
        private readonly long _defaultCacheSizeLimit;
        //private readonly CountDownLatch mCountDownLatch;
        private long _cacheSizeLimit;

        private readonly ICacheEventListener _cacheEventListener;

        /// <summary>
        /// All resourceId stored on disk (if any).
        /// </summary>
        internal readonly HashSet<string> _resourceIndex;

        private long _cacheSizeLastUpdateTime;

        private readonly long _cacheSizeLimitMinimum;

        private readonly StatFsHelper _statFsHelper;

        private readonly IDiskStorage _storage;
        private readonly IEntryEvictionComparatorSupplier _entryEvictionComparatorSupplier;
        private readonly ICacheErrorLogger _cacheErrorLogger;
        private readonly bool _indexPopulateAtStartupEnabled;

        private readonly CacheStats _cacheStats;

        private readonly Clock _clock;

        /// <summary>
        /// Synchronization object.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// Stats about the cache - currently size of the cache (in bytes) and number of items in
        /// the cache
        /// </summary>
        internal class CacheStats
        {
            /// <summary>
            /// Synchronization object.
            /// </summary>
            private readonly object _statsAccessGate = new object();

            private bool _initialized = false;

            /// <summary>
            /// Size of the cache (in bytes)
            /// </summary>
            private long _size = UNINITIALIZED;

            /// <summary>
            /// Number of items in the cache
            /// </summary>
            private long _count = UNINITIALIZED;

            public bool Initialized
            {
                get
                {
                    lock (_statsAccessGate)
                    {
                        return _initialized;
                    }
                }
            }

            public void Reset()
            {
                lock (_statsAccessGate)
                {
                    _initialized = false;
                    _count = UNINITIALIZED;
                    _size = UNINITIALIZED;
                }
            }

            public void Set(long size, long count)
            {
                lock (_statsAccessGate)
                {
                    _count = count;
                    _size = size;
                    _initialized = true;
                }
            }

            public void Increment(long sizeIncrement, long countIncrement)
            {
                lock (_statsAccessGate)
                {
                    if (_initialized)
                    {
                        _size += sizeIncrement;
                        _count += countIncrement;
                    }
                }
            }

            public long Size
            {
                get
                {
                    lock (_statsAccessGate)
                    {
                        return _size;
                    }
                }
            }

            public long Count
            {
                get
                {
                    lock (_statsAccessGate)
                    {
                        return _count;
                    }
                }
            }
        }
    }
}
