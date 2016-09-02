using System;

namespace ImagePipeline.Memory
{
    /**
     * Listener that logs pool statistics.
     */
    public abstract class PoolStatsTracker
    {
        public const string BUCKETS_USED_PREFIX = "buckets_used_";
        public const string USED_COUNT = "used_count";
        public const string USED_BYTES = "used_bytes";
        public const string FREE_COUNT = "free_count";
        public const string FREE_BYTES = "free_bytes";
        public const string SOFT_CAP = "soft_cap";
        public const string HARD_CAP = "hard_cap";

        public abstract void SetBasePool<T>(BasePool<T> basePool);

        public abstract void OnValueReuse(int bucketedSize);

        public abstract void OnSoftCapReached();

        public abstract void OnHardCapReached();

        public abstract void OnAlloc(int size);

        public abstract void OnFree(int sizeInBytes);

        public abstract void OnValueRelease(int sizeInBytes);
    }
}
