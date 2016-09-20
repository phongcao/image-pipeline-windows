using System;

namespace Cache.Common
{
    /// <summary>
    /// An implementation of <see cref="ICacheErrorLogger"/> that doesn't do anything.
    /// </summary>
    public class NoOpCacheErrorLogger : ICacheErrorLogger
    {
        private static readonly object _instanceGate = new object();
        private static NoOpCacheErrorLogger _instance = null;

        private NoOpCacheErrorLogger()
        {
        }

        /// <summary>
        /// Gets singleton
        /// </summary>
        /// <returns></returns>
        public static NoOpCacheErrorLogger GetInstance()
        {
            lock (_instanceGate)
            {
                if (_instance == null)
                {
                    _instance = new NoOpCacheErrorLogger();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Log an error of the specified category.
        /// <param name="category">Error category</param>
        /// <param name="clazz">Class reporting the error</param>
        /// <param name="message">An optional error message</param>
        /// </summary>
        public void LogError(
            CacheErrorCategory category,
            Type clazz,
            string message)
        {
        }
    }
}
