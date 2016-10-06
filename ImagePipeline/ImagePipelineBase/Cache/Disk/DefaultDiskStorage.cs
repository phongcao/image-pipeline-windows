using System;
using System.IO;

namespace Cache.Disk
{
    /// <summary>
    /// The default disk storage implementation. Subsumes both 'simple' and 'sharded' implementations
    /// via a new SubdirectorySupplier.
    /// </summary>
    public class DefaultDiskStorage
    {
        private const string CONTENT_FILE_EXTENSION = ".cnt";
        private const string TEMP_FILE_EXTENSION = ".tmp";

        private const string DEFAULT_DISK_STORAGE_VERSION_PREFIX = "v2";

        /// <summary>
       /// We use sharding to avoid Samsung's RFS problem, and to avoid having one big directory
       /// containing thousands of files.
       /// This number of directories is large enough based on the following reasoning:
       /// - high usage: 150 photos per day
       /// - such usage will hit Samsung's 6,500 photos cap in 43 days
       /// - 100 buckets will extend that period to 4,300 days which is 11.78 years
       /// </summary>
        private const int SHARDING_BUCKET_COUNT = 100;

        /// <summary>
        /// We will allow purging of any temp files older than this.
        /// </summary>
        private static readonly long TEMP_FILE_LIFETIME_MS = (long)TimeSpan.FromMinutes(30).TotalMilliseconds;

        /// <summary>
        /// The base directory used for the cache
        /// </summary>
        private readonly FileSystemInfo _rootDirectory;

        /// <summary>
       /// True if cache is external
       /// </summary>
        private readonly bool _isExternal;

        /// <summary>
        /// All the sharding occurs inside a version-directory. That allows for easy version upgrade.
        /// When we find a base directory with no version-directory in it, it means that it's a different
        /// version and we should delete the whole directory (including itself) for both reasons:
        /// 1) clear all unusable files 2) avoid Samsung RFS problem that was hit with old implementations
        /// of DiskStorage which used a single directory for all the files.
        /// </summary>
        private readonly FileSystemInfo _versionDirectory;

        //private readonly CacheErrorLogger _cacheErrorLogger;
        //private readonly Clock mClock;
    }
}
