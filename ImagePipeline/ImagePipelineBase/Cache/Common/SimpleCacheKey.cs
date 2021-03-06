﻿using FBCore.Common.Internal;
using System;

namespace Cache.Common
{
    /// <summary>
    /// <see cref="ICacheKey"/> implementation that is a simple wrapper around a
    /// <see cref="string"/> object.
    ///
    /// <para />Users of ICacheKey should construct it by providing a unique
    /// string that unambiguously identifies the cached resource.
    /// </summary>
    public class SimpleCacheKey : ICacheKey
    {
        private string _key;

        /// <summary>
        /// Instantiate the <see cref="SimpleCacheKey"/>.
        /// </summary>
        public SimpleCacheKey(string key)
        {
            _key = Preconditions.CheckNotNull(key);
        }

        /// <summary>
        /// This is useful for instrumentation and debugging purposes.
        /// </summary>
        public override string ToString()
        {
            return _key;
        }

        /// <summary>
        /// Compares objects _key.
        /// </summary>
        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o.GetType() == typeof(SimpleCacheKey))
            {
                SimpleCacheKey otherKey = (SimpleCacheKey)o;
                return _key.Equals(otherKey._key);
            }

            return false;
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        public override int GetHashCode()
        {
            return _key.GetHashCode();
        }

        /// <summary>
        /// Returns true if this key was constructed from this <see cref="Uri"/>.
        ///
        /// Used for cases like deleting all keys for a given uri.
        /// </summary>
        public bool ContainsUri(Uri uri)
        {
            return _key.Contains(uri.ToString());
        }
    }
}
