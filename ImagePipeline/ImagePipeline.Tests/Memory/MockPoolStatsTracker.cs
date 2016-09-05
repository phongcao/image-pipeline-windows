using ImagePipeline.Memory;

namespace ImagePipeline.Tests.Memory
{
    public class MockPoolStatsTracker : PoolStatsTracker
    {
        public override void OnAlloc(int size)
        {
        }

        public override void OnFree(int sizeInBytes)
        {
        }

        public override void OnHardCapReached()
        {
        }

        public override void OnSoftCapReached()
        {
        }

        public override void OnValueRelease(int sizeInBytes)
        {
        }

        public override void OnValueReuse(int bucketedSize)
        {
        }

        public override void SetBasePool<T>(BasePool<T> basePool)
        {
        }
    }
}
