using BinaryResource;
using Cache.Common;
using FBCore.Common.Disk;
using FBCore.Common.Statfs;
using FBCore.Common.Time;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cache.Disk
{
    /// <summary>
    /// Cache that manages disk storage.
    /// </summary>
    public class DiskStorageCache : IFileCache, IDiskTrimmable
    {
        /// Any subclass that uses MediaCache/DiskCache's versioning system should
        /// use this constant as the very first entry in their list of versions.
        /// When all subclasses of MediaCache have moved on to subsequent versions
        /// and are no longer using this constant, it can be removed.
        public const int START_OF_VERSIONING = 1;

        private static readonly long FUTURE_TIMESTAMP_THRESHOLD_MS = 
            (long)TimeSpan.FromHours(2).TotalMilliseconds;

        /// <summary>
        /// Force recalculation of the ground truth for filecache size at this
        /// interval.
        /// </summary>
        private static readonly long FILECACHE_SIZE_UPDATE_PERIOD_MS = 
            (long)TimeSpan.FromMinutes(30).TotalMilliseconds;

        /// <summary>
        /// Used for indexPopulateAtStartupEnabled.
        /// </summary>
        private readonly CountdownEvent _countdownEvent = new CountdownEvent(1);

        private const double TRIMMING_LOWER_BOUND = 0.02;
        private const long UNINITIALIZED = -1;
        private const string SHARED_PREFS_FILENAME_PREFIX = "disk_entries_list";

        private readonly long _lowDiskSpaceCacheSizeLimit;
        private readonly long _defaultCacheSizeLimit;
        private long _cacheSizeLimit;

        private readonly ICacheEventListener _cacheEventListener;

        /// <summary>
        /// All resourceId stored on disk (if any).
        /// </summary>
        internal readonly HashSet<string> _resourceIndex;

        private DateTime _cacheSizeLastUpdateTime;

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
        /// Stats about the cache - currently size of the cache (in bytes) and
        /// number of items in the cache.
        /// </summary>
        internal class CacheStats
        {
            /// <summary>
            /// Synchronization object.
            /// </summary>
            private readonly object _statsAccessGate = new object();

            private bool _initialized = false;

            /// <summary>
            /// Size of the cache (in bytes).
            /// </summary>
            private long _size = UNINITIALIZED;

            /// <summary>
            /// Number of items in the cache.
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

        /// <summary>
        /// Instantiates the <see cref="DiskStorageCache"/>.
        /// </summary>
        public DiskStorageCache(
            IDiskStorage diskStorage,
            IEntryEvictionComparatorSupplier entryEvictionComparatorSupplier,
            Params parameters,
            ICacheEventListener cacheEventListener,
            ICacheErrorLogger cacheErrorLogger,
            IDiskTrimmableRegistry diskTrimmableRegistry,
            bool indexPopulateAtStartupEnabled,
            Clock clock = null)
        {
            _lowDiskSpaceCacheSizeLimit = parameters.LowDiskSpaceCacheSizeLimit;
            _defaultCacheSizeLimit = parameters.DefaultCacheSizeLimit;
            _cacheSizeLimit = parameters.DefaultCacheSizeLimit;
            _statFsHelper = StatFsHelper.Instance;

            _storage = diskStorage;

            _entryEvictionComparatorSupplier = entryEvictionComparatorSupplier;

            _cacheSizeLastUpdateTime = default(DateTime);

            _cacheEventListener = cacheEventListener;

            _cacheSizeLimitMinimum = parameters.CacheSizeLimitMinimum;

            _cacheErrorLogger = cacheErrorLogger;

            _cacheStats = new CacheStats();

            if (diskTrimmableRegistry != null)
            {
                diskTrimmableRegistry.RegisterDiskTrimmable(this);
            }

            _clock = clock ?? SystemClock.Get();

            _indexPopulateAtStartupEnabled = indexPopulateAtStartupEnabled;

            _resourceIndex = new HashSet<string>();

            if (_indexPopulateAtStartupEnabled)
            {
                Task.Run(() =>
                {
                    try
                    {
                        lock (_lock)
                        {
                            MaybeUpdateFileCacheSize();
                        }
                    }
                    finally
                    {
                        _countdownEvent.Signal();
                    }
                });
            }
            else
            {
                _countdownEvent.Reset(0);
            }
        }

        /// <summary>
        /// Gets the disk dump info.
        /// </summary>
        public DiskDumpInfo GetDumpInfo()
        {
            return _storage.GetDumpInfo();
        }

        /// <summary>
        /// Is this storage enabled?
        /// </summary>
        /// <returns>true, if enabled.</returns>
        public bool IsEnabled
        {
            get
            {
                return _storage.IsEnabled;
            }
        }

        /// <summary>
        /// Blocks current thread until having finished initialization in
        /// Memory Index. Call only when you need memory index in cold start.
        /// </summary>
        protected internal void AwaitIndex()
        {
            _countdownEvent.Wait();
        }

        /// <summary>
        /// Retrieves the file corresponding to the key, if it is in the cache.
        /// Also touches the item, thus changing its LRU timestamp. If the file
        /// is not present in the file cache, returns null.
        /// <para />
        /// This should NOT be called on the UI thread.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>
        /// The resource if present in cache, otherwise null.
        /// </returns>
        public IBinaryResource GetResource(ICacheKey key)
        {
            SettableCacheEvent cacheEvent = SettableCacheEvent
                .Obtain()
                .SetCacheKey(key);

            try
            {
                lock (_lock)
                {
                    IBinaryResource resource = null;
                    IList<string> resourceIds = CacheKeyUtil.GetResourceIds(key);
                    string resourceId = default(string);
                    foreach (var entry in resourceIds)
                    {
                        resourceId = entry;
                        cacheEvent.SetResourceId(resourceId);
                        resource = _storage.GetResource(resourceId, key);
                        if (resource != null)
                        {
                            break;
                        }
                    }

                    if (resource == null)
                    {
                        _cacheEventListener.OnMiss(cacheEvent);
                        _resourceIndex.Remove(resourceId);
                    }
                    else
                    {
                        _cacheEventListener.OnHit(cacheEvent);
                        _resourceIndex.Add(resourceId);
                    }

                    return resource;
                }
            }
            catch (IOException ioe)
            {
                _cacheErrorLogger.LogError(
                    CacheErrorCategory.GENERIC_IO,
                    typeof(DiskStorageCache),
                    "GetResource");
                cacheEvent.SetException(ioe);
                _cacheEventListener.OnReadException(cacheEvent);
                return null;
            }
            finally
            {
                cacheEvent.Recycle();
            }
        }

        /// <summary>
        /// Probes whether the object corresponding to the key is in the cache.
        /// Note that the act of probing touches the item (if present in cache),
        /// thus changing its LRU timestamp.
        /// <para />
        /// This will be faster than retrieving the object, but it still has
        /// file system accesses and should NOT be called on the UI thread.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>Whether the keyed mValue is in the cache.</returns>
        public bool Probe(ICacheKey key)
        {
            string resourceId = null;

            try
            {
                lock (_lock)
                {
                    IList<string> resourceIds = CacheKeyUtil.GetResourceIds(key);
                    foreach (var entry in resourceIds)
                    {
                        resourceId = entry;
                        if (_storage.Touch(resourceId, key))
                        {
                            _resourceIndex.Add(resourceId);
                            return true;
                        }
                    }

                    return false;
                }
            }
            catch (IOException e)
            {
                SettableCacheEvent cacheEvent = SettableCacheEvent.Obtain()
                    .SetCacheKey(key)
                    .SetResourceId(resourceId)
                    .SetException(e);
                _cacheEventListener.OnReadException(cacheEvent);
                cacheEvent.Recycle();
                return false;
            }
        }

        /// <summary>
        /// Creates a temp file for writing outside the session lock.
        /// </summary>
        private IInserter StartInsert(
            string resourceId,
            ICacheKey key)
        {
            MaybeEvictFilesInCacheDir();
            return _storage.Insert(resourceId, key);
        }

        /// <summary>
        /// Commits the provided temp file to the cache, renaming it to match
        /// the cache's hashing convention.
        /// </summary>
        private IBinaryResource EndInsert(
            IInserter inserter,
            ICacheKey key,
            string resourceId)
        {
            lock (_lock)
            {
                IBinaryResource resource = inserter.Commit(key);
                _resourceIndex.Add(resourceId);
                _cacheStats.Increment(resource.GetSize(), 1);
                return resource;
            }
        }

        /// <summary>
        /// Inserts resource into file with key.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <param name="callback">
        /// Callback that writes to an output stream.
        /// </param>
        /// <returns>A sequence of bytes.</returns>
        public IBinaryResource Insert(ICacheKey key, IWriterCallback callback)
        {
            // Write to a temp file, then move it into place.
            // This allows more parallelism when writing files.
            SettableCacheEvent cacheEvent = SettableCacheEvent.Obtain().SetCacheKey(key);
            _cacheEventListener.OnWriteAttempt(cacheEvent);
            string resourceId;
            lock (_lock)
            {
                // For multiple resource ids associated with the same image,
                // we only write one file
                resourceId = CacheKeyUtil.GetFirstResourceId(key);
            }

            cacheEvent.SetResourceId(resourceId);

            try
            {
                // Getting the file is synchronized
                IInserter inserter = StartInsert(resourceId, key);

                try
                {
                    inserter.WriteData(callback, key);

                    // Committing the file is synchronized
                    IBinaryResource resource = EndInsert(inserter, key, resourceId);
                    cacheEvent.SetItemSize(resource.GetSize())
                        .SetCacheSize(_cacheStats.Size);

                    _cacheEventListener.OnWriteSuccess(cacheEvent);
                    return resource;
                }
                finally
                {
                    if (!inserter.CleanUp())
                    {
                        Debug.WriteLine("Failed to delete temp file");
                    }
                }
            }
            catch (IOException ioe)
            {
                cacheEvent.SetException(ioe);
                _cacheEventListener.OnWriteException(cacheEvent);
                Debug.WriteLine("Failed inserting a file into the cache");
                throw;
            }
            finally
            {
                cacheEvent.Recycle();
            }
        }

        /// <summary>
        /// Removes a resource by key from cache.
        /// </summary>
        /// <param name="key">Cache key.</param>
        public void Remove(ICacheKey key)
        {
            lock (_lock)
            {
                try
                {
                    IList<string> resourceIds = CacheKeyUtil.GetResourceIds(key);
                    foreach (var resourceId in resourceIds)
                    {
                        _storage.Remove(resourceId);
                        _resourceIndex.Remove(resourceId);
                    }
                }
                catch (IOException e)
                {
                    _cacheErrorLogger.LogError(
                        CacheErrorCategory.DELETE_FILE,
                        typeof(DiskStorageCache),
                        "delete: " + e.Message);
                }
            }
        }

        /// <summary>
        /// Deletes old cache files.
        /// </summary>
        /// <param name="cacheExpirationMs">
        /// Files older than this will be deleted.
        /// </param>
        /// <returns>
        /// The age in ms of the oldest file remaining in the cache.
        /// </returns>
        public long ClearOldEntries(long cacheExpirationMs)
        {
            long oldestRemainingEntryAgeMs = 0L;

            lock (_lock)
            {
                try
                {
                    DateTime now = _clock.Now;
                    ICollection<IEntry> allEntries = _storage.GetEntries();
                    long cacheSizeBeforeClearance = _cacheStats.Size;
                    int itemsRemovedCount = 0;
                    long itemsRemovedSize = 0L;
                    foreach (IEntry entry in allEntries)
                    {
                        // Entry age of zero is disallowed.
                        long entryAgeMs = Math.Max(1, Math.Abs((long)(now - entry.Timestamp).TotalMilliseconds));
                        if (entryAgeMs >= cacheExpirationMs)
                        {
                            long entryRemovedSize = _storage.Remove(entry);
                            _resourceIndex.Remove(entry.Id);
                            if (entryRemovedSize > 0)
                            {
                                itemsRemovedCount++;
                                itemsRemovedSize += entryRemovedSize;
                                SettableCacheEvent cacheEvent = SettableCacheEvent.Obtain()
                                    .SetResourceId(entry.Id)
                                    .SetEvictionReason(EvictionReason.CONTENT_STALE)
                                    .SetItemSize(entryRemovedSize)
                                    .SetCacheSize(cacheSizeBeforeClearance - itemsRemovedSize);
                                _cacheEventListener.OnEviction(cacheEvent);
                                cacheEvent.Recycle();
                            }
                        }
                        else
                        {
                            oldestRemainingEntryAgeMs = Math.Max(oldestRemainingEntryAgeMs, entryAgeMs);
                        }
                    }

                    _storage.PurgeUnexpectedResources();
                    if (itemsRemovedCount > 0)
                    {
                        MaybeUpdateFileCacheSize();
                        _cacheStats.Increment(-itemsRemovedSize, -itemsRemovedCount);
                    }
                }
                catch (IOException ioe)
                {
                    _cacheErrorLogger.LogError(
                        CacheErrorCategory.EVICTION,
                        typeof(DiskStorageCache),
                        "clearOldEntries: " + ioe.Message);
                }
            }

            return oldestRemainingEntryAgeMs;
        }

        /// <summary>
        /// Test if the cache size has exceeded its limits, and if so,
        /// evict some files. It also calls MaybeUpdateFileCacheSize
        ///
        /// This method uses _lock for synchronization purposes.
        /// </summary>
        private void MaybeEvictFilesInCacheDir()
        {
            lock (_lock)
            {
                bool calculatedRightNow = MaybeUpdateFileCacheSize();

                // Update the size limit (mCacheSizeLimit)
                UpdateFileCacheSizeLimit();

                long cacheSize = _cacheStats.Size;

                // If we are going to evict force a recalculation of the size
                // (except if it was already calculated!)
                if (cacheSize > _cacheSizeLimit && !calculatedRightNow)
                {
                    _cacheStats.Reset();
                    MaybeUpdateFileCacheSize();
                }

                // If size has exceeded the size limit, evict some files
                if (cacheSize > _cacheSizeLimit)
                {
                    EvictAboveSize(
                        _cacheSizeLimit * 9 / 10,
                        EvictionReason.CACHE_FULL); // 90%
                }
            }
        }

        private void EvictAboveSize(
            long desiredSize,
            EvictionReason reason)
        {
            ICollection<IEntry> entries;

            try
            {
                entries = GetSortedEntries(_storage.GetEntries());
            }
            catch (IOException ioe)
            {
                _cacheErrorLogger.LogError(
                    CacheErrorCategory.EVICTION,
                    typeof(DiskStorageCache),
                    "evictAboveSize: " + ioe.Message);

                throw;
            }

            long cacheSizeBeforeClearance = _cacheStats.Size;
            long deleteSize = cacheSizeBeforeClearance - desiredSize;
            int itemCount = 0;
            long sumItemSizes = 0L;
            foreach (var entry in entries)
            {
                if (sumItemSizes > (deleteSize))
                {
                    break;
                }

                long deletedSize = _storage.Remove(entry);
                _resourceIndex.Remove(entry.Id);
                if (deletedSize > 0)
                {
                    itemCount++;
                    sumItemSizes += deletedSize;
                    SettableCacheEvent cacheEvent = SettableCacheEvent.Obtain()
                        .SetResourceId(entry.Id)
                        .SetEvictionReason(reason)
                        .SetItemSize(deletedSize)
                        .SetCacheSize(cacheSizeBeforeClearance - sumItemSizes)
                        .SetCacheLimit(desiredSize);
                    _cacheEventListener.OnEviction(cacheEvent);
                    cacheEvent.Recycle();
                }
            }

            _cacheStats.Increment(-sumItemSizes, -itemCount);
            _storage.PurgeUnexpectedResources();
        }

        /// <summary>
        /// If any file timestamp is in the future
        /// (beyond now + FUTURE_TIMESTAMP_THRESHOLD_MS), we will set its
        /// effective timestamp to 0 (the beginning of unix time), thus
        /// sending it to the head of the queue for eviction (entries with
        /// the lowest timestamps are evicted first). This is a safety check
        /// in case we get files that are written with a future timestamp.
        /// We are adding a small delta (this constant) to account for
        /// network time changes, timezone changes, etc.
        /// </summary>
        private ICollection<IEntry> GetSortedEntries(ICollection<IEntry> allEntries)
        {
            DateTime threshold = _clock.Now.AddMilliseconds(FUTURE_TIMESTAMP_THRESHOLD_MS);
            List<IEntry> sortedList = new List<IEntry>(allEntries.Count);
            List<IEntry> listToSort = new List<IEntry>(allEntries.Count);
            foreach (var entry in allEntries)
            {
                if (entry.Timestamp > threshold)
                {
                    sortedList.Add(entry);
                }
                else
                {
                    listToSort.Add(entry);
                }
            }

            listToSort.Sort(_entryEvictionComparatorSupplier.Get());
            sortedList.AddRange(listToSort);
            return sortedList;
        }

        /// <summary>
        /// Helper method that sets the cache size limit to be either a high,
        /// or a low limit. If there is not enough free space to satisfy the
        /// high limit, it is set to the low limit.
        /// </summary>
        private void UpdateFileCacheSizeLimit()
        {
            // Test if _cacheSizeLimit can be set to the high limit
            bool isAvailableSpaceLowerThanHighLimit;

            StatFsHelper.StorageType storageType = _storage.IsExternal ? 
                StatFsHelper.StorageType.EXTERNAL : 
                StatFsHelper.StorageType.INTERNAL;

            isAvailableSpaceLowerThanHighLimit = _statFsHelper.TestLowDiskSpace(
                storageType, _defaultCacheSizeLimit - _cacheStats.Size);

            if (isAvailableSpaceLowerThanHighLimit)
            {
                _cacheSizeLimit = _lowDiskSpaceCacheSizeLimit;
            }
            else
            {
                _cacheSizeLimit = _defaultCacheSizeLimit;
            }
        }

        /// <summary>
        /// Returns the size of the cache (in bytes) in the cache.
        /// </summary>
        public long Size
        {
            get
            {
                return _cacheStats.Size;
            }
        }

        /// <summary>
        /// Returns the number of items in the cache.
        /// </summary>
        public long Count
        {
            get
            {
                return _cacheStats.Count;
            }
        }

        /// <summary>
        /// Clears all items in the cache.
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                try
                {
                    _storage.ClearAll();
                    _resourceIndex.Clear();
                    _cacheEventListener.OnCleared();
                }
                catch (IOException ioe)
                {
                    _cacheErrorLogger.LogError(
                        CacheErrorCategory.EVICTION,
                        typeof(DiskStorageCache),
                        "clearAll: " + ioe.Message);
                }

                _cacheStats.Reset();
            }
        }

        /// <summary>
        /// Returns true if the key is in the in-memory key index.
        ///
        /// Not guaranteed to be correct. The cache may yet have this key
        /// even if this returns false. But if it returns true, it definitely
        /// has it.
        ///
        /// Avoids a disk read.
        /// </summary>
        public bool HasKeySync(ICacheKey key)
        {
            lock (_lock)
            {
                IList<string> resourceIds = CacheKeyUtil.GetResourceIds(key);
                foreach (var resourceId in resourceIds)
                {
                    if (_resourceIndex.Contains(resourceId))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Returns true if the key is in the in-memory key index.
        /// </summary>
        public bool HasKey(ICacheKey key)
        {
            lock (_lock)
            {
                if (HasKeySync(key))
                {
                    return true;
                }

                try
                {
                    IList<string> resourceIds = CacheKeyUtil.GetResourceIds(key);
                    foreach (var resourceId in resourceIds)
                    {
                        if (_storage.Contains(resourceId, key))
                        {
                            _resourceIndex.Add(resourceId);
                            return true;
                        }
                    }

                    return false;
                }
                catch (IOException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Called when there is very little disk space left.
        /// </summary>
        public void TrimToMinimum()
        {
            lock (_lock)
            {
                MaybeUpdateFileCacheSize();
                long cacheSize = _cacheStats.Size;
                if (_cacheSizeLimitMinimum <= 0 || cacheSize <= 0 || cacheSize < _cacheSizeLimitMinimum)
                {
                    return;
                }

                double trimRatio = 1 - _cacheSizeLimitMinimum / cacheSize;
                if (trimRatio > TRIMMING_LOWER_BOUND)
                {
                    TrimBy(trimRatio);
                }
            }
        }

        /// <summary>
        /// Called when there is almost no disk space left and the app is
        /// likely to crash soon.
        /// </summary>
        public void TrimToNothing()
        {
            ClearAll();
        }

        private void TrimBy(double trimRatio)
        {
            lock (_lock)
            {
                try
                {
                    // Force update the ground truth if we are about to evict
                    _cacheStats.Reset();
                    MaybeUpdateFileCacheSize();
                    long cacheSize = _cacheStats.Size;
                    long newMaxBytesInFiles = cacheSize - (long)(trimRatio * cacheSize);
                    EvictAboveSize(newMaxBytesInFiles, EvictionReason.CACHE_MANAGER_TRIMMED);
                }
                catch (IOException ioe)
                {
                    _cacheErrorLogger.LogError(
                        CacheErrorCategory.EVICTION,
                        typeof(DiskStorageCache),
                        "trimBy: " + ioe.Message);
                }
            }
        }

        /// <summary>
        /// If file cache size is not calculated or if it was calculated
        /// a long time ago (FILECACHE_SIZE_UPDATE_PERIOD_MS) recalculated
        /// from file listing.
        /// </summary>
        /// <returns>
        /// true if it was recalculated, false otherwise.
        /// </returns>
        private bool MaybeUpdateFileCacheSize()
        {
            bool result = false;
            DateTime now = _clock.Now;
            if ((!_cacheStats.Initialized) ||
                _cacheSizeLastUpdateTime == default(DateTime) ||
                Math.Abs((now - _cacheSizeLastUpdateTime).TotalMilliseconds) > FILECACHE_SIZE_UPDATE_PERIOD_MS)
            {
                MaybeUpdateFileCacheSizeAndIndex();
                _cacheSizeLastUpdateTime = now;
                result = true;
            }
            return result;
        }

        private void MaybeUpdateFileCacheSizeAndIndex()
        {
            long size = 0;
            int count = 0;
            bool foundFutureTimestamp = false;
            int numFutureFiles = 0;
            long sizeFutureFiles = 0;
            long maxTimeDelta = -1;
            DateTime now = _clock.Now;
            DateTime timeThreshold = now.AddMilliseconds(FUTURE_TIMESTAMP_THRESHOLD_MS);
            HashSet<string> tempResourceIndex;
            if (_indexPopulateAtStartupEnabled && _resourceIndex.Count == 0)
            {
                tempResourceIndex = _resourceIndex;
            }
            else if (_indexPopulateAtStartupEnabled)
            {
                tempResourceIndex = new HashSet<string>();
            }
            else
            {
                tempResourceIndex = null;
            }

            try
            {
                ICollection<IEntry> entries = _storage.GetEntries();
                foreach (var entry in entries)
                {
                    count++;
                    size += entry.GetSize();

                    //Check if any files have a future timestamp, beyond our threshold
                    if (entry.Timestamp > timeThreshold)
                    {
                        foundFutureTimestamp = true;
                        numFutureFiles++;
                        sizeFutureFiles += entry.GetSize();
                        maxTimeDelta = Math.Max(
                            Math.Abs((long)(entry.Timestamp - now).TotalMilliseconds), maxTimeDelta);
                    }
                    else if (_indexPopulateAtStartupEnabled)
                    {
                        tempResourceIndex.Add(entry.Id);
                    }
                }

                if (foundFutureTimestamp)
                {
                    _cacheErrorLogger.LogError(
                        CacheErrorCategory.READ_INVALID_ENTRY,
                        typeof(DiskStorageCache),
                        "Future timestamp found in " + numFutureFiles +
                            " files , with a total size of " + sizeFutureFiles +
                            " bytes, and a maximum time delta of " + maxTimeDelta + "ms");
                }

                if ((_cacheStats.Count != count || _cacheStats.Size != size))
                {
                    if (_indexPopulateAtStartupEnabled && _resourceIndex != tempResourceIndex)
                    {
                        _resourceIndex.Clear();
                        _resourceIndex.UnionWith(tempResourceIndex);
                    }

                    _cacheStats.Set(size, count);
                }
            }
            catch (IOException ioe)
            {
                _cacheErrorLogger.LogError(
                    CacheErrorCategory.GENERIC_IO,
                    typeof(DiskStorageCache),
                    "calcFileCacheSize: " + ioe.Message);
            }
        }
    }
}
