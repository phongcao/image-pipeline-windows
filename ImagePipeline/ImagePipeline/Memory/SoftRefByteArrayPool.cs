using FBCore.Common.Memory;

namespace ImagePipeline.Memory
{
    class SoftRefByteArrayPool : GenericByteArrayPool
    {
        public SoftRefByteArrayPool(
            IMemoryTrimmableRegistry memoryTrimmableRegistry,
            PoolParams poolParams,
            PoolStatsTracker poolStatsTracker) : 
            base(memoryTrimmableRegistry, 
                poolParams, 
                poolStatsTracker)
        {
        }

        protected override Bucket<byte[]> NewBucket(int bucketedSize)
        {
            return new OOMSoftReferenceBucket<byte[]>(
                GetSizeInBytes(bucketedSize),
                _poolParams.MaxNumThreads,
                0);
        }
    }
}
