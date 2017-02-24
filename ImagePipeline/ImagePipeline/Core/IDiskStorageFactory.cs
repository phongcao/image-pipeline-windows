using Cache.Disk;

namespace ImagePipeline.Core
{
    /// <summary>
    /// Represents a factory for the IDiskStorage to use in the ImagePipeline.
    /// Used by ImagePipelineConfig/Factory.
    /// </summary>
    public interface IDiskStorageFactory
    {
        /// <summary>
        /// Returns the <see cref="IDiskStorage"/> from the <see cref="DiskCacheConfig"/>.
        /// </summary>
        IDiskStorage Get(DiskCacheConfig diskCacheConfig);
    }
}
