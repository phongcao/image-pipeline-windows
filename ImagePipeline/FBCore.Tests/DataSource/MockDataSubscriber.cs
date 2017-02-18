using FBCore.DataSource;
using System.Threading.Tasks;

namespace FBCore.Tests.DataSource
{
    class MockDataSubscriber<T> : IDataSubscriber<T>
    {
        private int _onNewResultCallCount;
        private int _onFailureCallCount;
        private int _onCancellationCallCount;
        private int _onProgressUpdateCallCount;
        private IDataSource<T> _dataSource;

        public Task OnNewResult(IDataSource<T> dataSource)
        {
            ++_onNewResultCallCount;
            _dataSource = dataSource;
            return Task.CompletedTask;
        }

        public void OnFailure(IDataSource<T> dataSource)
        {
            ++_onFailureCallCount;
            _dataSource = dataSource;
        }

        public void OnCancellation(IDataSource<T> dataSource)
        {
            ++_onCancellationCallCount;
            _dataSource = dataSource;
        }

        public void OnProgressUpdate(IDataSource<T> dataSource)
        {
            ++_onProgressUpdateCallCount;
            _dataSource = dataSource;
        }

        internal int OnNewResultCallCount
        {
            get
            {
                return _onNewResultCallCount;
            }
        }

        internal int OnFailureCallCount
        {
            get
            {
                return _onFailureCallCount;
            }
        }

        internal int OnCancellationCallCount
        {
            get
            {
                return _onCancellationCallCount;
            }
        }

        internal IDataSource<T> DataSource
        {
            get
            {
                return _dataSource;
            }
        }

        internal bool HasZeroInteractions
        {
            get
            {
                return (_onNewResultCallCount == 0) &&
                    (_onFailureCallCount == 0) &&
                    (_onCancellationCallCount == 0) &&
                    (_onProgressUpdateCallCount == 0);
            }
        }

        internal void Reset()
        {
            _onNewResultCallCount = 0;
            _onFailureCallCount = 0;
            _onCancellationCallCount = 0;
            _onProgressUpdateCallCount = 0;
            _dataSource = null;
        }
    }
}
