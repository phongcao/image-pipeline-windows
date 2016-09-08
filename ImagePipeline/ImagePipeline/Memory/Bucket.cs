using System.Collections;
using FBCore.Common.Internal;

namespace ImagePipeline.Memory
{
    /// <summary>
    /// The Bucket is a constituent class of <see cref="BasePool&lt;T&gt;"/>. The pool maintains its free values
    /// in a set of buckets, where each bucket represents a set of values of the same 'size'.
    /// <para />
    /// Each bucket maintains a freelist of values.
    /// When the pool receives a <see cref="BasePool&lt;T&gt;.Get(int)"/> request for a particular size, it finds the
    /// appropriate bucket, and delegates the request to the bucket (<see cref="Get()"/>.
    /// If the bucket's freelist is  non-empty, then one of the entries on the freelist is returned (and
    /// removed from the freelist).
    /// Similarly, when a value is released to the pool via a call to <see cref="BasePool&lt;T&gt;.Release(T)"/>,
    /// the pool locates the appropriate bucket and returns the value to the bucket's freelist - see
    /// (<see cref="Release(T)"/>
    /// <para />
    /// The bucket also maintains the current number of items (from this bucket) that are "in use" i.e.
    /// values that came from this bucket, but are now in use by the caller, and no longer on the
    /// freelist.
    /// The 'length' of the bucket is the number of values from this bucket that are currently in use
    /// (mInUseCount), plus the size of the freeList. The maxLength of the bucket is that maximum length
    /// that this bucket should grow to - and is used by the pool to determine whether values should
    /// be released to the bucket ot freed.
    /// &lt;T&gt; Type of values to be 'stored' in the bucket
    /// </summary>
    public class Bucket<T>
    {
        /// <summary>
        /// 'max' length for this bucket
        /// </summary>
        private readonly int _maxLength;

        /// <summary>
        /// The free list for this bucket, subclasses can vary type
        /// </summary>
        protected readonly Queue _freeList;

        /// <summary>
        /// Current number of entries 'in use' (i.e.) not in the free list
        /// </summary>
        private int _inUseLength;

        /// <summary>
        /// Size in bytes of items in this bucket
        /// </summary>
        public int ItemSize { get; }

        /// <summary>
        /// Constructs a new Bucket instance. The constructed bucket will have an empty freelist
        /// <param name="itemSize">Size in bytes of each item in this bucket</param>
        /// <param name="maxLength">Max length for the bucket (used + free)</param>
        /// <param name="inUseLength">Current in-use-length for the bucket</param>
        /// </summary>
        public Bucket(int itemSize, int maxLength, int inUseLength)
        {
            Preconditions.CheckState(itemSize > 0);
            Preconditions.CheckState(maxLength >= 0);
            Preconditions.CheckState(inUseLength >= 0);

            ItemSize = itemSize;
            _maxLength = maxLength;
            _freeList = new Queue();
            _inUseLength = inUseLength;
        }

        /// <summary>
       /// Determines if the current length of the bucket (free + used) exceeds the max length
       /// specified
       /// </summary>
        public bool IsMaxLengthExceeded()
        {
            return (_inUseLength + GetFreeListSize() > _maxLength);
        }

        /// <summary>
        /// Gets the number of items in the free list
        /// </summary>
        /// <returns>The number of items in the free list</returns>
        public int GetFreeListSize()
        {
            return _freeList.Count;
        }

        /// <summary>
        /// Gets a free item if possible from the freelist. Returns null if the free list is empty
        /// Updates the bucket inUse count
        /// @return an item from the free list, if available
        /// </summary>
        public T Get()
        {
            T value = Pop();
            if (value != null)
            {
                _inUseLength++;
            }

            return value;
        }

        /// <summary>
        /// Remove the first item (if any) from the freelist. Returns null if the free list is empty
        /// Does not update the bucket inUse count
        /// @return the first value (if any) from the free list
        /// </summary>
        public virtual T Pop()
        {
            return (_freeList.Count != 0) ? (T)_freeList.Dequeue() : default(T);
        }

        /// <summary>
        /// Increment the mInUseCount field.
        /// Used by the pool to update the bucket info when a value was 'alloc'ed (because no free value
        /// was available)
        /// </summary>
        public void IncrementInUseCount()
        {
            _inUseLength++;
        }

        /// <summary>
        /// Releases a value to this bucket and decrements the inUse count
        /// <param name="value">The value to release</param>
        /// </summary>
        public void Release(T value)
        {
            Preconditions.CheckNotNull(value);
            Preconditions.CheckState(_inUseLength > 0);
            _inUseLength--;
            AddToFreeList(value);
        }

        /// <summary>
        /// Add value to the free list size
        /// </summary>
        /// <param name="value">T</param>
        protected virtual void AddToFreeList(T value)
        {
            _freeList.Enqueue(value);
        }

        /// <summary>
        /// Decrement the mInUseCount field.
        /// Used by the pool to update the bucket info when a value was freed, instead of being returned
        /// to the bucket's free list
        /// </summary>
        public void DecrementInUseCount()
        {
            Preconditions.CheckState(_inUseLength > 0);
            _inUseLength--;
        }

        /// <summary>
        /// Get the current number of entries 'in use' (i.e.) not in the free list
        /// </summary>
        /// <returns>The current number of entries 'in use' (i.e.) not in the free list</returns>
        public int GetInUseCount()
        {
            return _inUseLength;
        }
    }
}
