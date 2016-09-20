namespace Cache.Common
{
    /// <summary>
    /// Extension of <see cref="SimpleCacheKey"/> which adds the ability to hold a caller context. This can be
    /// of use for debugging and has no bearing on equality.
    /// </summary>
    public class DebuggingCacheKey : SimpleCacheKey
    {
        private readonly object _callerContext;

        /// <summary>
        /// Instantiates the <see cref="DebuggingCacheKey"/>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="callerContext"></param>
        public DebuggingCacheKey(string key, object callerContext) : base(key)
        {
            _callerContext = callerContext;
        }

        /// <summary>
        /// Gets the caller context
        /// </summary>
        public object CallerContext
        {
            get
            {
                return _callerContext;
            }
        }
    }
}
