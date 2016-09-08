using FBCore.Common.References;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// ResourceReleaser for FlexByteArray
    /// </summary>
    public class FlexByteArrayResourceReleaser : IResourceReleaser<byte[]>
    {
        private FlexByteArrayPool _pool;

        /// <summary>
        /// Instantiates the <see cref="FlexByteArrayResourceReleaser"/>.
        /// </summary>
        /// <param name="pool"></param>
        public FlexByteArrayResourceReleaser(FlexByteArrayPool pool)
        {
            _pool = pool;
        }

        /// <summary>
        /// Invokes the Release method of the pool
        /// </summary>
        /// <param name="unused">Byte array</param>
        public void Release(byte[] unused)
        {
            _pool.Release(unused);
        }
    }
}
