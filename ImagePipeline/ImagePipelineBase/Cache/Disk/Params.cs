namespace Cache.Disk
{
    /// <summary>
    /// This class holds the params for the <see cref="DiskStorageCache"/> object.
    /// </summary>
    public class Params
    {
        /// <summary>
        /// Min cache size limit.
        /// </summary>
        public long CacheSizeLimitMinimum { get; }

        /// <summary>
        /// Cache size limit when there is low disk space.
        /// </summary>
        public long LowDiskSpaceCacheSizeLimit { get; }

        /// <summary>
        /// Default cache size limit.
        /// </summary>
        public long DefaultCacheSizeLimit { get; }

        /// <summary>
        /// Instantiates the <see cref="Params"/>.
        /// </summary>
        public Params(
            long cacheSizeLimitMinimum,
            long lowDiskSpaceCacheSizeLimit,
            long defaultCacheSizeLimit)
        {
            CacheSizeLimitMinimum = cacheSizeLimitMinimum;
            LowDiskSpaceCacheSizeLimit = lowDiskSpaceCacheSizeLimit;
            DefaultCacheSizeLimit = defaultCacheSizeLimit;
        }
    }
}
