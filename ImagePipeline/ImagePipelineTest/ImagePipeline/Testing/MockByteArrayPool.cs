using ImagePipeline.Memory;
using System.Threading;

namespace ImagePipelineTest.ImagePipeline.Testing
{
    /// <summary>
    /// Mock ByteArrayPool
    /// </summary>
    class MockByteArrayPool : IByteArrayPool
    {
        private int _getCallCount;
        private int _releaseCallCount;
        private byte[] _value;

        /// <summary>
        /// Instantiates the <see cref="MockByteArrayPool"/>.
        /// </summary>
        /// <param name="value"></param>
        public MockByteArrayPool(byte[] value)
        {
            _getCallCount = 0;
            _releaseCallCount = 0;
            _value = value;
        }

        /// <summary>
        /// Mock Get method
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public byte[] Get(int size)
        {
            Interlocked.Increment(ref _getCallCount);
            return _value;
        }

        /// <summary>
        /// Mock Release method
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public void Release(byte[] value)
        {
            Interlocked.Increment(ref _releaseCallCount);
        }

        /// <summary>
        /// Mock Trim method
        /// </summary>
        /// <param name="trimType"></param>
        public void Trim(double trimType)
        {
        }

        /// <summary>
        /// Mock method
        /// </summary>
        public int CallCount
        {
            get
            {
                return Volatile.Read(ref _getCallCount);
            }
        }

        /// <summary>
        /// Mock method
        /// </summary>
        public int ReleaseCallCount
        {
            get
            {
                return Volatile.Read(ref _releaseCallCount);
            }
        }
    }
}
