using System;

namespace Cache.Common
{
    /// <summary>
    /// Strongly typed cache key to be used instead of <see cref="object"/>.
    ///
    /// <para /> <see cref="ToString"/>, <see cref="Equals"/> and <see cref="GetHashCode"/> methods must be implemented.
    /// </summary>
    public interface ICacheKey
    {
        ///  This is useful for instrumentation and debugging purposes. 
        string ToString();

        ///  This method must be implemented, otherwise the cache keys will be be compared by reference. 
        bool Equals(object o);

        ///  This method must be implemented with accordance to the <see cref="Equals"/> method. 
        int GetHashCode();

        /// <summary>
        /// Returns true if this key was constructed from this <see cref="Uri"/>.
        ///
        /// Used for cases like deleting all keys for a given uri.
        /// </summary>
        bool ContainsUri(Uri uri);
    }
}
