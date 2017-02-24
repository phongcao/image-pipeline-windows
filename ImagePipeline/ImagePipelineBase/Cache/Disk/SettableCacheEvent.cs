using Cache.Common;
using System.IO;

namespace Cache.Disk
{
    /// <summary>
    /// Implementation of <see cref="ICacheEvent"/> that allows the values to be set
    /// and supports recycling of instances.
    /// </summary>
    public class SettableCacheEvent : ICacheEvent
    {
        private static readonly object RECYCLER_LOCK = new object();
        private static readonly int MAX_RECYCLED = 5;

        private static SettableCacheEvent _firstRecycledEvent;
        private static int _recycledCount;

        private ICacheKey _cacheKey;
        private string _resourceId;
        private long _itemSize;
        private long _cacheLimit;
        private long _cacheSize;
        private IOException _exception;
        private EvictionReason _evictionReason;
        private SettableCacheEvent _nextRecycledEvent;

        /// <summary>
        /// Obtains the cache event instance.
        /// </summary>
        public static SettableCacheEvent Obtain()
        {
            lock (RECYCLER_LOCK)
            {
                if (_firstRecycledEvent != null)
                {
                    SettableCacheEvent eventToReuse = _firstRecycledEvent;
                    _firstRecycledEvent = eventToReuse._nextRecycledEvent;
                    eventToReuse._nextRecycledEvent = null;
                    _recycledCount--;
                    return eventToReuse;
                }
            }

            return new SettableCacheEvent();
        }

        private SettableCacheEvent()
        {
        }

        /// <summary>
        /// Gets the cache key related to this event.
        /// </summary>
        public ICacheKey CacheKey
        {
            get
            {
                return _cacheKey;
            }
        }

        /// <summary>
        /// Sets the cache key related to this event.
        /// </summary>
        public SettableCacheEvent SetCacheKey(ICacheKey cacheKey)
        {
            _cacheKey = cacheKey;
            return this;
        }

        /// <summary>
        /// Gets the resource ID for the cached item.
        /// </summary>
        public string ResourceId
        {
            get
            {
                return _resourceId;
            }
        }

        /// <summary>
        /// Sets the resource ID for the cached item.
        /// </summary>
        public SettableCacheEvent SetResourceId(string resourceId)
        {
            _resourceId = resourceId;
            return this;
        }

        /// <summary>
        /// Gets the size of the new resource in storage, in bytes.
        /// </summary>
        public long ItemSize
        {
            get
            {
                return _itemSize;
            }
        }

        /// <summary>
        /// Sets the size of the new resource in storage, in bytes.
        /// </summary>
        public SettableCacheEvent SetItemSize(long itemSize)
        {
            _itemSize = itemSize;
            return this;
        }

        /// <summary>
        /// Gets the total size of the resources currently in storage, in bytes.
        /// </summary>
        public long CacheSize
        {
            get
            {
                return _cacheSize;
            }
        }

        /// <summary>
        /// Sets the total size of the resources currently in storage, in bytes.
        /// </summary>
        public SettableCacheEvent SetCacheSize(long cacheSize)
        {
            _cacheSize = cacheSize;
            return this;
        }

        /// <summary>
        /// Gets the current size limit for the cache, in bytes.
        /// </summary>
        public long CacheLimit
        {
            get
            {
                return _cacheLimit;
            }
        }

        /// <summary>
        /// Sets the current size limit for the cache, in bytes.
        /// </summary>
        public SettableCacheEvent SetCacheLimit(long cacheLimit)
        {
            _cacheLimit = cacheLimit;
            return this;
        }


        /// <summary>
        /// Gets the exception which occurred to trigger a read or write
        /// exception event.
        /// </summary>
        public IOException Exception
        {
            get
            {
                return _exception;
            }
        }

        /// <summary>
        /// Sets the exception which occurred to trigger a read or write
        /// exception event.
        /// </summary>
        public SettableCacheEvent SetException(IOException exception)
        {
            _exception = exception;
            return this;
        }

        /// <summary>
        /// Gets the reason for an item's eviction in eviction events.
        /// </summary>
        public EvictionReason EvictionReason
        {
            get
            {
                return _evictionReason;
            }
        }

        /// <summary>
        /// Sets the reason for an item's eviction in eviction events.
        /// </summary>
        public SettableCacheEvent SetEvictionReason(EvictionReason evictionReason)
        {
            _evictionReason = evictionReason;
            return this;
        }

        /// <summary>
        /// Recycles the cache event.
        /// </summary>
        public void Recycle()
        {
            lock (RECYCLER_LOCK)
            {
                if (_recycledCount < MAX_RECYCLED)
                {
                    Reset();
                    _recycledCount++;

                    if (_firstRecycledEvent != null)
                    {
                        _nextRecycledEvent = _firstRecycledEvent;
                    }

                    _firstRecycledEvent = this;
                }
            }
        }

        private void Reset()
        {
            _cacheKey = null;
            _resourceId = null;
            _itemSize = 0;
            _cacheLimit = 0;
            _cacheSize = 0;
            _exception = null;
            _evictionReason = EvictionReason.NONE;
        }
    }
}
