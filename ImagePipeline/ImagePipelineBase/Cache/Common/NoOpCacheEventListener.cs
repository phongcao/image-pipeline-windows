namespace Cache.Common
{
    /// <summary>
    /// Implementation of <see cref="ICacheEventListener"/> that doesn't do anything.
    /// </summary>
    public class NoOpCacheEventListener : ICacheEventListener
    {
        private static readonly object _instanceGate = new object();
        private static NoOpCacheEventListener _instance = null;

        private NoOpCacheEventListener()
        {
        }

        /// <summary>
        /// Gets singleton
        /// </summary>
        /// <returns></returns>
        public static NoOpCacheEventListener GetInstance()
        {
            lock (_instanceGate)
            {
                if (_instance == null)
                {
                    _instance = new NoOpCacheEventListener();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Do nothing
        /// </summary>
        /// <param name="cacheEvent"></param>
        public void OnHit(ICacheEvent cacheEvent) { }

        /// <summary>
        /// Do nothing
        /// </summary>
        /// <param name="cacheEvent"></param>
        public void OnMiss(ICacheEvent cacheEvent) { }

        /// <summary>
        /// Do nothing
        /// </summary>
        /// <param name="cacheEvent"></param>
        public void OnWriteAttempt(ICacheEvent cacheEvent) { }

        /// <summary>
        /// Do nothing
        /// </summary>
        /// <param name="cacheEvent"></param>
        public void OnWriteSuccess(ICacheEvent cacheEvent) { }

        /// <summary>
        /// Do nothing
        /// </summary>
        /// <param name="cacheEvent"></param>
        public void OnReadException(ICacheEvent cacheEvent) { }

        /// <summary>
        /// Do nothing
        /// </summary>
        /// <param name="cacheEvent"></param>
        public void OnWriteException(ICacheEvent cacheEvent) { }

        /// <summary>
        /// Do nothing
        /// </summary>
        /// <param name="cacheEvent"></param>
        public void OnEviction(ICacheEvent cacheEvent) { }

        /// <summary>
        /// Do nothing
        /// </summary>
        public void OnCleared() { }
    }
}
