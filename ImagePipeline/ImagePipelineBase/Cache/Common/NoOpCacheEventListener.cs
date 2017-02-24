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
        /// Gets singleton.
        /// </summary>
        public static NoOpCacheEventListener Instance
        {
            get
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
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnHit(ICacheEvent cacheEvent) { }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnMiss(ICacheEvent cacheEvent) { }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnWriteAttempt(ICacheEvent cacheEvent) { }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnWriteSuccess(ICacheEvent cacheEvent) { }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnReadException(ICacheEvent cacheEvent) { }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnWriteException(ICacheEvent cacheEvent) { }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnEviction(ICacheEvent cacheEvent) { }

        /// <summary>
        /// Do nothing.
        /// </summary>
        public void OnCleared() { }
    }
}
