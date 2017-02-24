using Cache.Common;
using FBCore.Common.Internal;
using FBCore.Common.Time;
using FBCore.Common.Util;
using ImagePipeline.Common;
using System;

namespace ImagePipeline.Cache
{
    /// <summary>
    /// Cache key for BitmapMemoryCache.
    /// </summary>
    public class BitmapMemoryCacheKey : ICacheKey
    {
        private readonly string _sourceString;
        private readonly ResizeOptions _resizeOptions;
        private readonly bool _autoRotated;
        private readonly ImageDecodeOptions _imageDecodeOptions;
        private readonly ICacheKey _postprocessorCacheKey;
        private readonly string _postprocessorName;
        private readonly int _hash;
        private readonly object _callerContext;
        private readonly long _cacheTime;

        /// <summary>
        ///  Instantites the <see cref="BitmapMemoryCacheKey"/>.
        /// </summary>
        public BitmapMemoryCacheKey(
            string sourceString,
            ResizeOptions resizeOptions,
            bool autoRotated,
            ImageDecodeOptions imageDecodeOptions,
            ICacheKey postprocessorCacheKey,
            string postprocessorName,
            object callerContext)
        {
            _sourceString = Preconditions.CheckNotNull(sourceString);
            _resizeOptions = resizeOptions;
            _autoRotated = autoRotated;
            _imageDecodeOptions = imageDecodeOptions;
            _postprocessorCacheKey = postprocessorCacheKey;
            _postprocessorName = postprocessorName;
            _hash = HashCodeUtil.HashCode(
                sourceString.GetHashCode(),
                (resizeOptions != null) ? resizeOptions.GetHashCode() : 0,
                autoRotated ? true.GetHashCode() : false.GetHashCode(),
                _imageDecodeOptions,
                _postprocessorCacheKey,
                postprocessorName);
            _callerContext = callerContext;
            _cacheTime = SystemClock.UptimeMillis;
        }

        /// <summary>
        /// Returns the source uri.
        /// </summary>
        public string SourceUriString
        {
            get
            {
                return _sourceString;
            }
        }

        /// <summary>
        /// Gets the post processor name.
        /// </summary>
        public string PostprocessorName
        {
            get
            {
                return _postprocessorName;
            }
        }

        /// <summary>
        /// Gets the caller context.
        /// </summary>
        public object CallerContext
        {
            get
            {
                return _callerContext;
            }
        }

        /// <summary>
        /// Gets the cache time.
        /// </summary>
        public long InBitmapCacheSince
        {
            get
            {
                return _cacheTime;
            }
        }

        /// <summary>
        /// Provides the custom Equals method to compare with other
        /// BitmapMemoryCacheKey objects.
        /// </summary>
        public override bool Equals(object o)
        {
            if (o.GetType() != typeof(BitmapMemoryCacheKey))
            {
                return false;
            }

            BitmapMemoryCacheKey otherKey = (BitmapMemoryCacheKey)o;
            return _hash == otherKey._hash &&
                _sourceString.Equals(otherKey._sourceString) &&
                Equals(_resizeOptions, otherKey._resizeOptions) &&
                _autoRotated == otherKey._autoRotated &&
                Equals(_imageDecodeOptions, otherKey._imageDecodeOptions) &&
                Equals(_postprocessorCacheKey, otherKey._postprocessorCacheKey) &&
                Equals(_postprocessorName, otherKey._postprocessorName);
        }

        /// <summary>
        /// Calculates the hash code basing on properties.
        /// </summary>
        public override int GetHashCode()
        {
            return _hash;
        }

        /// <summary>
        /// Provides the custom ToString method.
        /// </summary>
        public override string ToString()
        {
            return string.Format(
                "{0}_{1}_{2}_{3}_{4}_{5}_{6}",
                _sourceString,
                _resizeOptions,
                _autoRotated.ToString(),
                _imageDecodeOptions,
                _postprocessorCacheKey,
                _postprocessorName,
                _hash);
        }

        /// <summary>
        /// Returns true if this key was constructed from this
        /// <see cref="Uri"/>.
        /// 
        /// Used for cases like deleting all keys for a given uri.
        /// </summary>
        public bool ContainsUri(Uri uri)
        {
            return _sourceString.Contains(uri.ToString());
        }
    }
}
