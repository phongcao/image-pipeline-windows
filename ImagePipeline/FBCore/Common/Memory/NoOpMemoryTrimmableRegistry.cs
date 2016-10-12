namespace FBCore.Common.Memory
{
    /// <summary>
    /// Implementation of <see cref="IMemoryTrimmableRegistry"/> that does not do anything.
    /// </summary>
    public class NoOpMemoryTrimmableRegistry : IMemoryTrimmableRegistry
    {
        private static readonly object _instanceGate = new object();
        private static NoOpMemoryTrimmableRegistry _instance = null;

        /// <summary>
        /// Instantiates the <see cref="NoOpMemoryTrimmableRegistry"/>.
        /// </summary>
        public NoOpMemoryTrimmableRegistry()
        {
        }

        /// <summary>
        /// Singleton
        /// </summary>
        /// <returns></returns>
        public static NoOpMemoryTrimmableRegistry Instance
        {
            get
            {
                lock (_instanceGate)
                {
                    if (_instance == null)
                    {
                        _instance = new NoOpMemoryTrimmableRegistry();
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Register an object.
        /// </summary>
        /// <param name="trimmable"></param>
        public void RegisterMemoryTrimmable(IMemoryTrimmable trimmable)
        {
        }

        /// <summary>
        /// Unregister an object.
        /// </summary>
        /// <param name="trimmable"></param>
        public void UnregisterMemoryTrimmable(IMemoryTrimmable trimmable)
        {
        }
    }
}
