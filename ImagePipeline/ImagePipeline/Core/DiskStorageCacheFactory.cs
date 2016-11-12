using Cache.Disk;

namespace ImagePipeline.Core
{
    /// <summary>
    /// Factory for the default implementation of the <see cref="IFileCache"/>.
    /// </summary>
    public class DiskStorageCacheFactory : IFileCacheFactory
    {
        private IDiskStorageFactory _diskStorageFactory;

        /// <summary>
        /// Instantiates the <see cref="DiskStorageCacheFactory"/>
        /// </summary>
        /// <param name="diskStorageFactory"></param>
        public DiskStorageCacheFactory(IDiskStorageFactory diskStorageFactory)
        {
            _diskStorageFactory = diskStorageFactory;
        }

        /// <summary>
        /// Builds the <see cref="DiskStorageCache"/>
        /// </summary>
        /// <param name="diskCacheConfig"></param>
        /// <param name="diskStorage"></param>
        /// <returns></returns>
        public static DiskStorageCache BuildDiskStorageCache(
            DiskCacheConfig diskCacheConfig,
            IDiskStorage diskStorage)
        {
            Params parameters = new Params(
                diskCacheConfig.MinimumSizeLimit,
                diskCacheConfig.LowDiskSpaceSizeLimit,
                diskCacheConfig.DefaultSizeLimit);

            return new DiskStorageCache(
                diskStorage,
                diskCacheConfig.EntryEvictionComparatorSupplier,
                parameters,
                diskCacheConfig.CacheEventListener,
                diskCacheConfig.CacheErrorLogger,
                diskCacheConfig.DiskTrimmableRegistry,
                diskCacheConfig.IndexPopulateAtStartupEnabled);
        }

        /// <summary>
        /// Returns the <see cref="IFileCache"/> from the <see cref="DiskCacheConfig"/>
        /// </summary>
        public IFileCache Get(DiskCacheConfig diskCacheConfig)
        {
            return BuildDiskStorageCache(diskCacheConfig, _diskStorageFactory.Get(diskCacheConfig));
        }
    }
}
