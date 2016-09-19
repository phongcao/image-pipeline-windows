using FBCore.Common.Internal;
using FBCore.Common.Memory;
using FBCore.Common.References;
using ImagePipeline.Bitmaps;
using ImagePipeline.Memory;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// Layer of memory cache stack responsible for managing eviction of the the cached items.
    ///
    /// <para /> This layer is responsible for LRU eviction strategy and for maintaining the size boundaries
    /// of the cached items.
    ///
    /// <para /> Only the exclusively owned elements, i.e. the elements not referenced by any client, can be
    /// evicted.
    /// </summary>
    public class CountingMemoryCache<K, V> : IMemoryCache<K, V>, IMemoryTrimmable
    {
        /// <summary>
        /// The internal representation of a key-value pair stored by the cache.
        /// </summary>
        internal class Entry
        {
            public K Key { get; }

            public CloseableReference<V> ValueRef { get; }

            /// <summary>
            /// The number of clients that reference the value
            /// </summary>
            public int ClientCount { get; set; }

            /// <summary>
            /// Whether or not this entry is tracked by this cache. Orphans are not tracked by the cache and
            /// as soon as the last client of an orphaned entry closes their reference, the entry's copy is
            /// closed too.
            /// </summary>
            public bool IsOrphan { get; set; }

            public IEntryStateObserver<K> Observer { get; }

            private Entry(K key, CloseableReference<V> valueRef, IEntryStateObserver<K> observer)
            {
                Key = Preconditions.CheckNotNull(key);
                ValueRef = Preconditions.CheckNotNull(CloseableReference<V>.CloneOrNull(valueRef));
                ClientCount = 0;
                IsOrphan = false;
                Observer = observer;
            }

            /// <summary>
            /// Creates a new entry with the usage count of 0.
            /// </summary>
            internal static Entry of(
                K key,
                CloseableReference<V> valueRef,
                IEntryStateObserver<K> observer)
            {
                return new Entry(key, valueRef, observer);
            }
        }

        /// <summary>
        /// How often the cache checks for a new cache configuration.
        /// </summary>
        internal readonly long PARAMS_INTERCHECK_INTERVAL_MS = 5 * 60 * 1000; // 5 minutes

        /// <summary>
        /// Contains the items that are not being used by any client and are hence viable for eviction.
        /// </summary>
        internal readonly CountingLruMap<K, Entry> _exclusiveEntries;

        /// <summary>
        /// Contains all the cached items including the exclusively owned ones.
        /// </summary>
        internal readonly CountingLruMap<K, Entry> _cachedEntries;

        internal readonly ConditionalWeakTable<SoftwareBitmap, object> _otherEntries = 
            new ConditionalWeakTable<SoftwareBitmap, object>();

        private readonly IValueDescriptor<V> _valueDescriptor;

        private readonly ICacheTrimStrategy _cacheTrimStrategy;

        /// <summary>
        /// Cache size constraints.
        /// </summary>
        private readonly ISupplier<MemoryCacheParams> _memoryCacheParamsSupplier;

        /// <summary>
        /// Memory cache params
        /// </summary>
        protected MemoryCacheParams _memoryCacheParams;

        private long _lastCacheParamsCheck;

        private readonly object _cacheGate = new object();

        /// <summary>
        /// Instantiates the <see cref="CountingMemoryCache&lt;K, V&gt;"/>.
        /// </summary>
        /// <param name="valueDescriptor"></param>
        /// <param name="cacheTrimStrategy"></param>
        /// <param name="memoryCacheParamsSupplier"></param>
        /// <param name="platformBitmapFactory"></param>
        /// <param name="isExternalCreatedBitmapLogEnabled"></param>
        public CountingMemoryCache(
            IValueDescriptor<V> valueDescriptor,
            ICacheTrimStrategy cacheTrimStrategy,
            ISupplier<MemoryCacheParams> memoryCacheParamsSupplier,
            PlatformBitmapFactory platformBitmapFactory,
            bool isExternalCreatedBitmapLogEnabled)
        {
            _valueDescriptor = valueDescriptor;
            _exclusiveEntries = new CountingLruMap<K, Entry>(WrapValueDescriptor(valueDescriptor));
            _cachedEntries = new CountingLruMap<K, Entry>(WrapValueDescriptor(valueDescriptor));
            _cacheTrimStrategy = cacheTrimStrategy;
            _memoryCacheParamsSupplier = memoryCacheParamsSupplier;
            _memoryCacheParams = _memoryCacheParamsSupplier.Get();
            _lastCacheParamsCheck = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            if (isExternalCreatedBitmapLogEnabled)
            {
                platformBitmapFactory.SetCreationListener(
                    new BitmapCreationObserver((b, o) => _otherEntries.Add(b, o)));
            }
        }

        private IValueDescriptor<Entry> WrapValueDescriptor(IValueDescriptor<V> evictableValueDescriptor)
        {
            return new ValueDescriptor<Entry>(
                entry =>
                {
                    return entry.ValueRef.IsValid() ? evictableValueDescriptor.GetSizeInBytes(entry.ValueRef.Get()) : 0;
                });
        }

        /// <summary>
        /// Caches the given key-value pair.
        ///
        /// <para /> Important: the client should use the returned reference instead of the original one.
        /// It is the caller's responsibility to close the returned reference once not needed anymore.
        ///
        /// @return the new reference to be used, null if the value cannot be cached
        /// </summary>
        public CloseableReference<V> Cache(K key, CloseableReference<V> valueRef)
        {
            return Cache(key, valueRef, null);
        }

        /// <summary>
        /// Caches the given key-value pair.
        ///
        /// <para /> Important: the client should use the returned reference instead of the original one.
        /// It is the caller's responsibility to close the returned reference once not needed anymore.
        ///
        /// @return the new reference to be used, null if the value cannot be cached
        /// </summary>
        public CloseableReference<V> Cache(
            K key,
            CloseableReference<V> valueRef,
            IEntryStateObserver<K> observer)
        {
            Preconditions.CheckNotNull(key);
            Preconditions.CheckNotNull(valueRef);

            MaybeUpdateCacheParams();

            Entry oldExclusive;
            CloseableReference<V> oldRefToClose = null;
            CloseableReference<V> clientRef = null;
            lock (_cacheGate)
            {
                // Remove the old item (if any) as it is stale now
                oldExclusive = _exclusiveEntries.Remove(key);
                Entry oldEntry = _cachedEntries.Remove(key);
                if (oldEntry != null)
                {
                    MakeOrphan(oldEntry);
                    oldRefToClose = ReferenceToClose(oldEntry);
                }

                if (CanCacheNewValue(valueRef.Get()))
                {
                    Entry newEntry = Entry.of(key, valueRef, observer);
                    _cachedEntries.Put(key, newEntry);
                    clientRef = NewClientReference(newEntry);
                }
            }

            CloseableReference<V>.CloseSafely(oldRefToClose);
            MaybeNotifyExclusiveEntryRemoval(oldExclusive);

            MaybeEvictEntries();
            return clientRef;
        }

        /// <summary>
        /// Checks the cache constraints to determine whether the new value can be cached or not.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool CanCacheNewValue(V value)
        {
            lock (_cacheGate)
            {
                int newValueSize = _valueDescriptor.GetSizeInBytes(value);
                return (newValueSize <= _memoryCacheParams.MaxCacheEntrySize) &&
                    (InUseCount <= _memoryCacheParams.MaxCacheEntries - 1) &&
                    (InUseSizeInBytes <= _memoryCacheParams.MaxCacheSize - newValueSize);
            }
        }

        /// <summary>
        /// Gets the item with the given key, or null if there is no such item.
        ///
        /// <para /> It is the caller's responsibility to close the returned reference once not needed anymore.
        /// </summary>
        public CloseableReference<V> Get(K key)
        {
            Preconditions.CheckNotNull(key);
            Entry oldExclusive;
            CloseableReference<V> clientRef = null;
            lock (_cacheGate)
            {
                oldExclusive = _exclusiveEntries.Remove(key);
                Entry entry = _cachedEntries.Get(key);
                if (entry != null)
                {
                    clientRef = NewClientReference(entry);
                }
            }

            MaybeNotifyExclusiveEntryRemoval(oldExclusive);
            MaybeUpdateCacheParams();
            MaybeEvictEntries();
            return clientRef;
        }

        /// <summary>
        /// Creates a new reference for the client.
        /// </summary>
        private CloseableReference<V> NewClientReference(Entry entry)
        {
            IncreaseClientCount(entry);
            return CloseableReference<V>.of(
                entry.ValueRef.Get(),
                new ResourceReleaser<V>(v => ReleaseClientReference(entry)));
        }

        /// <summary>
        /// Called when the client closes its reference.
        /// </summary>
        private void ReleaseClientReference(Entry entry)
        {
            Preconditions.CheckNotNull(entry);
            bool isExclusiveAdded;
            CloseableReference<V> oldRefToClose;
            lock (_cacheGate)
            {
                DecreaseClientCount(entry);
                isExclusiveAdded = MaybeAddToExclusives(entry);
                oldRefToClose = ReferenceToClose(entry);
            }

            CloseableReference<V>.CloseSafely(oldRefToClose);
            MaybeNotifyExclusiveEntryInsertion(isExclusiveAdded ? entry : null);
            MaybeUpdateCacheParams();
            MaybeEvictEntries();
        }

        /// <summary>
        /// Adds the entry to the exclusively owned queue if it is viable for eviction.
        /// </summary>
        private bool MaybeAddToExclusives(Entry entry)
        {
            lock (_cacheGate)
            {
                if (!entry.IsOrphan && entry.ClientCount == 0 && entry.ValueRef.IsValid())
                {
                    _exclusiveEntries.Put(entry.Key, entry);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the value with the given key to be reused, or null if there is no such value.
        ///
        /// <para /> The item can be reused only if it is exclusively owned by the cache.
        /// </summary>
        public CloseableReference<V> Reuse(K key)
        {
            Preconditions.CheckNotNull(key);
            CloseableReference<V> clientRef = null;
            bool removed = false;
            Entry oldExclusive = null;
            lock (_cacheGate)
            {
                oldExclusive = _exclusiveEntries.Remove(key);
                if (oldExclusive != null)
                {
                    Entry entry = _cachedEntries.Remove(key);
                    Preconditions.CheckNotNull(entry);
                    Preconditions.CheckState(entry.ClientCount == 0);
                    // optimization: instead of cloning and then closing the original reference,
                    // we just do a move
                    clientRef = entry.ValueRef;
                    removed = true;
                }
            }

            if (removed)
            {
                MaybeNotifyExclusiveEntryRemoval(oldExclusive);
            }

            return clientRef;
        }

        /// <summary>
        /// Removes all the items from the cache whose key matches the specified predicate.
        ///
        /// <param name="predicate">returns true if an item with the given key should be removed</param>
        /// @return number of the items removed from the cache
        /// </summary>
        public int RemoveAll(Predicate<K> predicate)
        {
            IList<Entry> oldExclusives;
            IList<Entry> oldEntries;
            lock (_cacheGate)
            {
                oldExclusives = _exclusiveEntries.RemoveAll(predicate);
                oldEntries = _cachedEntries.RemoveAll(predicate);
                MakeOrphans(oldEntries);
            }

            MaybeClose(oldEntries);
            MaybeNotifyExclusiveEntryRemoval(oldExclusives);
            MaybeUpdateCacheParams();
            MaybeEvictEntries();
            return oldEntries.Count;
        }

        /// <summary>
        /// Removes all the items from the cache.
        /// </summary>
        public void Clear()
        {
            IList<Entry> oldExclusives;
            IList<Entry> oldEntries;
            lock (_cacheGate)
            {
                oldExclusives = _exclusiveEntries.Clear();
                oldEntries = _cachedEntries.Clear();
                MakeOrphans(oldEntries);
            }

            MaybeClose(oldEntries);
            MaybeNotifyExclusiveEntryRemoval(oldExclusives);
            MaybeUpdateCacheParams();
        }

        /// <summary>
        /// Check if any items from the cache whose key matches the specified predicate.
        ///
        /// <param name="predicate">returns true if an item with the given key matches</param>
        /// @return true is any items matches from the cache
        /// </summary>
        public bool Contains(Predicate<K> predicate)
        {
            lock (_cacheGate)
            {
                return !(_cachedEntries.GetMatchingEntries(predicate).Count == 0);
            }
        }

        /// <summary>
        /// Trims the cache according to the specified trimming strategy and the given trim type.
        /// </summary>
        /// <param name="trimType"></param>
        public void Trim(double trimType)
        {
            IList<Entry> oldEntries;
            double trimRatio = _cacheTrimStrategy.GetTrimRatio(trimType);
            lock (_cacheGate)
            {
                int targetCacheSize = (int)(_cachedEntries.SizeInBytes * (1 - trimRatio));
                int targetEvictionQueueSize = Math.Max(0, targetCacheSize - InUseSizeInBytes);
                oldEntries = TrimExclusivelyOwnedEntries(int.MaxValue, targetEvictionQueueSize);
                MakeOrphans(oldEntries);
            }

            MaybeClose(oldEntries);
            MaybeNotifyExclusiveEntryRemoval(oldEntries);
            MaybeUpdateCacheParams();
            MaybeEvictEntries();
        }

        /// <summary>
        /// Updates the cache params (constraints) if enough time has passed since the last update.
        /// </summary>
        private void MaybeUpdateCacheParams()
        {
            lock (_cacheGate)
            {
                long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                if (_lastCacheParamsCheck + PARAMS_INTERCHECK_INTERVAL_MS > currentTime)
                {
                    return;
                }

                _lastCacheParamsCheck = currentTime;
                _memoryCacheParams = _memoryCacheParamsSupplier.Get();
            }
        }

        /// <summary>
        /// Force updating the cache params (
        /// </summary>
        internal void ForceUpdateCacheParams(ISupplier<MemoryCacheParams> cacheParamsSupplier)
        {
            lock (_cacheGate)
            {
                long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                _lastCacheParamsCheck = currentTime;
                _memoryCacheParams = cacheParamsSupplier.Get();
            }
        }

        /// <summary>
        /// Removes the exclusively owned items until the cache constraints are met.
        ///
        /// <para /> This method invokes the external <see cref="CloseableReference&lt;V&gt;.Dispose"/> method,
        /// so it must not be called while holding the <code>this</code> lock.
        /// </summary>
        private void MaybeEvictEntries()
        {
            IList<Entry> oldEntries;
            lock (_cacheGate)
            {
                int maxCount = Math.Min(
                    _memoryCacheParams.MaxEvictionQueueEntries,
                    _memoryCacheParams.MaxCacheEntries - InUseCount);
                int maxSize = Math.Min(
                    _memoryCacheParams.MaxEvictionQueueSize,
                    _memoryCacheParams.MaxCacheSize - InUseSizeInBytes);
                oldEntries = TrimExclusivelyOwnedEntries(maxCount, maxSize);
                MakeOrphans(oldEntries);
            }

            MaybeClose(oldEntries);
            MaybeNotifyExclusiveEntryRemoval(oldEntries);
        }

        /// <summary>
        /// Removes the exclusively owned items until there is at most <code>count</code> of them
        /// and they occupy no more than <code>size</code> bytes.
        ///
        /// <para /> This method returns the removed items instead of actually closing them, so it is safe to
        /// be called while holding the <code>this</code> lock.
        /// </summary>
        private IList<Entry> TrimExclusivelyOwnedEntries(int count, int size)
        {
            lock (_cacheGate)
            {
                count = Math.Max(count, 0);
                size = Math.Max(size, 0);
                // fast path without array allocation if no eviction is necessary
                if (_exclusiveEntries.Count <= count && _exclusiveEntries.SizeInBytes <= size)
                {
                    return null;
                }

                IList<Entry> oldEntries = new List<Entry>();
                while (_exclusiveEntries.Count > count || _exclusiveEntries.SizeInBytes > size)
                {
                    K key = _exclusiveEntries.FirstKey;
                    _exclusiveEntries.Remove(key);
                    oldEntries.Add(_cachedEntries.Remove(key));
                }

                return oldEntries;
            }
        }

        /// <summary>
        /// Notifies the client that the cache no longer tracks the given items.
        ///
        /// <para /> This method invokes the external <see cref="CloseableReference&lt;V&gt;.Dispose"/> method,
        /// so it must not be called while holding the <code>this</code> lock.
        /// </summary>
        private void MaybeClose(IList<Entry> oldEntries)
        {
            if (oldEntries != null)
            {
                foreach (Entry oldEntry in oldEntries)
                {
                    CloseableReference<V>.CloseSafely(ReferenceToClose(oldEntry));
                }
            }
        }

        private void MaybeNotifyExclusiveEntryRemoval(IList<Entry> entries)
        {
            if (entries != null)
            {
                foreach (Entry entry in entries)
                {
                    MaybeNotifyExclusiveEntryRemoval(entry);
                }
            }
        }

        private static void MaybeNotifyExclusiveEntryRemoval(Entry entry)
        {
            if (entry != null && entry.Observer != null)
            {
                entry.Observer.OnExclusivityChanged(entry.Key, false);
            }
        }

        private static void MaybeNotifyExclusiveEntryInsertion(Entry entry)
        {
            if (entry != null && entry.Observer != null)
            {
                entry.Observer.OnExclusivityChanged(entry.Key, true);
            }
        }

        /// <summary>
        /// Marks the given entries as orphans.
        /// </summary>
        private void MakeOrphans(IList<Entry> oldEntries)
        {
            lock (_cacheGate)
            {
                if (oldEntries != null)
                {
                    foreach (Entry oldEntry in oldEntries)
                    {
                        MakeOrphan(oldEntry);
                    }
                }
            }
        }

        /// <summary>
        /// Marks the entry as orphan.
        /// </summary>
        private void MakeOrphan(Entry entry)
        {
            lock (_cacheGate)
            {
                Preconditions.CheckNotNull(entry);
                Preconditions.CheckState(!entry.IsOrphan);
                entry.IsOrphan = true;
            }
        }

        /// <summary>
        /// Increases the entry's client count.
        /// </summary>
        private void IncreaseClientCount(Entry entry)
        {
            lock (_cacheGate)
            {
                Preconditions.CheckNotNull(entry);
                Preconditions.CheckState(!entry.IsOrphan);
                entry.ClientCount++;
            }
        }

        /// <summary>
        /// Decreases the entry's client count.
        /// </summary>
        private void DecreaseClientCount(Entry entry)
        {
            lock (_cacheGate)
            {
                Preconditions.CheckNotNull(entry);
                Preconditions.CheckState(entry.ClientCount > 0);
                entry.ClientCount--;
            }
        }

        /// <summary>
        /// Returns the value reference of the entry if it should be closed, null otherwise.
        /// </summary>
        private CloseableReference<V> ReferenceToClose(Entry entry)
        {
            lock (_cacheGate)
            {
                Preconditions.CheckNotNull(entry);
                return (entry.IsOrphan && entry.ClientCount == 0) ? entry.ValueRef : null;
            }
        }

        /// <summary>
        /// Gets the total number of all currently cached items.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_cacheGate)
                {
                    return _cachedEntries.Count;
                }
            }
        }

        /// <summary>
        /// Gets the total size in bytes of all currently cached items.
        /// </summary>
        public int SizeInBytes
        {
            get
            {
                lock (_cacheGate)
                {
                    return _cachedEntries.SizeInBytes;
                }
            }
        }

        /// <summary>
        /// Gets the number of the cached items that are used by at least one client.
        /// </summary>
        public int InUseCount
        {
            get
            {
                lock (_cacheGate)
                {
                    return _cachedEntries.Count - _exclusiveEntries.Count;
                }
            }
        }

        /// <summary>
        /// Gets the total size in bytes of the cached items that are used by at least one client.
        /// </summary>
        public int InUseSizeInBytes
        {
            get
            {
                lock (_cacheGate)
                {
                    return _cachedEntries.SizeInBytes - _exclusiveEntries.SizeInBytes;
                }
            }
        }

        /// <summary>
        /// Gets the number of the exclusively owned items.
        /// </summary>
        public int EvictionQueueCount
        {
            get
            {
                lock (_cacheGate)
                {
                    return _exclusiveEntries.Count;
                }
            }
        }

        /// <summary>
        /// Gets the total size in bytes of the exclusively owned items.
        /// </summary>
        public int EvictionQueueSizeInBytes
        {
            get
            {
                lock (_cacheGate)
                {
                    return _exclusiveEntries.SizeInBytes;
                }
            }
        }
    }
}
