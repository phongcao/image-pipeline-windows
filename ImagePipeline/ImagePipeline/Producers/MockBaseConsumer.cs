using System;
using System.Threading;

namespace ImagePipeline.Producers
{
    /// <summary>
    ///  Mock BaseConsumer for unit tests
    /// </summary>
    public class MockBaseConsumer<T> : BaseConsumer<T>
    {
        private Action _onCancellationImplFunc;
        private Action<Exception> _onFailureImplFunc;
        private Action<T, bool> _onNewResultImplFunc;
        private int _onNewResultCount;
        private int _onFailureCount;
        private int _onCancellationCount;

        /// <summary>
        ///  Instantiates the MockBaseConsumer
        /// </summary>
        public MockBaseConsumer()
        {
            _onNewResultCount = 0;
            _onFailureCount = 0;
            _onCancellationCount = 0;
        }

        /// <summary>
        /// Mock implementation
        /// </summary>
        protected override void OnCancellationImpl()
        {
            Interlocked.Increment(ref _onCancellationCount);
            _onCancellationImplFunc?.Invoke();
        }

        /// <summary>
        /// Mock implementation
        /// </summary>
        protected override void OnFailureImpl(Exception error)
        {
            Interlocked.Increment(ref _onFailureCount);
            _onFailureImplFunc?.Invoke(error);
        }

        /// <summary>
        /// Mock implementation
        /// </summary>
        protected override void OnNewResultImpl(T newResult, bool isLast)
        {
            Interlocked.Increment(ref _onNewResultCount);
            _onNewResultImplFunc?.Invoke(newResult, isLast);
        }

        /// <summary>
        /// Mock implementation
        /// </summary>
        public void SetOnCancellationImplFunc(Action onCancellationImplFunc)
        {
            _onCancellationImplFunc = onCancellationImplFunc;
        }

        /// <summary>
        /// Mock implementation
        /// </summary>
        public void SetOnFailureImplFunc(Action<Exception> onFailureImplFunc)
        {
            _onFailureImplFunc = onFailureImplFunc;
        }

        /// <summary>
        /// Mock implementation
        /// </summary>
        public void SetOnNewResultImplFunc(Action<T, bool> onNewResultImplFunc)
        {
            _onNewResultImplFunc = onNewResultImplFunc;
        }

        /// <summary>
        /// Mock implementation
        /// </summary>
        public int OnNewResultCount
        {
            get
            {
                return Volatile.Read(ref _onNewResultCount);
            }
        }

        /// <summary>
        /// Mock implementation
        /// </summary>
        public int OnFailureCount
        {
            get
            {
                return Volatile.Read(ref _onFailureCount);
            }
        }

        /// <summary>
        /// Mock implementation
        /// </summary>
        public int OnCancellationCount
        {
            get
            {
                return Volatile.Read(ref _onCancellationCount);
            }
        }
    }
}
