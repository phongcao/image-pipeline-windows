using FBCore.Common.Internal;
using FBCore.Common.Memory;
using System;
using System.Collections.Generic;
#if DEBUG_MEMORY_POOL
using System.Diagnostics;
#endif // DEBUG_MEMORY_POOL

namespace ImagePipeline.Memory
{
    /// <summary>
    /// A base pool class that manages a pool of values (of type T).
    /// <para />
    /// The pool is organized as a map. Each entry in the map is a
    /// free-list (modeled by a queue) of entries for a given size.
    /// Some pools have a fixed set of buckets (aka bucketized sizes),
    /// while others don't.
    /// <para />
    /// The pool supports two main operations:
    /// <ul>
    ///   <li>
    ///   <see cref="Get(int)"/> - Returns a value of size that's
    ///   the same or larger than specified, hopefully from the pool;
    ///   otherwise, this value is allocated (via the alloc function)
    ///   </li>
    ///   <li>
    ///   <see cref="Release(T)"/> - Releases a value to the pool.
    ///   </li>
    /// </ul>
    /// In addition, the pool subscribes to the
    /// <see cref="IMemoryTrimmableRegistry"/>, and responds to
    /// low-memory events (calls to trim).
    /// Some percent (perhaps all) of the values in the pool are then
    /// released (via the underlying free function), and no longer
    /// belong to the pool.
    /// <para />
    /// Sizes
    /// There are 3 different notions of sizes we consider here
    /// (not all of them may be relevant for each use case).
    /// <ul>
    ///   <li>
    ///   Logical size is simply the size of the value in terms
    ///   appropriate for the value. For example, for byte arrays,
    ///   the size is simply the length. For a bitmap, the size 
    ///   is just the number of pixels.
    ///   </li>
    ///   <li>
    ///   Bucketed size typically represents one of discrete set
    ///   of logical sizes - such that each bucketed size can
    ///   accommodate a range of logical sizes. For example, for
    ///   byte arrays, using sizes that are powers of 2 for
    ///   bucketed sizes allows these byte arrays to support a
    ///   number of logical sizes.
    ///   </li>
    ///   <li>
    ///   Finally, Size-in-bytes is exactly that - the size of
    ///   the value in bytes.
    ///   </li>
    /// </ul>
    /// Logical Size and BucketedSize are both represented by the
    /// type parameter T, while size-in-bytes is represented by
    /// an int.
    /// <para />
    /// Each concrete subclass of the pool must implement the
    /// following methods:
    /// <ul>
    ///   <li>
    ///   <see cref="GetBucketedSize(int)"/> - returns the
    ///   bucketized size for the given request size.
    ///   </li>
    ///   <li>
    ///   <see cref="GetBucketedSizeForValue(T)"/> - returns
    ///   the bucketized size for a given value.
    ///   </li>
    ///   <li>
    ///   <see cref="GetSizeInBytes(int)"/> - gets the size
    ///   in bytes for a given bucketized size.
    ///   </li>
    ///   <li>
    ///   <see cref="Alloc(int)"/> - allocates a value of
    ///   given size.
    ///   </li>
    ///   <li>
    ///   <see cref="Free(T)"/> - frees the value T.
    ///   </li>
    /// Subclasses may optionally implement
    ///   <li>
    ///   <see cref="OnParamsChanged()"/> - called whenever
    ///   this class determines to re-read the pool params.
    ///   </li>
    ///   <li>
    ///   <see cref="IsReusable(T)"/> - used to determine
    ///   if a value can be reused or must be freed.
    ///   </li>
    /// </ul>
    /// <para />
    /// InUse values
    /// The pool keeps track of values currently in use
    /// (in addition to the free values in the buckets).
    /// This is maintained in an IdentityHashSet
    /// (using reference equality for the values).
    /// The in-use set helps with accounting/book-keeping;
    /// we also use this during <see cref="Release(T)"/>
    /// to avoid messing with (freeing/reusing) values that
    /// are 'unknown' to the pool.
    /// <para />
    /// PoolParams
    /// Pools are "configured" with a set of parameters
    /// (the PoolParams) supplied via a provider.
    /// This set of parameters includes
    /// <ul>
    ///   <li>
    ///   <see cref="PoolParams.MaxSizeSoftCap"/>
    ///   The size of a pool includes its used and free space.
    ///   The maxSize setting for a pool is a soft cap on the
    ///   overall size of the pool. A key point is that
    ///   <see cref="Get(int)"/> requests will not fail because
    ///   the max size has been exceeded (unless the underlying
    ///   <see cref="Alloc(int)"/> function fails). However, the
    ///   pool's free portion will be trimmed as much as possible
    ///   so that the pool's size may fall below the max size.
    ///   Note that when the free portion has fallen to zero,
    ///   the pool may still be larger than its maxSizeSoftCap.
    ///   On a <see cref="Release(T)"/> request, the value will
    ///   be 'freed' instead of being added to the free portion of
    ///   the pool, if the pool exceeds its maxSizeSoftCap.
    ///   The invariant we want to maintain - see 
    ///   <see cref="EnsurePoolSizeInvariant()"/> - is that the
    ///   pool must be below the max size soft cap OR the free
    ///   lists must be empty.
    ///   </li>
    ///   <li>
    ///   <see cref="PoolParams.MaxSizeHardCap"/>
    ///   The hard cap is a stronger limit on the pool size.
    ///   When this limit is reached, we first attempt to trim the
    ///   pool. If the pool size is still over the hard, the
    ///   <see cref="Get(int)"/> call will fail with a
    ///   <see cref="PoolSizeViolationException"/>.
    ///   </li>
    ///   <li>
    ///   <see cref="PoolParams.BucketSizes"/>
    ///   The pool can be configured with a set of 'sizes' - a bucket
    ///   is created for each such size. Additionally, each bucket
    ///   can have a a max-length specified, which is the sum of the
    ///   used and free items in that bucket. As with the MaxSize
    ///   parameter above, the maxLength here is a soft cap, in that
    ///   it will not cause an exception on get; it simply controls
    ///   the release path. If the BucketSizes parameter is null, then
    ///   the pool will dynamically create buckets on demand.
    ///   </li>
    /// </ul>
    /// </summary>
    public abstract class BasePool<T> : IPool<T>
    {
        private readonly object _poolGate = new object();

        /// <summary>
        /// Determines if new buckets can be created.
        /// </summary>
        private bool _allowNewBuckets;

        /// <summary>
        /// The memory manager to register with.
        /// </summary>
        protected internal readonly IMemoryTrimmableRegistry _memoryTrimmableRegistry;

        /// <summary>
       /// Provider for pool parameters.
       /// </summary>
        protected internal readonly PoolParams _poolParams;

        /// <summary>
        /// Keeps track of pool stats.
        /// </summary>
        protected internal readonly PoolStatsTracker _poolStatsTracker;

        /// <summary>
       /// The buckets - representing different 'sizes'.
       /// </summary>
        public Dictionary<int, Bucket<T>> Buckets { get; }

        /// <summary>
        /// An Identity hash-set to keep track of values by
        /// reference equality.
        /// </summary>
        public HashSet<T> InUseValues { get; }

        /// <summary>
        /// Tracks 'used space' - space allocated via the pool.
        /// </summary>
        internal Counter _usedCounter;

        /// <summary>
        /// Tracks 'free space' in the pool.
        /// </summary>
        internal Counter _freeCounter;

        /// <summary>
        /// Creates a new instance of the pool.
        /// </summary>
        /// <param name="memoryTrimmableRegistry">
        /// A class to be notified of system memory events.
        /// </param>
        /// <param name="poolParams">Pool parameters.</param>
        /// <param name="poolStatsTracker">
        /// Listener that logs pool statistics.
        /// </param>
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

            _freeCounter = new Counter();
            _usedCounter = new Counter();
        }

        /// <summary>
        /// Finish pool initialization.
        /// </summary>
        protected void Initialize()
        {
            _memoryTrimmableRegistry.RegisterMemoryTrimmable(this);
            _poolStatsTracker.SetBasePool(this);
        }

        /// <summary>
        /// Gets a new 'value' from the pool, if available.
        /// Allocates a new value if necessary.
        /// If we need to perform an allocation,
        ///   - If the pool size exceeds the max-size soft cap,
        ///   then we attempt to trim the free portion of the pool.
        ///   - If the pool size exceeds the max-size hard-cap
        ///   (after trimming), then we throw an
        ///   <see cref="PoolSizeViolationException"/>.
        /// Bucket length constraints are not considered in
        /// this function.
        /// </summary>
        /// <param name="size">
        /// The logical size to allocate.
        /// </param>
        /// <returns>A new value.</returns>
        /// <exception cref="InvalidSizeException">
        /// If the size of the value doesn't match the pool's
        /// constraints.
        /// </exception>
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
                        _usedCounter.Increment(sizeInBytes);
                        _freeCounter.Decrement(sizeInBytes);
                        _poolStatsTracker.OnValueReuse(sizeInBytes);
                        LogStats();
#if DEBUG_MEMORY_POOL
                        Debug.WriteLine($"get (reuse) (object, size) = ({ val.GetHashCode() }, { bucketedSize })");
#endif // DEBUG_MEMORY_POOL
                        return val;
                    }
                    // Fall through
                }

                // Check to see if we can allocate a value of the given size without 
                // exceeding the hard cap
                sizeInBytes = GetSizeInBytes(bucketedSize);
                if (!CanAllocate(sizeInBytes))
                {
                    throw new PoolSizeViolationException(
                        _poolParams.MaxSizeHardCap,
                        _usedCounter.NumBytes,
                        _freeCounter.NumBytes,
                        sizeInBytes);
                }

                // Optimistically assume that allocation succeeds - if it fails, we 
                // need to undo those changes
                _usedCounter.Increment(sizeInBytes);
                if (bucket != null)
                {
                    bucket.IncrementInUseCount();
                }
            }

            T value = default(T);
            try
            {
                // allocate the value outside the synchronized block, because
                // it can be pretty expensive we could have done the allocation
                // inside thesynchronized block, but that would have blocked out
                // other operations on the pool
                value = Alloc(bucketedSize);
            }
            catch (Exception)
            {
                // Assumption we made previously is not valid - allocation failed.
                // We need to fix internal counters.
                lock(_poolGate)
                {
                    _usedCounter.Decrement(sizeInBytes);
                    Bucket<T> bucket = GetBucket(bucketedSize);
                    if (bucket != null)
                    {
                        bucket.DecrementInUseCount();
                    }
                }

                throw;
            }

            // NOTE: We checked for hard caps earlier, and then did the alloc above.
            // Now we need to update state - but it is possible that a concurrent
            // thread did a similar operation - with the result being that we're now
            // over the hard cap. We are willing to live with that situation -
            // especially since the trim call below should be able to trim back
            // memory usage.
            lock (_poolGate)
            {
                Preconditions.CheckState(InUseValues.Add(value));

                // If we're over the pool's max size, try to trim the pool appropriately
                TrimToSoftCap();

                _poolStatsTracker.OnAlloc(sizeInBytes);
                LogStats();
#if DEBUG_MEMORY_POOL
                Debug.WriteLine($"get (alloc) (object, size) = ({ value.GetHashCode() }, { bucketedSize })");
#endif // DEBUG_MEMORY_POOL
            }

            return value;
        }

        /// <summary>
        /// Releases the given value to the pool.
        /// In a few cases, the value is 'freed' instead of
        /// being released to the pool. If
        ///   - The pool currently exceeds its max size OR
        ///   - If the value does not map to a bucket that's
        ///     currently maintained by the pool, OR
        ///   - If the bucket for the value exceeds its
        ///     maxLength, OR
        ///   - If the value is not recognized by the pool
        /// Then, the value is 'freed'.
        /// </summary>
        /// <param name="value">
        /// The value to release to the pool.
        /// </param>
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
#if DEBUG_MEMORY_POOL
                    Debug.WriteLine($"release (free, value unrecognized) (object, size) = ({ value.GetHashCode() }, { bucketedSize })");
#endif // DEBUG_MEMORY_POOL
                    Free(value);
                    _poolStatsTracker.OnFree(sizeInBytes);
                }
                else
                {
                    // free the value, if
                    //  - Pool exceeds maxSize
                    //  - There is no bucket for this value
                    //  - There is a bucket for this value, but it has exceeded its maxLength
                    //  - The value is not reusable
                    // If no bucket was found for the value, simply free it
                    // We should free the value if no bucket is found, or if the bucket length 
                    // cap is exceeded.
                    // However, if the pool max size softcap is exceeded, it may not always be 
                    // best to free *this* value.
                    if (bucket == null ||
                        bucket.IsMaxLengthExceeded() ||
                        IsMaxSizeSoftCapExceeded() ||
                        !IsReusable(value))
                    {
                        if (bucket != null)
                        {
                            bucket.DecrementInUseCount();
                        }

#if DEBUG_MEMORY_POOL
                        Debug.WriteLine($"release (free) (object, size) = ({ value.GetHashCode() }, { bucketedSize })");
#endif // DEBUG_MEMORY_POOL
                        Free(value);
                        _usedCounter.Decrement(sizeInBytes);
                        _poolStatsTracker.OnFree(sizeInBytes);
                    }
                    else
                    {
                        bucket.Release(value);
                        _freeCounter.Increment(sizeInBytes);
                        _usedCounter.Decrement(sizeInBytes);
                        _poolStatsTracker.OnValueRelease(sizeInBytes);
#if DEBUG_MEMORY_POOL
                        Debug.WriteLine($"release (reuse) (object, size) = ({ value.GetHashCode() }, { bucketedSize })");
#endif // DEBUG_MEMORY_POOL
                    }
                }

                LogStats();
            }
        }

        /// <summary>
        /// Trims the pool in response to low-memory states
        /// (invoked from MemoryManager).
        /// For now, we'll do the simplest thing, and simply
        /// clear out the entire pool. 
        /// We may consider more sophisticated approaches later.
        /// In other words, we ignore the memoryTrimType parameter.
        /// </summary>
        /// <param name="memoryTrimType">
        /// The kind of trimming we want to perform.
        /// </param>
        public void Trim(double memoryTrimType)
        {
            TrimToNothing();
        }

        /// <summary>
        /// Allocates a new 'value' with the given size.
        /// </summary>
        /// <param name="bucketedSize">
        /// The logical size to allocate.
        /// </param>
        /// <returns>A new value.</returns>
        protected internal abstract T Alloc(int bucketedSize);

        /// <summary>
        /// Frees the 'value'.
        /// </summary>
        /// <param name="value">The value to free.</param>
        protected internal abstract void Free(T value);

        /// <summary>
        /// Gets the bucketed size (typically something the same
        /// or larger than the requested size).
        /// </summary>
        /// <param name="requestSize">
        /// The logical request size.
        /// </param>
        /// <returns>The 'bucketed' size.</returns>
        /// <exception cref="InvalidSizeException">
        /// If the size of the value doesn't match the pool's
        /// constraints.
        /// </exception>
        protected internal abstract int GetBucketedSize(int requestSize);

        /// <summary>
        /// Gets the bucketed size of the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Bucketed size of the value.</returns>
        /// <exception cref="InvalidSizeException">
        /// If the size of the value doesn't match the pool's
        /// constraints.
        /// </exception>
        /// <exception cref="InvalidValueException">
        /// If the value is invalid.
        /// </exception>
        protected internal abstract int GetBucketedSizeForValue(T value);

        /// <summary>
        /// Gets the size in bytes for the given bucketed size.
        /// </summary>
        /// <param name="bucketedSize">The bucketed size.</param>
        /// <returns>Size in bytes.</returns>
        protected internal int GetSizeInBytes(int bucketedSize)
        {
            return bucketedSize;
        }

        /// <summary>
        /// The pool parameters may have changed. Subclasses can
        /// override this to update any state they were maintaining.
        /// </summary>
        protected void OnParamsChanged()
        {
        }

        /// <summary>
        /// Determines if the supplied value is 'reusable'.
        /// This is called during <see cref="Release(T)"/>, and
        /// determines if the value can be added to the freelists
        /// of the pool (for future reuse), or must be released 
        /// right away. Subclasses can override this to provide
        /// custom implementations.
        /// </summary>
        /// <param name="value">
        /// The value to test for reusability.
        /// </param>
        /// <returns>true if the value is reusable.</returns>
        protected internal virtual bool IsReusable(T value)
        {
            Preconditions.CheckNotNull(value);
            return true;
        }

        /// <summary>
        /// Ensure pool size invariants.
        /// The pool must either be below the soft-cap OR it
        /// must have no free values left.
        /// </summary>
        private void EnsurePoolSizeInvariant()
        {
            lock (_poolGate)
            {
                Preconditions.CheckState(!IsMaxSizeSoftCapExceeded() || _freeCounter.NumBytes == 0);
            }
        }

        /// <summary>
        /// Initialize the list of buckets. Get the bucket sizes
        /// (and bucket lengths) from the bucket sizes provider.
        /// </summary>
        /// <param name="inUseCounts">
        /// Map of current buckets and their in use counts.
        /// </param>
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

        /// <summary>
        /// Gets rid of all free values in the pool.
        /// At the end of this method, _freeCounter will be zero
        /// (reflecting that there are no more free values in the
        /// pool). _usedCounter will however not be reset, since 
        /// that's a reflection of the values that were allocated
        /// via the pool, but are in use elsewhere.
        /// </summary>
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
                _freeCounter.Reset();
                LogStats();
            }

            // The pool parameters 'may' have changed.
            OnParamsChanged();

            // Explicitly free all the values.
            // All the core data structures have now been reset. We no longer need to block 
            // other calls. This is true even for a concurrent Trim() call.
            foreach (var bucket in bucketsToTrim)
            {
                while (true)
                {
                    // what happens if we run into an exception during the recycle. I'm going 
                    // to ignore these exceptions for now, and let the GC handle the rest of 
                    // the to-be-recycled-bitmaps in its usual fashion.
                    T item = bucket.Pop();
                    if (item == null)
                    {
                        break;
                    }

                    Free(item);
                }
            }
        }

        /// <summary>
        /// Trim the (free portion of the) pool so that the pool
        /// size is at or below the soft cap.
        /// This will try to free up values in the free portion
        /// of the pool, until:
        ///   (a) The pool size is now below the soft cap
        ///   configured OR
        ///   (b) The free portion of the pool is empty
        /// </summary>
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

        /// <summary>
        /// (Try to) trim the pool until its total space falls
        /// below the max size (soft cap).
        /// This will get rid of values on the free list, until
        /// the free lists are empty, or we fall below the max
        /// size; whichever comes first.
        /// NOTE: It is NOT an error if we have eliminated all
        /// the free values, but the pool is still above its
        /// max size (soft cap).
        /// <para />
        /// The approach we take is to go from the smallest
        /// sized bucket down to the largest sized bucket.
        /// This may seem a bit counter-intuitive, but the
        /// rationale is that allocating larger-sized values
        /// is more expensive than the smaller-sized ones, so 
        /// we want to keep them around for a while.
        /// </summary>
        /// <param name="targetSize">
        /// Target size to trim to.
        /// </param>
        internal void TrimToSize(int targetSize)
        {
            lock (_poolGate)
            {
                // Find how much we need to free
                int bytesToFree = Math.Min(
                    _usedCounter.NumBytes + _freeCounter.NumBytes - targetSize, _freeCounter.NumBytes);

                if (bytesToFree <= 0)
                {
                    return;
                }

#if DEBUG_MEMORY_POOL
                Debug.WriteLine($"trimToSize: TargetSize = { targetSize }; Initial Size = { _usedCounter.NumBytes + _freeCounter.NumBytes }; Bytes to free = { bytesToFree }");
#endif // DEBUG_MEMORY_POOL
                LogStats();

                // Now walk through the buckets from the smallest to the largest. 
                // Keep freeing things until we've gotten to what we want
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
                        _freeCounter.Decrement(bucket.Value.ItemSize);
                    }
                }

                // Dump stats at the end
                LogStats();
#if DEBUG_MEMORY_POOL
                Debug.WriteLine($"trimToSize: TargetSize = { targetSize }; Final Size = { _usedCounter.NumBytes + _freeCounter.NumBytes }");
#endif // DEBUG_MEMORY_POOL
            }
        }

        /// <summary>
        /// Gets the freelist for the specified bucket.
        /// Create the freelist if there isn't one.
        /// </summary>
        /// <param name="bucketedSize">The bucket size.</param>
        /// <returns>The freelist for the bucket.</returns>
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
#if DEBUG_MEMORY_POOL
                Debug.WriteLine($"Creating new bucket { bucketedSize }");
#endif // DEBUG_MEMORY_POOL
                Bucket<T> newBucket = NewBucket(bucketedSize);
                Buckets.Add(bucketedSize, newBucket);
                return newBucket;
            }
        }

        /// <summary>
        /// Instantiates the <see cref="Bucket{T}"/>.
        /// </summary>
        /// <param name="bucketedSize">Bucket size.</param>
        /// <returns>Bucket.</returns>
        protected virtual Bucket<T> NewBucket(int bucketedSize)
        {
            return new Bucket<T>(
                /*itemSize*/GetSizeInBytes(bucketedSize),
                /*maxLength*/int.MaxValue,
                /*inUseLength*/0);
        }

        /// <summary>
        /// Returns true if the pool size (sum of the used and
        /// the free portions) exceeds its 'max size' soft cap
        /// as specified by the pool parameters.
        /// </summary>
        internal bool IsMaxSizeSoftCapExceeded()
        {
            lock (_poolGate)
            {
                bool isMaxSizeSoftCapExceeded = 
                    (_usedCounter.NumBytes + _freeCounter.NumBytes) > _poolParams.MaxSizeSoftCap;

                if (isMaxSizeSoftCapExceeded)
                {
                    _poolStatsTracker.OnSoftCapReached();
                }

                return isMaxSizeSoftCapExceeded;
            }
        }

        /// <summary>
        /// Can we allocate a value of size 'sizeInBytes' without
        /// exceeding the hard cap on the pool size? If allocating
        /// this value will take the pool over the hard cap, we 
        /// will first trim the pool down to its soft cap, and then
        /// check again.
        /// If the current used bytes + this new value will take us
        /// above the hard cap, then we return false immediately - 
        /// there is no point freeing up anything.
        /// </summary>
        /// <param name="sizeInBytes">
        /// The size (in bytes) of the value to allocate.
        /// </param>
        /// <returns>
        /// true, if we can allocate this; false otherwise.
        /// </returns>
        internal bool CanAllocate(int sizeInBytes)
        {
            lock (_poolGate)
            {
                int hardCap = _poolParams.MaxSizeHardCap;

                // Even with our best effort we cannot ensure hard cap limit.
                // Return immediately - no point in trimming any space
                if (sizeInBytes > hardCap - _usedCounter.NumBytes)
                {
                    _poolStatsTracker.OnHardCapReached();
                    return false;
                }

                // Trim if we need to
                int softCap = _poolParams.MaxSizeSoftCap;
                if (sizeInBytes > softCap - (_usedCounter.NumBytes + _freeCounter.NumBytes))
                {
                    TrimToSize(softCap - sizeInBytes);
                }

                // Check again to see if we're below the hard cap
                if (sizeInBytes > hardCap - (_usedCounter.NumBytes + _freeCounter.NumBytes))
                {
                    _poolStatsTracker.OnHardCapReached();
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Export memory stats regarding buckets used, memory caps,
        /// reused values.
        /// </summary>
        public Dictionary<string, int> GetStats()
        {
            lock (_poolGate)
            {
                Dictionary<string, int> stats = new Dictionary<string, int>();
                foreach (var bucket in Buckets)
                {
                    int bucketedSize = bucket.Key;
                    Bucket<T> bucketValue = bucket.Value;
                    string BUCKET_USED_KEY = 
                        PoolStatsTracker.BUCKETS_USED_PREFIX + GetSizeInBytes(bucketedSize);

                    stats.Add(BUCKET_USED_KEY, bucketValue.GetInUseCount());
                }

                stats.Add(PoolStatsTracker.SOFT_CAP, _poolParams.MaxSizeSoftCap);
                stats.Add(PoolStatsTracker.HARD_CAP, _poolParams.MaxSizeHardCap);
                stats.Add(PoolStatsTracker.USED_COUNT, _usedCounter.Count);
                stats.Add(PoolStatsTracker.USED_BYTES, _usedCounter.NumBytes);
                stats.Add(PoolStatsTracker.FREE_COUNT, _freeCounter.Count);
                stats.Add(PoolStatsTracker.FREE_BYTES, _freeCounter.NumBytes);

                return stats;
            }
        }

        /// <summary>
       /// Simple 'debug' logging of stats.
       /// WARNING: The caller is responsible for synchronization.
       /// </summary>
        private void LogStats()
        {
#if DEBUG_MEMORY_POOL
            Debug.WriteLine($"Used = ({ _usedCounter.Count }, { _usedCounter.NumBytes }); Free = ({ _freeCounter.Count }, { _freeCounter.NumBytes })");
#endif // DEBUG_MEMORY_POOL
        }
    }
}
