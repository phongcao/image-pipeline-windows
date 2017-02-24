using System;

namespace Cache.Common
{
    /// <summary>
    /// Strongly typed cache key to be used instead of <see cref="object"/>.
    ///
    /// <para /><see cref="ToString"/>, <see cref="Equals"/> and
    /// <see cref="GetHashCode"/> methods must be implemented.
    /// </summary>
    public interface ICacheKey
    {
        /// <summary>
        /// This is useful for instrumentation and debugging purposes.
        /// </summary>
        string ToString();

        /// <summary>
        /// This method must be implemented, otherwise the cache keys will be
        /// compared by reference.
        /// </summary>
        bool Equals(object o);

        /// <summary>
        /// This method must be implemented with accordance to the
        /// <see cref="Equals"/> method.
        /// </summary>
        int GetHashCode();

        /// <summary>
        /// Returns true if this key was constructed from this <see cref="Uri"/>.
        ///
        /// Used for cases like deleting all keys for a given uri.
        /// </summary>
        bool ContainsUri(Uri uri);
    }
}
