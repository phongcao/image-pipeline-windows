using ImagePipeline.Memory;
using System.Collections.Generic;

namespace ImagePipeline.Tests.Memory
{
    /**
     * Helper class to get pool stats
    */
    public class PoolStats<T>
    {
        public BasePool<T> Pool { get; set; }

        public int UsedBytes { get; set; }

        public int UsedCount { get; set; }

        public int FreeBytes { get; set; }

        public int FreeCount { get; set; }

        public Dictionary<int, KeyValuePair<int, int>> BucketStats { get; set; }

        public PoolStats(BasePool<T> pool)
        {
            Pool = pool;
            BucketStats = new Dictionary<int, KeyValuePair<int, int>>();
        }

        public void SetPool(BasePool<T> pool)
        {
            Pool = pool;
        }

        /**
         * Refresh all pool stats
         */
        public void Refresh()
        {
            RefreshBasic();
            RefreshBucketStats();
        }

        public void RefreshBasic()
        {
            UsedBytes = Pool.UsedCounter.NumBytes;
            UsedCount = Pool.UsedCounter.Count;
            FreeBytes = Pool.FreeCounter.NumBytes;
            FreeCount = Pool.FreeCounter.Count;
        }

        public void RefreshBucketStats()
        {
            BucketStats.Clear();
            foreach (var entry in Pool.Buckets)
            {
                int bucketedSize = entry.Key;
                Bucket<T> bucket = entry.Value;
                BucketStats.Add(bucketedSize, new KeyValuePair<int, int>(bucket.GetInUseCount(), bucket.GetFreeListSize()));
            }
        }
    }
}
