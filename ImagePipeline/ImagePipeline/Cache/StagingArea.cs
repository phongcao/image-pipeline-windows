using Cache.Common;
using FBCore.Common.Internal;
using FBCore.Common.References;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using System.Collections.Generic;
using System.Diagnostics;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// This is class encapsulates Map that maps ImageCacheKeys to EncodedImages pointing to
    /// PooledByteBuffers. It is used by SimpleImageCache to store values that are being written
    /// to disk cache, so that they can be returned by parallel cache get operations.
    /// </summary>
    public class StagingArea
    {
        private static readonly object _instanceGate = new object();
        private static StagingArea _instance = null;

        private readonly object _mapGate = new object();
        private IDictionary<ICacheKey, EncodedImage> _map;

        private StagingArea()
        {
            _map = new Dictionary<ICacheKey, EncodedImage>();
        }

        /// <summary>
        /// Singleton
        /// </summary>
        /// <returns></returns>
        public static StagingArea Instance
        {
            get
            {
                lock (_instanceGate)
                {
                    if (_instance == null)
                    {
                        _instance = new StagingArea();
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Stores key-value in this StagingArea. This call overrides previous value
        /// of stored reference if
        /// <param name="key"></param>
        /// <param name="encodedImage">EncodedImage to be associated with key</param>
        /// </summary>
        public void Put(ICacheKey key, EncodedImage encodedImage)
        {
            lock (_mapGate)
            {
                Preconditions.CheckNotNull(key);
                Preconditions.CheckArgument(EncodedImage.IsValid(encodedImage));

                // We're making a 'copy' of this reference - so duplicate it
                EncodedImage oldEntry = default(EncodedImage);
                if (_map.TryGetValue(key, out oldEntry))
                {
                    _map.Remove(key);
                }

                _map.Add(key, EncodedImage.CloneOrNull(encodedImage));
                EncodedImage.CloseSafely(oldEntry);
                LogStats();
            }
        }

        /// <summary>
        /// Removes all items from the StagingArea.
        /// </summary>
        public void ClearAll()
        {
            IList<EncodedImage> old;
            lock (_mapGate)
            {
                old = new List<EncodedImage>(_map.Values);
                _map.Clear();
            }

            foreach (var encodedImage in old)
            {
                if (encodedImage != null)
                {
                    encodedImage.Dispose();
                }
            }
        }

        /// <summary>
        /// Removes item from the StagingArea.
        /// <param name="key"></param>
        /// @return true if item was removed
        /// </summary>
        public bool Remove(ICacheKey key)
        {
            Preconditions.CheckNotNull(key);
            EncodedImage encodedImage = default(EncodedImage);
            lock (_mapGate)
            {
                if (!_map.TryGetValue(key, out encodedImage))
                {
                    return false;
                }

                _map.Remove(key);
            }

            try
            {
                return encodedImage.Valid;
            }
            finally
            {
                encodedImage.Dispose();
            }
        }

        /// <summary>
        /// Removes key-value from the StagingArea. Both key and value must match.
        /// <param name="key"></param>
        /// <param name="encodedImage">value corresponding to key</param>
        /// @return true if item was removed
        /// </summary>
        public bool Remove(ICacheKey key, EncodedImage encodedImage)
        {
            lock (_mapGate)
            {
                Preconditions.CheckNotNull(key);
                Preconditions.CheckNotNull(encodedImage);
                Preconditions.CheckArgument(EncodedImage.IsValid(encodedImage));

                EncodedImage oldValue = default(EncodedImage);
                if (!_map.TryGetValue(key, out oldValue))
                {
                    return false;
                }

                CloseableReference<IPooledByteBuffer> oldReference = oldValue.GetByteBufferRef();
                CloseableReference<IPooledByteBuffer> reference = encodedImage.GetByteBufferRef();

                try
                {
                    if (oldReference == null || reference == null || oldReference.Get() != reference.Get())
                    {
                        return false;
                    }

                    _map.Remove(key);
                }
                finally
                {
                    CloseableReference<IPooledByteBuffer>.CloseSafely(reference);
                    CloseableReference<IPooledByteBuffer>.CloseSafely(oldReference);
                    EncodedImage.CloseSafely(oldValue);
                }

                LogStats();
                return true;
            }
        }

        /// <summary>
        /// <param name="key"></param>
        /// @return value associated with given key or null if no value is associated
        /// </summary>
        public EncodedImage Get(ICacheKey key)
        {
            lock (_mapGate)
            {
                Preconditions.CheckNotNull(key);
                EncodedImage storedEncodedImage = default(EncodedImage);
                if (_map.TryGetValue(key, out storedEncodedImage))
                {
                    if (!EncodedImage.IsValid(storedEncodedImage))
                    {
                        // Reference is not valid, this means that someone cleared reference while it was still in
                        // use. Log error
                        // TODO: 3697790
                        _map.Remove(key);
                        Debug.WriteLine($"Found closed reference { storedEncodedImage.GetHashCode() } for key { key.ToString() } ({ key.GetHashCode() })");
                        return null;
                    }

                    storedEncodedImage = EncodedImage.CloneOrNull(storedEncodedImage);
                }

                return storedEncodedImage;
            }
        }

        /// <summary>
        /// Determine if an valid entry for the key exists in the staging area.
        /// </summary>
        public bool ContainsKey(ICacheKey key)
        {
            lock (_mapGate)
            {
                Preconditions.CheckNotNull(key);
                EncodedImage storedEncodedImage = default(EncodedImage);
                if (!_map.TryGetValue(key, out storedEncodedImage))
                {
                    return false;
                }

                if (!EncodedImage.IsValid(storedEncodedImage))
                {
                    // Reference is not valid, this means that someone cleared reference while it was still in
                    // use. Log error
                    // TODO: 3697790
                    _map.Remove(key);
                    Debug.WriteLine($"Found closed reference { storedEncodedImage.GetHashCode() } for key { key.ToString() } ({ key.GetHashCode() })");
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Simple 'debug' logging of stats.
        /// </summary>
        private void LogStats()
        {
            lock (_mapGate)
            {
                Debug.WriteLine($"Count = { _map.Count }");
            }
        }
    }
}
