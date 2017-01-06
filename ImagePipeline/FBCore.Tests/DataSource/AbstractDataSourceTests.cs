using FBCore.Concurrency;
using FBCore.DataSource;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;

namespace FBCore.Tests.DataSource
{
    /// <summary>
    /// Tests for abstract data source
    /// </summary>
    [TestClass]
    public class AbstractDataSourceTests
    {
        private IExecutorService _executor1;
        private IExecutorService _executor2;
        private IDataSubscriber<IValue> _dataSubscriber1;
        private IDataSubscriber<IValue> _dataSubscriber2;
        private FakeAbstractDataSource _dataSource;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _executor1 = CallerThreadExecutor.Instance;
            _executor2 = CallerThreadExecutor.Instance;
            _dataSubscriber1 = new MockDataSubscriber<IValue>();
            _dataSubscriber2 = new MockDataSubscriber<IValue>();
            _dataSource = new FakeAbstractDataSource();
        }

        private void VerifySubscribers(int expected)
        {
            switch (expected)
            {
                case DataSourceTestUtils.NO_INTERACTIONS:
                    Assert.IsTrue(((MockDataSubscriber<IValue>)_dataSubscriber1).HasZeroInteractions);
                    Assert.IsTrue(((MockDataSubscriber<IValue>)_dataSubscriber2).HasZeroInteractions);
                    break;

                case DataSourceTestUtils.ON_NEW_RESULT:
                    Assert.IsTrue(((MockDataSubscriber<IValue>)_dataSubscriber1).OnNewResultCallCount == 1);
                    Assert.IsTrue(((MockDataSubscriber<IValue>)_dataSubscriber2).OnNewResultCallCount == 1);
                    break;

                case DataSourceTestUtils.ON_FAILURE:
                    Assert.IsTrue(((MockDataSubscriber<IValue>)_dataSubscriber1).OnFailureCallCount == 1);
                    Assert.IsTrue(((MockDataSubscriber<IValue>)_dataSubscriber2).OnFailureCallCount == 1);
                    break;

                case DataSourceTestUtils.ON_CANCELLATION:
                    Assert.IsTrue(((MockDataSubscriber<IValue>)_dataSubscriber1).OnCancellationCallCount == 1);
                    Assert.IsTrue(((MockDataSubscriber<IValue>)_dataSubscriber2).OnCancellationCallCount == 1);
                    break;
            }

            ((MockDataSubscriber<IValue>)_dataSubscriber1).Reset();
            ((MockDataSubscriber<IValue>)_dataSubscriber2).Reset();
        }

        private void Subscribe()
        {
            _dataSource.Subscribe(_dataSubscriber1, _executor1);
            _dataSource.Subscribe(_dataSubscriber2, _executor2);
        }

        /// <summary>
        /// Tests out the initial state
        /// </summary>
        [TestMethod]
        public void TestInitialState()
        {
            DataSourceTestUtils.VerifyState(
                _dataSource, 
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);
        }

        /// <summary>
        /// Tests out the last result and close
        /// </summary>
        [TestMethod]
        public void TestLifeCycle_LastResult_Close()
        {
            Subscribe();

            // Last result
            IValue value = new MockValue();
            _dataSource.SetResult(value, DataSourceTestUtils.LAST);
            VerifySubscribers(DataSourceTestUtils.ON_NEW_RESULT);
            DataSourceTestUtils.VerifyState(
                _dataSource, 
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT, 
                value,
                DataSourceTestUtils.NOT_FAILED, 
                null);
            
            // Close
            _dataSource.Close();
            VerifySubscribers(DataSourceTestUtils.NO_INTERACTIONS);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);
        }

        /// <summary>
        /// Tests out the failure and close
        /// </summary>
        [TestMethod]
        public void TestLifeCycle_Failure_Close()
        {
            Subscribe();

            // failure
            Exception throwable = new Exception();
            _dataSource.SetFailure(throwable);
            VerifySubscribers(DataSourceTestUtils.ON_FAILURE);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                throwable);

            // close
            _dataSource.Close();
            VerifySubscribers(DataSourceTestUtils.NO_INTERACTIONS);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                throwable);
        }

        /// <summary>
        /// Tests out the intermediate result, last result and close
        /// </summary>
        [TestMethod]
        public void TestLifeCycle_IntermediateResult_LastResult_Close()
        {
            Subscribe();
            
            // intermediate result
            IValue value1 = new MockValue();
            _dataSource.SetResult(value1, DataSourceTestUtils.INTERMEDIATE);
            VerifySubscribers(DataSourceTestUtils.ON_NEW_RESULT);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                value1,
                DataSourceTestUtils.NOT_FAILED,
                null);

            // last result
            IValue value = new MockValue();
            _dataSource.SetResult(value, DataSourceTestUtils.LAST);
            VerifySubscribers(DataSourceTestUtils.ON_NEW_RESULT);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                value,
                DataSourceTestUtils.NOT_FAILED,
                null);

            // close
            _dataSource.Close();
            VerifySubscribers(DataSourceTestUtils.NO_INTERACTIONS);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);
        }

        /// <summary>
        /// Tests out the intermediate result, failure and close
        /// </summary>
        [TestMethod]
        public void TestLifeCycle_IntermediateResult_Failure_Close()
        {
            Subscribe();

            // intermediate result
            IValue value1 = new MockValue();
            _dataSource.SetResult(value1, DataSourceTestUtils.INTERMEDIATE);
            VerifySubscribers(DataSourceTestUtils.ON_NEW_RESULT);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                value1,
                DataSourceTestUtils.NOT_FAILED,
                null);

            // failure
            Exception throwable = new Exception();
            _dataSource.SetFailure(throwable);
            VerifySubscribers(DataSourceTestUtils.ON_FAILURE);
            DataSourceTestUtils.VerifyState(
                    _dataSource,
                    DataSourceTestUtils.NOT_CLOSED,
                    DataSourceTestUtils.FINISHED,
                    DataSourceTestUtils.WITH_RESULT,
                    value1,
                    DataSourceTestUtils.FAILED,
                    throwable);

            // close
            _dataSource.Close();
            VerifySubscribers(DataSourceTestUtils.NO_INTERACTIONS);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                throwable);
        }

        /// <summary>
        /// Tests out the interactions after setting successfully
        /// </summary>
        [TestMethod]
        public void TestLifeCycle_AfterSuccess()
        {
            Subscribe();

            // success
            IValue value = new MockValue();
            _dataSource.SetResult(value, DataSourceTestUtils.LAST);
            VerifySubscribers(DataSourceTestUtils.ON_NEW_RESULT);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                value,
                DataSourceTestUtils.NOT_FAILED,
                null);

            // try intermediate
            _dataSource.SetResult(new MockValue(), DataSourceTestUtils.INTERMEDIATE);
            VerifySubscribers(DataSourceTestUtils.NO_INTERACTIONS);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                value,
                DataSourceTestUtils.NOT_FAILED,
                null);

            // try last
            _dataSource.SetResult(new MockValue(), DataSourceTestUtils.LAST);
            VerifySubscribers(DataSourceTestUtils.NO_INTERACTIONS);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                value,
                DataSourceTestUtils.NOT_FAILED,
                null);

            // try failure
            _dataSource.SetFailure(new Exception());
            VerifySubscribers(DataSourceTestUtils.NO_INTERACTIONS);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                value,
                DataSourceTestUtils.NOT_FAILED,
                null);
        }

        /// <summary>
        /// Tests out the interactions after failure
        /// </summary>
        [TestMethod]
        public void TestLifeCycle_AfterFailure()
        {
            Subscribe();

            // failure
            Exception throwable = new Exception();
            _dataSource.SetFailure(throwable);
            VerifySubscribers(DataSourceTestUtils.ON_FAILURE);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                throwable);

            // try intermediate
            _dataSource.SetResult(new MockValue(), DataSourceTestUtils.INTERMEDIATE);
            VerifySubscribers(DataSourceTestUtils.NO_INTERACTIONS);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                throwable);

            // try last
            _dataSource.SetResult(new MockValue(), DataSourceTestUtils.LAST);
            VerifySubscribers(DataSourceTestUtils.NO_INTERACTIONS);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                throwable);

            // try failure
            _dataSource.SetFailure(new Exception());
            VerifySubscribers(DataSourceTestUtils.NO_INTERACTIONS);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                throwable);
        }

        /// <summary>
        /// Tests out the interactions after being closed
        /// </summary>
        [TestMethod]
        public void TestLifeCycle_AfterClose()
        {
            Subscribe();

            // close
            _dataSource.Close();
            VerifySubscribers(DataSourceTestUtils.ON_CANCELLATION);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);

            // try intermediate
            _dataSource.SetResult(new MockValue(), DataSourceTestUtils.INTERMEDIATE);
            VerifySubscribers(DataSourceTestUtils.NO_INTERACTIONS);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);

            // try last
            _dataSource.SetResult(new MockValue(), DataSourceTestUtils.LAST);
            VerifySubscribers(DataSourceTestUtils.NO_INTERACTIONS);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);

            // try failure
            _dataSource.SetFailure(new Exception());
            VerifySubscribers(DataSourceTestUtils.NO_INTERACTIONS);
            DataSourceTestUtils.VerifyState(
                _dataSource,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);
        }

        /// <summary>
        /// Tests out the subcribe in progress
        /// </summary>
        [TestMethod]
        public void TestSubscribe_InProgress_WithoutResult()
        {
            Subscribe();
            VerifySubscribers(DataSourceTestUtils.NO_INTERACTIONS);
        }

        /// <summary>
        /// Tests subcribe in progress with result
        /// </summary>
        [TestMethod]
        public void TestSubscribe_InProgress_WithResult()
        {
            _dataSource.SetResult(new MockValue(), DataSourceTestUtils.INTERMEDIATE);
            Subscribe();
            VerifySubscribers(DataSourceTestUtils.ON_NEW_RESULT);
        }

        /// <summary>
        /// Tests subcribe finished
        /// </summary>
        [TestMethod]
        public void TestSubscribe_Finished_WithoutResult()
        {
            _dataSource.SetResult(null, DataSourceTestUtils.LAST);
            Subscribe();
            VerifySubscribers(DataSourceTestUtils.ON_NEW_RESULT);
        }

        /// <summary>
        /// Tests subcribe finished with result
        /// </summary>
        [TestMethod]
        public void TestSubscribe_Finished_WithResult()
        {
            _dataSource.SetResult(new MockValue(), DataSourceTestUtils.LAST);
            Subscribe();
            VerifySubscribers(DataSourceTestUtils.ON_NEW_RESULT);
        }

        /// <summary>
        /// Tests subcribe failed
        /// </summary>
        [TestMethod]
        public void TestSubscribe_Failed_WithoutResult()
        {
            _dataSource.SetFailure(new Exception());
            Subscribe();
            VerifySubscribers(DataSourceTestUtils.ON_FAILURE);
        }

        /// <summary>
        /// Tests subcribe failed with result
        /// </summary>
        [TestMethod]
        public void TestSubscribe_Failed_WithResult()
        {
            _dataSource.SetResult(new MockValue(), DataSourceTestUtils.INTERMEDIATE);
            _dataSource.SetFailure(new Exception());
            Subscribe();
            VerifySubscribers(DataSourceTestUtils.ON_FAILURE);
        }

        /// <summary>
        /// Tests out the interaction after subcribe and close
        /// </summary>
        [TestMethod]
        public void TestSubscribe_Closed_AfterSuccess()
        {
            _dataSource.SetResult(new MockValue(), DataSourceTestUtils.LAST);
            _dataSource.Close();
            Subscribe();
            VerifySubscribers(DataSourceTestUtils.NO_INTERACTIONS);
        }

        /// <summary>
        /// Tests out the interaction after failure and close
        /// </summary>
        [TestMethod]
        public void TestSubscribe_Closed_AfterFailure()
        {
            _dataSource.SetFailure(new Exception());
            _dataSource.Close();
            Subscribe();
            VerifySubscribers(DataSourceTestUtils.NO_INTERACTIONS);
        }

        /// <summary>
        /// Tests out the interaction after close
        /// </summary>
        [TestMethod]
        public void TestCloseResult()
        {
            IValue value1 = new MockValue();
            _dataSource.SetResult(value1, false);

            IValue value2 = new MockValue();
            _dataSource.SetResult(value2, false);
            Assert.IsTrue(((MockValue)value1).CloseCallCount == 1);
            Assert.IsTrue(((MockValue)value2).CloseCallCount == 0);

            IValue value3 = new MockValue();
            _dataSource.SetResult(value3, false);
            Assert.IsTrue(((MockValue)value2).CloseCallCount == 1);
            Assert.IsTrue(((MockValue)value3).CloseCallCount == 0);

            _dataSource.Close();
            Assert.IsTrue(((MockValue)value3).CloseCallCount == 1);
        }
    
        interface IValue
        {
            void Close();
        }

        class MockValue : IValue
        {
            private int _closeCallCount;

            public MockValue()
            {
                _closeCallCount = 0;
            }

            public void Close()
            {
                ++_closeCallCount;
            }

            internal int CloseCallCount
            {
                get
                {
                    return _closeCallCount;
                }
            }
        }

        class FakeAbstractDataSource : MockAbstractDataSource<IValue>
        {
            public override void CloseResult(IValue result)
            {
                result.Close();
            }
        }
    }
}
