using FBCore.Common.Internal;

namespace ImagePipelineBase.Tests.ImagePipeline.Cache
{
    /// <summary>
    /// Mock Supplier class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MockSupplier<T> : ISupplier<T>
    {
        private T _value;

        /// <summary>
        /// Returns how many times the Get method has been invoked
        /// </summary>
        public int GetCallCount { get; private set; }

        /// <summary>
        /// Instantiates the mock supplier
        /// </summary>
        /// <param name="value"></param>
        public MockSupplier(T value)
        {
            _value = value;
        }

        /// <summary>
        /// Mock Get method
        /// </summary>
        /// <returns></returns>
        public T Get()
        {
            ++GetCallCount;
            return _value;
        }
    }
}
