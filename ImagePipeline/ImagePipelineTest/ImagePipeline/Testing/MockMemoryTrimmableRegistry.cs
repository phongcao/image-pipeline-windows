using FBCore.Common.Memory;

namespace ImagePipeline.Testing
{
    /// <summary>
    /// MockMemoryTrimmableRegistry class for unit tests.
    /// </summary>
    class MockMemoryTrimmableRegistry : IMemoryTrimmableRegistry
    {
        /// <summary>
        /// Mock RegisterMemoryTrimmable.
        /// </summary>
        public void RegisterMemoryTrimmable(IMemoryTrimmable trimmable)
        {
        }

        /// <summary>
        /// Mock UnregisterMemoryTrimmable.
        /// </summary>
        public void UnregisterMemoryTrimmable(IMemoryTrimmable trimmable)
        {
        }
    }
}
