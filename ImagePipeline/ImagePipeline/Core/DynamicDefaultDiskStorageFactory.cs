using Cache.Disk;

namespace ImagePipeline.Core
{
    /// <summary>
    /// Factory for the default implementation of the DiskStorage.
    /// </summary>
    public class DynamicDefaultDiskStorageFactory : IDiskStorageFactory
    {
        /// <summary>
        /// Returns the <see cref="IDiskStorage"/> from the <see cref="DiskCacheConfig"/>.
        /// </summary>
        public IDiskStorage Get(DiskCacheConfig diskCacheConfig)
        {
            return new DynamicDefaultDiskStorage(
                diskCacheConfig.Version,
                diskCacheConfig.BaseDirectoryPathSupplier,
                diskCacheConfig.BaseDirectoryName,
                diskCacheConfig.CacheErrorLogger);
        }
    }
}
