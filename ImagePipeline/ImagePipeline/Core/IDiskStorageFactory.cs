using Cache.Disk;

namespace ImagePipeline.Core
{
    /// <summary>
    /// Represents a factory for the DiskStorage to use in the ImagePipeline.
    /// Used by ImagePipelineConfig/Factory
    /// </summary>
    public interface IDiskStorageFactory
    {
        /// <summary>
        /// Returns the <see cref="IDiskStorage"/> from the <see cref="DiskCacheConfig"/>
        /// </summary>
        /// <param name="diskCacheConfig"></param>
        /// <returns></returns>
        IDiskStorage Get(DiskCacheConfig diskCacheConfig);
    }
}
