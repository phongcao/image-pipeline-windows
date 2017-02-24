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
    /// This is class encapsulates Map that maps ImageCacheKeys to EncodedImages
    /// pointing to IPooledByteBuffer.
    /// It is used by SimpleImageCache to store values that are being written
    /// to disk cache, so that they can be returned by parallel cache get
    /// operations.
    /// </summary>
    public class StagingArea
    {
        private static readonly object _instanceGate = new object();
        private static StagingArea _instance = null;

        private readonly object _mapGate = new object();
        private IDictionary<ICacheKey, EncodedImage> _map;

        /// <summary>
        /// Test-only variables.
        ///
        /// <para /><b>DO NOT USE in application code.</b>
        /// </summary>
        internal int _removeCallsTestOnly;
        internal int _clearAllCallsTestOnly;

        private StagingArea()
        {
            _map = new Dictionary<ICacheKey, EncodedImage>();

            // For unit test
            _removeCallsTestOnly = 0;
            _clearAllCallsTestOnly = 0;
        }

        /// <summary>
        /// Singleton.
        /// </summary>
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
        /// Stores key-value in this StagingArea.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="encodedImage">
        /// EncodedImage to be associated with key.
        /// </param>
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
#if DEBUG_STAGING_AREA
                LogStats();
#endif // DEBUG_STAGING_AREA
            }
        }

        /// <summary>
        /// Removes all items from the StagingArea.
        /// </summary>
        public void ClearAll()
        {
            // For unit test
            ++_clearAllCallsTestOnly;

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
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <returns>true if item was removed.</returns>
        public bool Remove(ICacheKey key)
        {
            // For unit test
            ++_removeCallsTestOnly;

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
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="encodedImage">Value corresponding to key.</param>
        /// <returns>true if item was removed.</returns>
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

#if DEBUG_STAGING_AREA
                LogStats();
#endif // DEBUG_STAGING_AREA
                return true;
            }
        }

        /// <summary>
        /// Gets the encoded image.
        /// </summary>
        /// <returns>
        /// Value associated with given key or null if no value is associated.
        /// </returns>
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
                        // Reference is not valid, this means that someone
                        // cleared reference while it was still in use.
                        // Log error TODO: 3697790
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
                    // Reference is not valid, this means that someone cleared reference
                    // while it was still in use.
                    // Log error TODO: 3697790
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
