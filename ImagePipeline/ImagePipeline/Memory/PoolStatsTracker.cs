namespace ImagePipeline.Memory
{
    /// <summary>
    /// Listener that logs pool statistics.
    /// </summary>
    public abstract class PoolStatsTracker
    {
        /// <summary>
        /// The bucket size key in the stats dictionary.
        /// </summary>
        public const string BUCKETS_USED_PREFIX = "buckets_used_";

        /// <summary>
        /// The used count key in the stats dictionary.
        /// </summary>
        public const string USED_COUNT = "used_count";

        /// <summary>
        /// The used bytes key in the stats dictionary.
        /// </summary>
        public const string USED_BYTES = "used_bytes";

        /// <summary>
        /// The free count key in the stats dictionary.
        /// </summary>
        public const string FREE_COUNT = "free_count";

        /// <summary>
        /// The free bytes key in the stats dictionary.
        /// </summary>
        public const string FREE_BYTES = "free_bytes";

        /// <summary>
        /// The soft cap key in the stats dictionary.
        /// </summary>
        public const string SOFT_CAP = "soft_cap";

        /// <summary>
        /// The hard cap key in the stats dictionary.
        /// </summary>
        public const string HARD_CAP = "hard_cap";

        /// <summary>
        /// Set the pool to track.
        /// </summary>
        public abstract void SetBasePool<T>(BasePool<T> basePool);

        /// <summary>
        /// Raise when a bucket is re-used.
        /// </summary>
        public abstract void OnValueReuse(int bucketedSize);

        /// <summary>
        /// Raise when the pool size (sum of the used and the free portions)
        /// exceeds its 'max size' soft cap as specified by the pool params.
        /// </summary>
        public abstract void OnSoftCapReached();

        /// <summary>
        /// Raise when the pool size (sum of the used and the free portions)
        /// exceeds its 'max size' hard cap as specified by the pool params.
        /// </summary>
        public abstract void OnHardCapReached();

        /// <summary>
        /// Raise when a bucket is being allocated.
        /// </summary>
        public abstract void OnAlloc(int size);

        /// <summary>
        /// Raise when a bucket is being freed.
        /// </summary>
        public abstract void OnFree(int sizeInBytes);

        /// <summary>
        /// Raise when a bucket is being released.
        /// </summary>
        public abstract void OnValueRelease(int sizeInBytes);
    }
}
