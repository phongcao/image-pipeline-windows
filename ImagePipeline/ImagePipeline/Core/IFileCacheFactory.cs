using Cache.Disk;

namespace ImagePipeline.Core
{
    /// <summary>
    /// Represents a factory for the IFileCache to use in the ImagePipeline.
    /// Used by ImagePipelineConfig/Factory
    /// </summary>
    public interface IFileCacheFactory
    {
        /// <summary>
        /// Returns the <see cref="IFileCache"/> from the <see cref="DiskCacheConfig"/>
        /// </summary>
        /// <param name="diskCacheConfig"></param>
        /// <returns></returns>
        IFileCache Get(DiskCacheConfig diskCacheConfig);
    }
}
