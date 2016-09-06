using FBCore.Common.Internal;
using FBCore.Common.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ImagePipeline.Memory
{
    public abstract class BasePool<T> : IPool<T>
    {
        // Init lock
        private readonly object _poolGate = new object();

        /**
         * Determines if new buckets can be created
         */
        private bool _allowNewBuckets;

        /**
         * The memory manager to register with
         */
        protected internal readonly IMemoryTrimmableRegistry _memoryTrimmableRegistry;

        /**
        * Provider for pool parameters
        */
        protected internal readonly PoolParams _poolParams;

        protected internal readonly PoolStatsTracker _poolStatsTracker;

        /**
        * The buckets - representing different 'sizes'
        */
        public Dictionary<int, Bucket<T>> Buckets { get; }

        /**
         * An Identity hash-set to keep track of values by reference equality
         */
        public HashSet<T> InUseValues { get; }

        /**
         * Tracks 'used space' - space allocated via the pool
         */
        internal Counter UsedCounter { get; }

        /**
        * Tracks 'free space' in the pool
        */
        internal Counter FreeCounter { get; }

        /**
         * Creates a new instance of the pool.
         * @param poolParams pool parameters
         * @param poolStatsTracker
         */
        public BasePool(
            IMemoryTrimmableRegistry memoryTrimmableRegistry,
            PoolParams poolParams,
            PoolStatsTracker poolStatsTracker)
        {
            _memoryTrimmableRegistry = Preconditions.CheckNotNull(memoryTrimmableRegistry);
            _poolParams = Preconditions.CheckNotNull(poolParams);
            _poolStatsTracker = Preconditions.CheckNotNull(poolStatsTracker);

            // Initialize the buckets
            Buckets = new Dictionary<int, Bucket<T>>();
            InitBuckets(new Dictionary<int, int>(0));

            InUseValues = new HashSet<T>();

            FreeCounter = new Counter();
            UsedCounter = new Counter();
        }

        /**
         * Finish pool initialization.
         */
        protected void Initialize()
        {
            _memoryTrimmableRegistry.RegisterMemoryTrimmable(this);
            _poolStatsTracker.SetBasePool(this);
        }

        /**
         * Gets a new 'value' from the pool, if available. Allocates a new value if necessary.
         * If we need to perform an allocation,
         *   - If the pool size exceeds the max-size soft cap, then we attempt to trim the free portion
         *     of the pool.
         *   - If the pool size exceeds the max-size hard-cap (after trimming), then we throw an
         *     {@link PoolSizeViolationException}
         * Bucket length constraints are not considered in this function
         * @param size the logical size to allocate
         * @return a new value
         * @throws InvalidSizeException
         */
        public T Get(int size)
        {
            EnsurePoolSizeInvariant();

            int bucketedSize = GetBucketedSize(size);
            int sizeInBytes = -1;

            lock (_poolGate)
            {
                Bucket<T> bucket = GetBucket(bucketedSize);

                if (bucket != null)
                {
                    // Find an existing value that we can reuse
                    T val = bucket.Get();
                    if (val != null)
                    {
                        Preconditions.CheckState(InUseValues.Add(val));

                        // It is possible that we got a 'larger' value than we asked for.
                        // lets recompute size in bytes here
                        bucketedSize = GetBucketedSizeForValue(val);
                        sizeInBytes = GetSizeInBytes(bucketedSize);
                        UsedCounter.Increment(sizeInBytes);
                        FreeCounter.Decrement(sizeInBytes);
                        _poolStatsTracker.OnValueReuse(sizeInBytes);
                        LogStats();
                        Debug.WriteLine($"get (reuse) (object, size) = ({ val.GetHashCode() }, { bucketedSize })");
                        return val;
                    }
                    // Fall through
                }

                // Check to see if we can allocate a value of the given size without exceeding the hard cap
                sizeInBytes = GetSizeInBytes(bucketedSize);
                if (!CanAllocate(sizeInBytes))
                {
                    throw new PoolSizeViolationException(
                        _poolParams.MaxSizeHardCap,
                        UsedCounter.NumBytes,
                        FreeCounter.NumBytes,
                        sizeInBytes);
                }

                // Optimistically assume that allocation succeeds - if it fails, we need to undo those changes
                UsedCounter.Increment(sizeInBytes);
                if (bucket != null)
                {
                    bucket.IncrementInUseCount();
                }
            }

            T value = default(T);
            try
            {
                // allocate the value outside the synchronized block, because it can be pretty expensive
                // we could have done the allocation inside the synchronized block,
                // but that would have blocked out other operations on the pool
                value = Alloc(bucketedSize);
            }
            catch (Exception e)
            {
                // Assumption we made previously is not valid - allocation failed. We need to fix internal
                // counters.
                lock(_poolGate)
                {
                    UsedCounter.Decrement(sizeInBytes);
                    Bucket<T> bucket = GetBucket(bucketedSize);
                    if (bucket != null)
                    {
                        bucket.DecrementInUseCount();
                    }
                }

                throw e;
            }

            // NOTE: We checked for hard caps earlier, and then did the alloc above. Now we need to
            // update state - but it is possible that a concurrent thread did a similar operation - with
            // the result being that we're now over the hard cap.
            // We are willing to live with that situation - especially since the trim call below should
            // be able to trim back memory usage.
            lock (_poolGate)
            {
                Preconditions.CheckState(InUseValues.Add(value));

                // If we're over the pool's max size, try to trim the pool appropriately
                TrimToSoftCap();

                _poolStatsTracker.OnAlloc(sizeInBytes);
                LogStats();
                Debug.WriteLine($"get (alloc) (object, size) = ({ value.GetHashCode() }, { bucketedSize })");
            }

            return value;
        }

        /**
         * Releases the given value to the pool.
         * In a few cases, the value is 'freed' instead of being released to the pool. If
         *   - the pool currently exceeds its max size OR
         *   - if the value does not map to a bucket that's currently maintained by the pool, OR
         *   - if the bucket for the value exceeds its maxLength, OR
         *   - if the value is not recognized by the pool
         *  then, the value is 'freed'.
         * @param value the value to release to the pool
         */
        public void Release(T value)
        {
            Preconditions.CheckNotNull(value);

            int bucketedSize = GetBucketedSizeForValue(value);
            int sizeInBytes = GetSizeInBytes(bucketedSize);
            lock (_poolGate)
            {
                Bucket<T> bucket = GetBucket(bucketedSize);
                if (!InUseValues.Remove(value))
                {
                    // This value was not 'known' to the pool (i.e.) allocated via the pool.
                    // Something is going wrong, so let's free the value and report soft error.
                    Debug.WriteLine($"release (free, value unrecognized) (object, size) = ({ value.GetHashCode() }, { bucketedSize })");
                    Free(value);
                    _poolStatsTracker.OnFree(sizeInBytes);
                }
                else
                {
                    // free the value, if
                    //  - pool exceeds maxSize
                    //  - there is no bucket for this value
                    //  - there is a bucket for this value, but it has exceeded its maxLength
                    //  - the value is not reusable
                    // If no bucket was found for the value, simply free it
                    // We should free the value if no bucket is found, or if the bucket length cap is exceeded.
                    // However, if the pool max size softcap is exceeded, it may not always be best to free
                    // *this* value.
                    if (bucket == null ||
                        bucket.IsMaxLengthExceeded() ||
                        IsMaxSizeSoftCapExceeded() ||
                        !IsReusable(value))
                    {
                        if (bucket != null)
                        {
                            bucket.DecrementInUseCount();
                        }

                        Debug.WriteLine($"release (free) (object, size) = ({ value.GetHashCode() }, { bucketedSize })");
                        Free(value);
                        UsedCounter.Decrement(sizeInBytes);
                        _poolStatsTracker.OnFree(sizeInBytes);
                    }
                    else
                    {
                        bucket.Release(value);
                        FreeCounter.Increment(sizeInBytes);
                        UsedCounter.Decrement(sizeInBytes);
                        _poolStatsTracker.OnValueRelease(sizeInBytes);
                        Debug.WriteLine($"release (reuse) (object, size) = ({ value.GetHashCode() }, { bucketedSize })");
                    }
                }

                LogStats();
            }
        }

        /**
         * Trims the pool in response to low-memory states (invoked from MemoryManager)
         * For now, we'll do the simplest thing, and simply clear out the entire pool. We may consider
         * more sophisticated approaches later.
         * In other words, we ignore the memoryTrimType parameter
         * @param memoryTrimType the kind of trimming we want to perform
         */
        public void Trim(double memoryTrimType)
        {
            TrimToNothing();
        }

        /**
         * Allocates a new 'value' with the given size
         * @param bucketedSize the logical size to allocate
         * @return a new value
         */
        protected internal abstract T Alloc(int bucketedSize);

        /**
         * Frees the 'value'
         * @param value the value to free
         */
        protected internal abstract void Free(T value);

        /**
         * Gets the bucketed size (typically something the same or larger than the requested size)
         * @param requestSize the logical request size
         * @return the 'bucketed' size
         * @throws InvalidSizeException, if the size of the value doesn't match the pool's constraints
         */
        protected internal abstract int GetBucketedSize(int requestSize);

        /**
         * Gets the bucketed size of the value
         * @param value the value
         * @return bucketed size of the value
         * @throws InvalidSizeException, if the size of the value doesn't match the pool's constraints
         * @throws InvalidValueException, if the value is invalid
         */
        protected internal abstract int GetBucketedSizeForValue(T value);

        /**
         * Gets the size in bytes for the given bucketed size
         * @param bucketedSize the bucketed size
         * @return size in bytes
         */
        protected internal abstract int GetSizeInBytes(int bucketedSize);

        /**
         * The pool parameters may have changed. Subclasses can override this to update any state they
         * were maintaining
         */
        protected void OnParamsChanged()
        {
        }

        /**
         * Determines if the supplied value is 'reusable'.
         * This is called during {@link #release(Object)}, and determines if the value can be added
         * to the freelists of the pool (for future reuse), or must be released right away.
         * Subclasses can override this to provide custom implementations
         * @param value the value to test for reusability
         * @return true if the value is reusable
         */
        protected virtual bool IsReusable(T value)
        {
            Preconditions.CheckNotNull(value);
            return true;
        }

        /**
         * Ensure pool size invariants.
         * The pool must either be below the soft-cap OR it must have no free values left
         */
        private void EnsurePoolSizeInvariant()
        {
            lock (_poolGate)
            {
                Preconditions.CheckState(!IsMaxSizeSoftCapExceeded() || FreeCounter.NumBytes == 0);
            }
        }

        /**
         * Initialize the list of buckets. Get the bucket sizes (and bucket lengths) from the bucket
         * sizes provider
         * @param inUseCounts map of current buckets and their in use counts
         */
        private void InitBuckets(Dictionary<int, int> inUseCounts)
        {
            lock (_poolGate)
            {
                Preconditions.CheckNotNull(inUseCounts);

                // Clear out all the buckets
                Buckets.Clear();

                // Create the new buckets
                Dictionary<int, int> bucketSizes = _poolParams.BucketSizes;
                if (bucketSizes != null)
                {
                    foreach (KeyValuePair<int, int> entry in bucketSizes)
                    {
                        int bucketSize = entry.Key;
                        int maxLength = entry.Value;
                        int bucketInUseCount = 0;
                        inUseCounts.TryGetValue(bucketSize, out bucketInUseCount);
                        Buckets.Add(bucketSize, 
                                new Bucket<T>(
                                GetSizeInBytes(bucketSize),
                                maxLength,
                                bucketInUseCount));
                    }

                    _allowNewBuckets = false;
                }
                else
                {
                    _allowNewBuckets = true;
                }
            }
        }

        /**
         * Gets rid of all free values in the pool
         * At the end of this method, mFreeSpace will be zero (reflecting that there are no more free
         * values in the pool). mUsedSpace will however not be reset, since that's a reflection of the
         * values that were allocated via the pool, but are in use elsewhere
         */
        internal void TrimToNothing()
        {
            List<Bucket<T>> bucketsToTrim = new List<Bucket<T>>(Buckets.Count);
            Dictionary<int, int> inUseCounts = new Dictionary<int, int>();

            lock(_poolGate)
            {
                foreach (var bucket in Buckets)
                {
                    if (bucket.Value.GetFreeListSize() > 0)
                    {
                        bucketsToTrim.Add(bucket.Value);
                    }

                    inUseCounts.Add(bucket.Key, bucket.Value.GetInUseCount());
                }

                // Reinitialize the buckets
                InitBuckets(inUseCounts);

                // Free up the stats
                FreeCounter.Reset();
                LogStats();
            }

            // the pool parameters 'may' have changed.
            OnParamsChanged();

            // Explicitly free all the values.
            // All the core data structures have now been reset. We no longer need to block other calls.
            // This is true even for a concurrent trim() call
            foreach (var bucket in bucketsToTrim)
            {
                while (true)
                {
                    // what happens if we run into an exception during the recycle. I'm going to ignore
                    // these exceptions for now, and let the GC handle the rest of the to-be-recycled-bitmaps
                    // in its usual fashion
                    T item = bucket.Pop();
                    if (item == null)
                    {
                        break;
                    }

                    Free(item);
                }
            }
        }

        /**
         * Trim the (free portion of the) pool so that the pool size is at or below the soft cap.
         * This will try to free up values in the free portion of the pool, until
         *   (a) the pool size is now below the soft cap configured OR
         *   (b) the free portion of the pool is empty
         */
        private void TrimToSoftCap()
        {
            lock (_poolGate)
            {
                if (IsMaxSizeSoftCapExceeded())
                {
                    TrimToSize(_poolParams.MaxSizeSoftCap);
                }
            }
        }

        /**
         * (Try to) trim the pool until its total space falls below the max size (soft cap). This will
         * get rid of values on the free list, until the free lists are empty, or we fall below the
         * max size; whichever comes first.
         * NOTE: It is NOT an error if we have eliminated all the free values, but the pool is still
         * above its max size (soft cap)
         * <p>
         * The approach we take is to go from the smallest sized bucket down to the largest sized
         * bucket. This may seem a bit counter-intuitive, but the rationale is that allocating
         * larger-sized values is more expensive than the smaller-sized ones, so we want to keep them
         * around for a while.
         * @param targetSize target size to trim to
         */
        internal void TrimToSize(int targetSize)
        {
            lock (_poolGate)
            {
                // find how much we need to free
                int bytesToFree = Math.Min(UsedCounter.NumBytes + FreeCounter.NumBytes - targetSize, FreeCounter.NumBytes);
                if (bytesToFree <= 0)
                {
                    return;
                }

                Debug.WriteLine($"trimToSize: TargetSize = { targetSize }; Initial Size = { UsedCounter.NumBytes + FreeCounter.NumBytes }; Bytes to free = { bytesToFree }");
                LogStats();

                // now walk through the buckets from the smallest to the largest. Keep freeing things
                // until we've gotten to what we want
                foreach (var bucket in Buckets)
                {
                    if (bytesToFree <= 0)
                    {
                        break;
                    }

                    while (bytesToFree > 0)
                    {
                        T value = bucket.Value.Pop();
                        if (value == null)
                        {
                            break;
                        }

                        Free(value);
                        bytesToFree -= bucket.Value.ItemSize;
                        FreeCounter.Decrement(bucket.Value.ItemSize);
                    }
                }

                // Dump stats at the end
                LogStats();
                Debug.WriteLine($"trimToSize: TargetSize = { targetSize }; Final Size = { UsedCounter.NumBytes + FreeCounter.NumBytes }");
            }
        }

        /**
         * Gets the freelist for the specified bucket. Create the freelist if there isn't one
         * @param bucketedSize the bucket size
         * @return the freelist for the bucket
         */
        internal Bucket<T> GetBucket(int bucketedSize)
        {
            lock (_poolGate)
            {
                // Get an existing bucket
                Bucket<T> bucket = default(Bucket<T>);
                Buckets.TryGetValue(bucketedSize, out bucket);
                if (bucket != null || !_allowNewBuckets)
                {
                    return bucket;
                }

                // Create a new bucket
                Debug.WriteLine($"Creating new bucket { bucketedSize }");
                Bucket<T> newBucket = NewBucket(bucketedSize);
                Buckets.Add(bucketedSize, newBucket);
                return newBucket;
            }
        }

        protected virtual Bucket<T> NewBucket(int bucketedSize)
        {
            return new Bucket<T>(
                /*itemSize*/GetSizeInBytes(bucketedSize),
                /*maxLength*/int.MaxValue,
                /*inUseLength*/0);
        }

        /**
         * Returns true if the pool size (sum of the used and the free portions) exceeds its 'max size'
         * soft cap as specified by the pool parameters.
         */
        internal bool IsMaxSizeSoftCapExceeded()
        {
            lock (_poolGate)
            {
                bool isMaxSizeSoftCapExceeded = (UsedCounter.NumBytes + FreeCounter.NumBytes) > _poolParams.MaxSizeSoftCap;
                if (isMaxSizeSoftCapExceeded)
                {
                    _poolStatsTracker.OnSoftCapReached();
                }

                return isMaxSizeSoftCapExceeded;
            }
        }

        /**
         * Can we allocate a value of size 'sizeInBytes' without exceeding the hard cap on the pool size?
         * If allocating this value will take the pool over the hard cap, we will first trim the pool down
         * to its soft cap, and then check again.
         * If the current used bytes + this new value will take us above the hard cap, then we return
         * false immediately - there is no point freeing up anything.
         * @param sizeInBytes the size (in bytes) of the value to allocate
         * @return true, if we can allocate this; false otherwise
         */
        internal bool CanAllocate(int sizeInBytes)
        {
            lock (_poolGate)
            {
                int hardCap = _poolParams.MaxSizeHardCap;

                // even with our best effort we cannot ensure hard cap limit.
                // Return immediately - no point in trimming any space
                if (sizeInBytes > hardCap - UsedCounter.NumBytes)
                {
                    _poolStatsTracker.OnHardCapReached();
                    return false;
                }

                // trim if we need to
                int softCap = _poolParams.MaxSizeSoftCap;
                if (sizeInBytes > softCap - (UsedCounter.NumBytes + FreeCounter.NumBytes))
                {
                    TrimToSize(softCap - sizeInBytes);
                }

                // check again to see if we're below the hard cap
                if (sizeInBytes > hardCap - (UsedCounter.NumBytes + FreeCounter.NumBytes))
                {
                    _poolStatsTracker.OnHardCapReached();
                    return false;
                }

                return true;
            }
        }

        /**
         * Export memory stats regarding buckets used, memory caps, reused values.
         */
        public Dictionary<string, int> GetStats()
        {
            lock (_poolGate)
            {
                Dictionary<string, int> stats = new Dictionary<string, int>();
                foreach (var bucket in Buckets)
                {
                    int bucketedSize = bucket.Key;
                    Bucket<T> bucketValue = bucket.Value;
                    string BUCKET_USED_KEY = PoolStatsTracker.BUCKETS_USED_PREFIX + GetSizeInBytes(bucketedSize);
                    stats.Add(BUCKET_USED_KEY, bucketValue.GetInUseCount());
                }

                stats.Add(PoolStatsTracker.SOFT_CAP, _poolParams.MaxSizeSoftCap);
                stats.Add(PoolStatsTracker.HARD_CAP, _poolParams.MaxSizeHardCap);
                stats.Add(PoolStatsTracker.USED_COUNT, UsedCounter.Count);
                stats.Add(PoolStatsTracker.USED_BYTES, UsedCounter.NumBytes);
                stats.Add(PoolStatsTracker.FREE_COUNT, FreeCounter.Count);
                stats.Add(PoolStatsTracker.FREE_BYTES, FreeCounter.NumBytes);

                return stats;
            }
        }

        /**
        * Simple 'debug' logging of stats.
        * WARNING: The caller is responsible for synchronization
        */
        private void LogStats()
        {
            Debug.WriteLine($"Used = ({ UsedCounter.Count }, { UsedCounter.NumBytes }); Free = ({ FreeCounter.Count }, { FreeCounter.NumBytes })");
        }
    }
}
