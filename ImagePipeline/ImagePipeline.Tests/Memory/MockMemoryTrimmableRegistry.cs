using FBCore.Common.Memory;

namespace ImagePipeline.Tests.Memory
{
    public class MockMemoryTrimmableRegistry : IMemoryTrimmableRegistry
    {
        public void RegisterMemoryTrimmable(IMemoryTrimmable trimmable)
        {
        }

        public void UnregisterMemoryTrimmable(IMemoryTrimmable trimmable)
        {
        }
    }
}
