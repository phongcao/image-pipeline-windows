using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Windows.Storage;

namespace FBCore.Common.Statfs
{
    /// <summary>
    /// Helper class that periodically checks the amount of free space available.
    /// <para />To keep the overhead low, it caches the free space information, and
    /// only updates that info after two minutes.
    ///
    /// <para />It is a singleton, and is thread-safe.
    ///
    /// <para />Initialization is delayed until first use, so the first call to any method may incur some
    /// additional cost.
    /// </summary>
    public class StatFsHelper
    {
        /// <summary>
        /// Storage types
        /// </summary>
        public enum StorageType
        {
            /// <summary>
            /// Internal storage
            /// </summary>
            INTERNAL,

            /// <summary>
            /// External storage
            /// </summary>
            EXTERNAL
        };

        private static StatFsHelper _statsFsHelper;

        /// <summary>
        /// Time interval for updating disk information
        /// </summary>
        private static readonly long RESTAT_INTERVAL_MS = (long)TimeSpan.FromMinutes(2).TotalMilliseconds;

        /// <summary>
        /// Internal path
        /// </summary>
        protected volatile StorageFolder _internalPath;

        /// <summary>
        /// External path
        /// </summary>
        protected volatile StorageFolder _externalPath;

        /// <summary>
        /// Internal storage space
        /// </summary>
        protected ulong? _internalStatFs = null;

        /// <summary>
        /// External storage space
        /// </summary>
        protected ulong? _externalStatFs = null;

        private long _lastRestatTime;

        private static readonly object _instanceGate = new object();
        private readonly object _statsUpdateGate = new object();
        private volatile bool _initialized = false;

        /// <summary>
        /// Returns StatFsHelper singleton
        /// </summary>
        public static StatFsHelper Instance
        {
            get
            {
                lock (_instanceGate)
                {
                    if (_statsFsHelper == null)
                    {
                        _statsFsHelper = new StatFsHelper();
                    }

                    return _statsFsHelper;
                }
            }
        }

        /// <summary>
        /// Constructor.
        ///
        /// <para />Initialization is delayed until first use, so we must call <see cref="EnsureInitialized()"/>
        /// when implementing member methods.
        /// </summary>
        protected StatFsHelper()
        {
        }

        /// <summary>
        /// Initialization code that can sometimes take a long time.
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                Monitor.Enter(_statsUpdateGate);

                try
                {
                    if (!_initialized)
                    {
                        _internalPath = ApplicationData.Current.LocalFolder;

                        // Phong Cao: Checking external storage requires 'Removable devices' permission in the
                        // app manifest, skip it for now
                        _externalPath = null;

                        UpdateStats();
                        _initialized = true;
                    }
                }
                finally
                {
                    Monitor.Exit(_statsUpdateGate);
                }
            }
        }

        /// <summary>
        /// Check if free space available in the filesystem is greater than the given threshold.
        /// Note that the free space stats are cached and updated in intervals of RESTAT_INTERVAL_MS.
        /// If the amount of free space has crossed over the threshold since the last update, it will
        /// return incorrect results till the space stats are updated again.
        ///
        /// <param name="storageType">StorageType (internal or external) to test</param>
        /// <param name="freeSpaceThreshold">compare the available free space to this size</param>
        /// @return whether free space is lower than the input freeSpaceThreshold,
        /// returns true if disk information is not available
        /// </summary>
        public bool TestLowDiskSpace(StorageType storageType, long freeSpaceThreshold)
        {
            EnsureInitialized();

            ulong availableStorageSpace = GetAvailableStorageSpace(storageType);
            if (availableStorageSpace > 0)
            {
                return availableStorageSpace < (ulong)freeSpaceThreshold;
            }

            return true;
        }

        /// <summary>
        /// Gets the information about the available storage space
        /// either internal or external depends on the give input
        /// <param name="storageType">Internal or external storage type</param>
        /// @return available space in bytes, 0 if no information is available
        /// </summary>
        public ulong GetAvailableStorageSpace(StorageType storageType)
        {
            EnsureInitialized();
            MaybeUpdateStats();
            ulong? statFS = (storageType == StorageType.INTERNAL) ? _internalStatFs : _externalStatFs;
            return statFS ?? 0;
        }

        /// <summary>
        /// Thread-safe call to update disk stats. Update occurs if the thread is able to acquire
        /// the lock (i.e., no other thread is updating it at the same time), and it has been
        /// at least RESTAT_INTERVAL_MS since the last update.
        /// Assumes that initialization has been completed before this method is called.
        /// </summary>
        private void MaybeUpdateStats()
        {
            // Update the free space if able to get the lock,
            // with a frequency of once in RESTAT_INTERVAL_MS
            if (Monitor.TryEnter(_statsUpdateGate))
            {
                try
                {
                    if ((Environment.TickCount - _lastRestatTime) > RESTAT_INTERVAL_MS)
                    {
                        UpdateStats();
                    }
                }
                finally
                {
                    Monitor.Exit(_statsUpdateGate);
                }
            }
        }

        /// <summary>
        /// Thread-safe call to reset the disk stats.
        /// If we know that the free space has changed recently (for example, if we have
        /// deleted files), use this method to reset the internal state and
        /// start tracking disk stats afresh, resetting the internal timer for updating stats.
        /// </summary>
        public void ResetStats()
        {
            // Update the free space if able to get the lock
            if (Monitor.TryEnter(_statsUpdateGate))
            {
                try
                {
                    EnsureInitialized();
                    UpdateStats();
                }
                finally
                {
                    Monitor.Exit(_statsUpdateGate);
                }
            }
        }

        /// <summary>
        /// (Re)calculate the stats.
        /// It is the callers responsibility to ensure thread-safety.
        /// Assumes that it is called after initialization (or at the end of it).
        /// </summary>
        private void UpdateStats()
        {
            _internalStatFs = UpdateStatsHelper(_internalPath);
            _externalStatFs = UpdateStatsHelper(_externalPath);
            _lastRestatTime = Environment.TickCount;
        }

        /// <summary>
        /// Update stats for a single directory and return the available space for that directory. If the
        /// directory does not exist or the RetrievePropertiesAsync method fails, a null object is returned.
        /// </summary>
        internal virtual ulong? UpdateStatsHelper(StorageFolder dir)
        {
            if (dir == null)
            {
                // The path does not exist, do not track stats for it.
                return null;
            }

            try
            {
                IDictionary<string, object> retrievedProperties = dir.Properties.RetrievePropertiesAsync(
                    new string[] { "System.FreeSpace" }).GetAwaiter().GetResult();

                return (ulong)retrievedProperties["System.FreeSpace"];
            }
            catch (Exception e)
            {
                // The most likely reason for this call to fail is because the provided path 
                // no longer exists. The next call to UpdateStats() will update the statfs object 
                // if the path exists. This will handle the case that a path is unmounted and
                // later remounted (but it has to have been mounted when this object was initialized)
                Debug.WriteLine(e.Message);
                return null;
            }
        }
    }
}
