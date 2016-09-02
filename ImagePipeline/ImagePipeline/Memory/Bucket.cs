using System.Collections;
using FBCore.Common.Internal;

namespace ImagePipeline.Memory
{
    /**
     * The Bucket is a constituent class of {@link BasePool}. The pool maintains its free values
     * in a set of buckets, where each bucket represents a set of values of the same 'size'.
     * <p>
     * Each bucket maintains a freelist of values.
     * When the pool receives a {@link BasePool#get(Object)} request for a particular size, it finds the
     * appropriate bucket, and delegates the request to the bucket ({@link #get()}.
     * If the bucket's freelist is  non-empty, then one of the entries on the freelist is returned (and
     * removed from the freelist).
     * Similarly, when a value is released to the pool via a call to {@link BasePool#release(Object)},
     * the pool locates the appropriate bucket and returns the value to the bucket's freelist - see
     * ({@link #release(Object)}
     * <p>
     * The bucket also maintains the current number of items (from this bucket) that are "in use" i.e.
     * values that came from this bucket, but are now in use by the caller, and no longer on the
     * freelist.
     * The 'length' of the bucket is the number of values from this bucket that are currently in use
     * (mInUseCount), plus the size of the freeList. The maxLength of the bucket is that maximum length
     * that this bucket should grow to - and is used by the pool to determine whether values should
     * be released to the bucket ot freed.
     * @param <V> type of values to be 'stored' in the bucket
     */
    public class Bucket<T>
    {
        // 'max' length for this bucket
        private int MaxLength { get; }

        // The free list for this bucket, subclasses can vary type
        private Queue FreeList { get; }

        // Current number of entries 'in use' (i.e.) not in the free list
        private int _inUseLength;

        // Size in bytes of items in this bucket
        public int ItemSize { get; }

        /**
         * Constructs a new Bucket instance. The constructed bucket will have an empty freelist
         * @param itemSize size in bytes of each item in this bucket
         * @param maxLength max length for the bucket (used + free)
         * @param inUseLength current in-use-length for the bucket
         */
        public Bucket(int itemSize, int maxLength, int inUseLength)
        {
            Preconditions.CheckState(itemSize > 0);
            Preconditions.CheckState(maxLength >= 0);
            Preconditions.CheckState(inUseLength >= 0);

            ItemSize = itemSize;
            MaxLength = maxLength;
            FreeList = new Queue();
            _inUseLength = inUseLength;
        }

        /**
        * Determines if the current length of the bucket (free + used) exceeds the max length
        * specified
        */
        public bool IsMaxLengthExceeded()
        {
            return (_inUseLength + GetFreeListSize() > MaxLength);
        }

        public int GetFreeListSize()
        {
            return FreeList.Count;
        }

        /**
         * Gets a free item if possible from the freelist. Returns null if the free list is empty
         * Updates the bucket inUse count
         * @return an item from the free list, if available
         */
        public T Get()
        {
            T value = Pop();
            if (value != null)
            {
                _inUseLength++;
            }

            return value;
        }

        /**
         * Remove the first item (if any) from the freelist. Returns null if the free list is empty
         * Does not update the bucket inUse count
         * @return the first value (if any) from the free list
         */
        public T Pop()
        {
            return (FreeList.Count != 0) ? (T)FreeList.Dequeue() : default(T);
        }

        /**
         * Increment the mInUseCount field.
         * Used by the pool to update the bucket info when a value was 'alloc'ed (because no free value
         * was available)
         */
        public void IncrementInUseCount()
        {
            _inUseLength++;
        }

        /**
         * Releases a value to this bucket and decrements the inUse count
         * @param value the value to release
         */
        public void Release(T value)
        {
            Preconditions.CheckNotNull(value);
            Preconditions.CheckState(_inUseLength > 0);
            _inUseLength--;
            AddToFreeList(value);
        }

        private void AddToFreeList(T value)
        {
            FreeList.Enqueue(value);
        }

        /**
         * Decrement the mInUseCount field.
         * Used by the pool to update the bucket info when a value was freed, instead of being returned
         * to the bucket's free list
         */
        public void DecrementInUseCount()
        {
            Preconditions.CheckState(_inUseLength > 0);
            _inUseLength--;
        }

        public int GetInUseCount()
        {
            return _inUseLength;
        }
    }
}
