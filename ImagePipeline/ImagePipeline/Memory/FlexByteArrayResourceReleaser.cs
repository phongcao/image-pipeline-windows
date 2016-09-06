using FBCore.Common.References;

namespace ImagePipeline.Memory
{
    public class FlexByteArrayResourceReleaser : IResourceReleaser<byte[]>
    {
        private FlexByteArrayPool _pool;

        public FlexByteArrayResourceReleaser(FlexByteArrayPool pool)
        {
            _pool = pool;
        }

        public void Release(byte[] unused)
        {
            _pool.Release(unused);
        }
    }
}
