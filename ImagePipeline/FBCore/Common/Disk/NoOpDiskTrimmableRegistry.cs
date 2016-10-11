namespace FBCore.Common.Disk
{
    /// <summary>
    /// Implementation of <see cref="IDiskTrimmableRegistry"/> that does not do anything.
    /// </summary>
    public class NoOpDiskTrimmableRegistry : IDiskTrimmableRegistry
    {
        private static readonly object _instanceGate = new object();
        private static NoOpDiskTrimmableRegistry _instance = null;

        private NoOpDiskTrimmableRegistry()
        {
        }

        /// <summary>
        /// Singleton
        /// </summary>
        /// <returns></returns>
        public static NoOpDiskTrimmableRegistry GetInstance()
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

        ///  Register an object
        public void RegisterDiskTrimmable(IDiskTrimmable trimmable)
        {
        }

        ///  Unregister an object
        public void UnregisterDiskTrimmable(IDiskTrimmable trimmable)
        {
        }
    }
}
