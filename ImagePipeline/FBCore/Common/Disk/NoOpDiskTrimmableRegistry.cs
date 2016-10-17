namespace FBCore.Common.Disk
{
    /// <summary>
    /// Implementation of <see cref="IDiskTrimmableRegistry"/> that does not do anything.
    /// </summary>
    public class NoOpDiskTrimmableRegistry : IDiskTrimmableRegistry
    {
        private static readonly object _instanceGate = new object();
        private static NoOpDiskTrimmableRegistry _instance = null;

        /// <summary>
        /// Returns how many times the RegisterDiskTrimmable method has been invoked
        /// </summary>
        public int RegisterDiskTrimmableCount { get; set; }

        /// <summary>
        /// Returns how many times the UnregisterDiskTrimmable method has been invoked
        /// </summary>
        public int UnregisterDiskTrimmableCount { get; set; }

        private NoOpDiskTrimmableRegistry()
        {
        }

        /// <summary>
        /// Singleton
        /// </summary>
        /// <returns></returns>
        public static NoOpDiskTrimmableRegistry Instance
        {
            get
            {
                lock (_instanceGate)
                {
                    if (_instance == null)
                    {
                        _instance = new NoOpDiskTrimmableRegistry();
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Reset counters
        /// </summary>
        public void ResetCounter()
        {
            RegisterDiskTrimmableCount = 0;
            UnregisterDiskTrimmableCount = 0;
        }

        /// <summary>
        /// Register an object
        /// </summary>
        /// <param name="trimmable"></param>
        public void RegisterDiskTrimmable(IDiskTrimmable trimmable)
        {
            ++RegisterDiskTrimmableCount;
        }

        /// <summary>
        /// Unregister an object
        /// </summary>
        /// <param name="trimmable"></param>
        public void UnregisterDiskTrimmable(IDiskTrimmable trimmable)
        {
            ++UnregisterDiskTrimmableCount;
        }
    }
}
