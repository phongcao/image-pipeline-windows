using FBCore.Concurrency;
using FBCore.DataSource;
using System;
using System.Collections.Generic;

namespace FBCore.Tests.DataSource
{
    class MockDataSource<T> : IDataSource<T>
    {
        private bool _isClosed;
        private bool _hasResult;
        private T _value;
        private bool _isFinished;
        private bool _hasFailed;
        private Exception _failureCause;

        // Mock
        private IDictionary<string, IList<int>> _methodInvocations;
        private int _inOrderCount;
        private int _response;

        public MockDataSource()
        {
            _methodInvocations = new Dictionary<string, IList<int>>(9);
            _inOrderCount = 0;
            _response = -1;
        }

        public void SetState(
            bool isClosed,
            bool isFinished,
            bool hasResult,
            T value,
            bool hasFailed,
            Exception failureCause)
        {
            _isClosed = isClosed;
            _isFinished = isFinished;
            _hasResult = hasResult;
            _value = value;
            _hasFailed = hasFailed;
            _failureCause = failureCause;
        }

        /// <summary>
        /// @return true if the data source is closed, false otherwise
        /// </summary>
        public bool IsClosed
        {
            get
            {
                AddMethodInvocation("IsClosed");
                return _isClosed;
            }
        }

        /// <summary>
        /// The most recent result of the asynchronous computation.
        ///
        /// <para />The caller gains ownership of the object and is responsible for releasing it.
        /// Note that subsequent calls to getResult might give different results. Later results should be
        /// considered to be of higher quality.
        ///
        /// <para />This method will return null in the following cases:
        /// when the DataSource does not have a result (<code> HasResult</code> returns false).
        /// when the last result produced was null.
        /// @return current best result
        /// </summary>
        public T GetResult()
        {
            AddMethodInvocation("GetResult");
            return _value;
        }

        /// <summary>
        /// @return true if any result (possibly of lower quality) is available right now, false otherwise
        /// </summary>
        public bool HasResult
        {
            get
            {
                AddMethodInvocation("HasResult");
                return _hasResult;
            }
        }

        /// <summary>
        /// @return true if request is finished, false otherwise
        /// </summary>
        public bool IsFinished
        {
            get
            {
                AddMethodInvocation("IsFinished");
                return _isFinished;
            }
        }

        /// <summary>
        /// @return true if request finished due to error
        /// </summary>
        public bool HasFailed
        {
            get
            {
                AddMethodInvocation("HasFailed");
                return _hasFailed;
            }
        }

        /// <summary>
        /// @return failure cause if the source has failed, else null
        /// </summary>
        public Exception GetFailureCause()
        {
            AddMethodInvocation("GetFailureCause");
            return _failureCause;
        }

        /// <summary>
        /// @return progress in range [0, 1]
        /// </summary>
        public float GetProgress()
        {
            AddMethodInvocation("GetProgress");
            return 0;
        }

        /// <summary>
        /// Cancels the ongoing request and releases all associated resources.
        ///
        /// <para />Subsequent calls to <see cref="GetResult"/> will return null.
        /// @return true if the data source is closed for the first time
        /// </summary>
        public bool Close()
        {
            AddMethodInvocation("Close");
            return true;
        }

        /// <summary>
        /// Subscribe for notifications whenever the state of the DataSource changes.
        ///
        /// <para />All changes will be observed on the provided executor.
        /// <param name="dataSubscriber"></param>
        /// <param name="executor"></param>
        /// </summary>
        public void Subscribe(IDataSubscriber<T> dataSubscriber, IExecutorService executor)
        {
            AddMethodInvocation("Subscribe");
            DataSubscriber = dataSubscriber;
            switch (_response)
            {
                case DataSourceTestUtils.NO_INTERACTIONS:
                    break;

                case DataSourceTestUtils.ON_NEW_RESULT:
                    dataSubscriber.OnNewResult(this);
                    break;

                case DataSourceTestUtils.ON_FAILURE:
                    dataSubscriber.OnFailure(this);
                    break;

                case DataSourceTestUtils.ON_CANCELLATION:
                    dataSubscriber.OnCancellation(this);
                    break;
            }
        }

        internal IDataSubscriber<T> DataSubscriber { get; set; }

        internal bool VerifyMethodInvocation(string methodName, int minNumberOfInvocations)
        {
            IList<int> methodInvocation = default(IList<int>);
            if (_methodInvocations.TryGetValue(methodName, out methodInvocation))
            {
                _methodInvocations.Remove(methodName);
                return methodInvocation[0] >= minNumberOfInvocations;
            }

            return (minNumberOfInvocations == 0) || false;
        }

        internal bool VerifyMethodInvocationOrder(string methodName, int order)
        {
            IList<int> methodInvocation = default(IList<int>);
            if (_methodInvocations.TryGetValue(methodName, out methodInvocation))
            {
                return methodInvocation[1] == order;
            }

            return false;
        }

        internal bool VerifyNoMoreInteraction()
        {
            return _methodInvocations.Count == 0;
        }

        internal void RespondOnSubscribe(int response)
        {
            _response = response;
        }

        private void AddMethodInvocation(string methodName)
        {
            IList<int> methodInvocation = default(IList<int>);
            if (_methodInvocations.TryGetValue(methodName, out methodInvocation))
            {
                ++methodInvocation[0];
                methodInvocation[1] = _inOrderCount;
                //_methodInvocations[methodName] = methodInvocation;
            }
            else
            {
                methodInvocation = new List<int>();
                methodInvocation.Add(1); // number of invocations
                methodInvocation.Add(_inOrderCount); // order
                _methodInvocations.Add(methodName, methodInvocation);
            }

            ++_inOrderCount;
        }
    }
}
