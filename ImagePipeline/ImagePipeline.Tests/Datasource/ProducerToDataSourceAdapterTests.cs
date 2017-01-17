using FBCore.Concurrency;
using FBCore.DataSource;
using ImagePipeline.Common;
using ImagePipeline.Datasource;
using ImagePipeline.Listener;
using ImagePipeline.Producers;
using ImagePipeline.Request;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;

namespace ImagePipeline.Tests.Datasource
{
    /// <summary>
    /// Tests for <see cref="ProducerToDataSourceAdapter{T}"/>
    /// </summary>
    [TestClass]
    public class ProducerToDataSourceAdapterTests
    {
        private const bool FINISHED = true;
        private const bool NOT_FINISHED = false;
        private const bool WITH_RESULT = true;
        private const bool WITHOUT_RESULT = false;
        private const bool FAILED = true;
        private const bool NOT_FAILED = false;
        private const bool LAST = true;
        private const bool INTERMEDIATE = false;
        private const int NO_INTERACTIONS = 0;
        private const int ON_NEW_RESULT = 1;
        private const int ON_FAILURE = 2;

        private static readonly Exception NPE = new NullReferenceException();
        private static readonly string _requestId = "requestId";
        private static readonly ImageRequest _imageRequest = ImageRequest.FromUri("http://microsoft.com");
        private static readonly object _callerContext = new object();
        private static readonly bool _isPrefetch = false;
        private static readonly Exception _exception = new Exception();

        private IRequestListener _requestListener;
        private ImageRequest _internalImageRequest;
        private object _internalCallerContext;
        private string _internalRequestId;
        private bool _internalIsPrefetch;
        private Exception _internalException;
        private bool _onRequestStartInvocation;
        private bool _onRequestSuccessInvocation;
        private bool _onRequestFailureInvocation;
        private bool _onRequestCancellationInvocation;

        private object _result1;
        private object _result2;
        private object _result3;

        private IDataSubscriber<object> _dataSubscriber1;
        private IDataSubscriber<object> _dataSubscriber2;

        private SettableProducerContext _settableProducerContext;
        private IProducer<object> _producer;
        private IConsumer<object> _internalConsumer;

        private IDataSource<object> _dataSource;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            // Initializes the mock RequestListener
            ProducerListenerImpl producerListener = new ProducerListenerImpl(
                (_, __) => { },
                (_, __, ___) => { },
                (_, __, ___) => { },
                (_, __, ___, ____) => { },
                (_, __, ___) => { },
                (_) => { return false; });
            _requestListener = new RequestListenerImpl(
                producerListener,
                (imageRequest, callerContext, requestId, isPrefetch) =>
                {
                    _onRequestStartInvocation = true;
                    _internalImageRequest = imageRequest;
                    _internalCallerContext = callerContext;
                    _internalRequestId = requestId;
                    _internalIsPrefetch = isPrefetch;
                },
                (imageRequest, requestId, isPrefetch) =>
                {
                    _onRequestSuccessInvocation = true;
                    _internalImageRequest = imageRequest;
                    _internalRequestId = requestId;
                    _internalIsPrefetch = isPrefetch;
                },
                (imageRequest, requestId, exception, isPrefetch) =>
                {
                    _onRequestFailureInvocation = true;
                    _internalImageRequest = imageRequest;
                    _internalRequestId = requestId;
                    _internalException = exception;
                    _internalIsPrefetch = isPrefetch;
                },
                (requestId) =>
                {
                    _onRequestCancellationInvocation = true;
                    _internalRequestId = requestId;
                });
            _result1 = new object();
            _result2 = new object();
            _result3 = new object();

            _dataSubscriber1 = new MockDataSubscriber<object>();
            _dataSubscriber2 = new MockDataSubscriber<object>();

            _internalIsPrefetch = true;
            _settableProducerContext = new SettableProducerContext(
                _imageRequest,
                _requestId,
                producerListener,
                _callerContext,
                RequestLevel.FULL_FETCH,
                _isPrefetch,
                true,
                Priority.HIGH);
            _producer = new ProducerImpl<object>(
                (consumer, _) =>
                {
                    _internalConsumer = consumer;
                });
            _dataSource = ProducerToDataSourceAdapter<object>.Create(
                _producer,
                _settableProducerContext,
                _requestListener);

            Assert.IsTrue(_onRequestStartInvocation);
            Assert.AreSame(_internalImageRequest, _imageRequest);
            Assert.AreSame(_internalCallerContext, _callerContext);
            Assert.AreSame(_internalRequestId, _requestId);
            Assert.IsFalse(_internalIsPrefetch);
            Assert.IsNotNull(_internalConsumer);
            _onRequestStartInvocation = false;

            _dataSource.Subscribe(_dataSubscriber1, CallerThreadExecutor.Instance);
        }

        /// <summary>
        /// Verification helpers
        /// </summary>
        private void VerifyState(
            bool isFinished,
            bool hasResult,
            object result,
            bool hasFailed,
            Exception failureCause)
        {
            IDataSource<object> dataSource = _dataSource;
            Assert.IsTrue(isFinished == dataSource.IsFinished(), "isFinished");
            Assert.IsTrue(hasResult == dataSource.HasResult(), "hasResult");
            Assert.AreSame(result, dataSource.GetResult(), "getResult");
            Assert.IsTrue(hasFailed == dataSource.HasFailed(), "hasFailed");
            if (failureCause == NPE)
            {
                Assert.IsNotNull(dataSource.GetFailureCause(), "failure");
                Assert.IsTrue(dataSource.GetFailureCause().GetType() == typeof(NullReferenceException), "failure");
            }
            else
            {
                Assert.AreSame(failureCause, dataSource.GetFailureCause(), "failure");
            }
        }

        private void VerifyNoMoreInteractionsAndReset()
        {
            // Verify
            Assert.IsFalse(_onRequestStartInvocation);
            Assert.IsFalse(_onRequestSuccessInvocation);
            Assert.IsFalse(_onRequestFailureInvocation);
            Assert.IsFalse(_onRequestCancellationInvocation);
            Assert.IsTrue(((MockDataSubscriber<object>)_dataSubscriber1).HasZeroInteractions);
            Assert.IsTrue(((MockDataSubscriber<object>)_dataSubscriber2).HasZeroInteractions);

            // Reset
            _internalImageRequest = null;
            _internalCallerContext = null;
            _internalRequestId = null;
            _internalIsPrefetch = true;
            _internalException = null;
            ((MockDataSubscriber<object>)_dataSubscriber1).Reset();
            ((MockDataSubscriber<object>)_dataSubscriber2).Reset();
        }

        /// <summary>
        /// State verification methods
        /// </summary>
        private void VerifyInitial()
        {
            VerifyState(NOT_FINISHED, WITHOUT_RESULT, null, NOT_FAILED, null);
            VerifyNoMoreInteractionsAndReset();
        }

        private void VerifyWithResult(object result, bool isLast)
        {
            VerifyState(isLast, result != null, result, NOT_FAILED, null);
            VerifyNoMoreInteractionsAndReset();
        }

        private void VerifyFailed(object result, Exception throwable)
        {
            VerifyState(FINISHED, result != null, result, FAILED, throwable);
            VerifyNoMoreInteractionsAndReset();
        }

        private void VerifyClosed(bool isFinished, Exception throwable)
        {
            VerifyState(isFinished, WITHOUT_RESULT, null, throwable != null, throwable);
            VerifyNoMoreInteractionsAndReset();
        }

        /// <summary>
        /// Event testing helpers
        /// </summary>
        private void TestSubscribe(int expected)
        {
            _dataSource.Subscribe(_dataSubscriber2, CallerThreadExecutor.Instance);
            switch (expected)
            {
                case NO_INTERACTIONS:
                    break;

                case ON_NEW_RESULT:
                    Assert.IsTrue(((MockDataSubscriber<object>)_dataSubscriber2).OnNewResultCallCount == 1);
                    Assert.AreSame(
                        _dataSource,
                        ((MockDataSubscriber<object>)_dataSubscriber2).DataSource);
                    ((MockDataSubscriber<object>)_dataSubscriber2).OnNewResultCallCount = 0;
                    break;

                case ON_FAILURE:
                    Assert.IsTrue(((MockDataSubscriber<object>)_dataSubscriber2).OnFailureCallCount == 1);
                    Assert.AreSame(
                        _dataSource,
                        ((MockDataSubscriber<object>)_dataSubscriber2).DataSource);
                    ((MockDataSubscriber<object>)_dataSubscriber2).OnFailureCallCount = 0;
                    break;
            }

            VerifyNoMoreInteractionsAndReset();
        }

        private void TestNewResult(
            object result,
            bool isLast,
            int numSubscribers)
        {
            _internalImageRequest = null;
            _internalRequestId = null;
            _internalIsPrefetch = true;
            _internalConsumer.OnNewResult(result, isLast);
            if (isLast)
            {
                Assert.IsTrue(_onRequestSuccessInvocation);
                Assert.AreSame(_internalImageRequest, _imageRequest);
                Assert.AreSame(_internalRequestId, _requestId);
                Assert.IsFalse(_internalIsPrefetch);
                _onRequestSuccessInvocation = false;
            }

            if (numSubscribers >= 1)
            {
                Assert.IsTrue(((MockDataSubscriber<object>)_dataSubscriber1).OnNewResultCallCount == 1);
                Assert.AreSame(
                    _dataSource,
                    ((MockDataSubscriber<object>)_dataSubscriber1).DataSource);
                ((MockDataSubscriber<object>)_dataSubscriber1).OnNewResultCallCount = 0;
            }

            if (numSubscribers >= 2)
            {
                Assert.IsTrue(((MockDataSubscriber<object>)_dataSubscriber2).OnNewResultCallCount == 1);
                Assert.AreSame(
                    _dataSource,
                    ((MockDataSubscriber<object>)_dataSubscriber2).DataSource);
                ((MockDataSubscriber<object>)_dataSubscriber2).OnNewResultCallCount = 0;
            }

            VerifyWithResult(result, isLast);
        }

        private void TestFailure(
            object result,
            int numSubscribers)
        {
            _internalImageRequest = null;
            _internalRequestId = null;
            _internalException = null;
            _internalIsPrefetch = true;
            _internalConsumer.OnFailure(_exception);
            Assert.IsTrue(_onRequestFailureInvocation);
            Assert.AreSame(_internalImageRequest, _imageRequest);
            Assert.AreSame(_internalRequestId, _requestId);
            Assert.AreSame(_internalException, _exception);
            Assert.IsFalse(_internalIsPrefetch);
            _onRequestFailureInvocation = false;

            if (numSubscribers >= 1)
            {
                Assert.IsTrue(((MockDataSubscriber<object>)_dataSubscriber1).OnFailureCallCount == 1);
                Assert.AreSame(
                    _dataSource,
                    ((MockDataSubscriber<object>)_dataSubscriber1).DataSource);
                ((MockDataSubscriber<object>)_dataSubscriber1).OnFailureCallCount = 0;
            }

            if (numSubscribers >= 2)
            {
                Assert.IsTrue(((MockDataSubscriber<object>)_dataSubscriber2).OnFailureCallCount == 1);
                Assert.AreSame(
                    _dataSource,
                    ((MockDataSubscriber<object>)_dataSubscriber2).DataSource);
                ((MockDataSubscriber<object>)_dataSubscriber2).OnFailureCallCount = 0;
            }

            VerifyFailed(result, _exception);
        }

        private void TestClose(Exception throwable)
        {
            _dataSource.Close();
            VerifyClosed(FINISHED, throwable);
        }

        private void TestClose(bool isFinished, int numSubscribers)
        {
            _internalRequestId = null;
            _dataSource.Close();
            if (!isFinished)
            {
                Assert.IsTrue(_onRequestCancellationInvocation);
                Assert.AreSame(_internalRequestId, _requestId);
                _onRequestCancellationInvocation = false;

                if (numSubscribers >= 1)
                {
                    Assert.IsTrue(((MockDataSubscriber<object>)_dataSubscriber1).OnCancellationCallCount == 1);
                    Assert.AreSame(
                        _dataSource,
                        ((MockDataSubscriber<object>)_dataSubscriber1).DataSource);
                    ((MockDataSubscriber<object>)_dataSubscriber1).OnCancellationCallCount = 0;
                }

                if (numSubscribers >= 2)
                {
                    Assert.IsTrue(((MockDataSubscriber<object>)_dataSubscriber2).OnCancellationCallCount == 1);
                    Assert.AreSame(
                        _dataSource,
                        ((MockDataSubscriber<object>)_dataSubscriber2).DataSource);
                    ((MockDataSubscriber<object>)_dataSubscriber2).OnCancellationCallCount = 0;
                }
            }

            VerifyClosed(isFinished, null);
        }

        /// <summary>
        /// Tests the initial state
        /// </summary>
        [TestMethod]
        public void TestInitialState()
        {
            VerifyInitial();
        }

        /// <summary>
        /// Tests Close and Subscribe
        /// </summary>
        [TestMethod]
        public void Test_C_a()
        {
            TestClose(NOT_FINISHED, 1);
            TestSubscribe(NO_INTERACTIONS);
        }

        /// <summary>
        /// Tests Close, OnNewResult and Subscribe
        /// </summary>
        [TestMethod]
        public void Test_C_I_a()
        {
            TestClose(NOT_FINISHED, 1);
            _internalConsumer.OnNewResult(_result2, INTERMEDIATE);
            VerifyClosed(NOT_FINISHED, null);
            TestSubscribe(NO_INTERACTIONS);
        }

        /// <summary>
        /// Tests Close, OnNewResult and Subscribe
        /// </summary>
        [TestMethod]
        public void Test_C_L_a()
        {
            TestClose(NOT_FINISHED, 1);
            _internalConsumer.OnNewResult(_result2, LAST);
            VerifyClosed(NOT_FINISHED, null);
            TestSubscribe(NO_INTERACTIONS);
        }

        /// <summary>
        /// Tests Close, OnFailure and Subscribe
        /// </summary>
        [TestMethod]
        public void TestC_F_a()
        {
            TestClose(NOT_FINISHED, 1);
            _internalConsumer.OnFailure(_exception);
            VerifyClosed(NOT_FINISHED, null);
            TestSubscribe(NO_INTERACTIONS);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_I_a_C()
        {
            TestNewResult(_result1, INTERMEDIATE, 1);
            TestSubscribe(ON_NEW_RESULT);
            TestClose(NOT_FINISHED, 2);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_I_I_a_C()
        {
            TestNewResult(_result1, INTERMEDIATE, 1);
            TestNewResult(_result2, INTERMEDIATE, 1);
            TestSubscribe(ON_NEW_RESULT);
            TestClose(NOT_FINISHED, 2);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_I_I_L_a_C()
        {
            TestNewResult(_result1, INTERMEDIATE, 1);
            TestNewResult(_result2, INTERMEDIATE, 1);
            TestNewResult(_result3, LAST, 1);
            TestSubscribe(ON_NEW_RESULT);
            TestClose(FINISHED, 2);
        }

        /// <summary>
        /// Tests NewResult, Failure, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_I_I_F_a_C()
        {
            TestNewResult(_result1, INTERMEDIATE, 1);
            TestNewResult(_result2, INTERMEDIATE, 1);
            TestFailure(_result2, 1);
            TestSubscribe(ON_FAILURE);
            TestClose(_exception);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_I_L_a_C()
        {
            TestNewResult(_result1, INTERMEDIATE, 1);
            TestNewResult(_result2, LAST, 1);
            TestSubscribe(ON_NEW_RESULT);
            TestClose(FINISHED, 2);
        }

        /// <summary>
        /// Tests NewResult, Failure, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_I_F_a_C()
        {
            TestNewResult(_result1, INTERMEDIATE, 1);
            TestFailure(_result1, 1);
            TestSubscribe(ON_FAILURE);
            TestClose(_exception);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_L_a_C()
        {
            TestNewResult(_result1, LAST, 1);
            TestSubscribe(ON_NEW_RESULT);
            TestClose(FINISHED, 2);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_L_I_a_C()
        {
            TestNewResult(_result1, LAST, 1);
            _internalConsumer.OnNewResult(_result2, INTERMEDIATE);
            VerifyWithResult(_result1, LAST);
            TestSubscribe(ON_NEW_RESULT);
            TestClose(FINISHED, 2);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_L_L_a_C()
        {
            TestNewResult(_result1, LAST, 1);
            _internalConsumer.OnNewResult(_result2, LAST);
            VerifyWithResult(_result1, LAST);
            TestSubscribe(ON_NEW_RESULT);
            TestClose(FINISHED, 2);
        }

        /// <summary>
        /// Tests NewResult, Failure, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_L_F_a_C()
        {
            TestNewResult(_result1, LAST, 1);
            _internalConsumer.OnFailure(_exception);
            VerifyWithResult(_result1, LAST);
            TestSubscribe(ON_NEW_RESULT);
            TestClose(FINISHED, 2);
        }

        /// <summary>
        /// Tests Failure, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_F_a_C()
        {
            TestFailure(null, 1);
            TestSubscribe(ON_FAILURE);
            TestClose(_exception);
        }

        /// <summary>
        /// Tests Failure, NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_F_I_a_C()
        {
            TestFailure(null, 1);
            _internalConsumer.OnNewResult(_result1, INTERMEDIATE);
            VerifyFailed(null, _exception);
            TestSubscribe(ON_FAILURE);
            TestClose(_exception);
        }

        /// <summary>
        /// Tests Failure, NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_F_L_a_C()
        {
            TestFailure(null, 1);
            _internalConsumer.OnNewResult(_result1, LAST);
            VerifyFailed(null, _exception);
            TestSubscribe(ON_FAILURE);
            TestClose(_exception);
        }

        /// <summary>
        /// Tests Failure, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_F_F_a_C()
        {
            TestFailure(null, 1);
            _internalConsumer.OnFailure(new Exception());
            VerifyFailed(null, _exception);
            TestSubscribe(ON_FAILURE);
            TestClose(_exception);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_NI_S_a_C()
        {
            _internalConsumer.OnNewResult(null, INTERMEDIATE);
            Assert.IsTrue(((MockDataSubscriber<object>)_dataSubscriber1).OnNewResultCallCount == 1);
            Assert.AreSame(
                _dataSource,
                ((MockDataSubscriber<object>)_dataSubscriber1).DataSource);
            ((MockDataSubscriber<object>)_dataSubscriber1).OnNewResultCallCount = 0;
            VerifyWithResult(null, INTERMEDIATE);

            TestNewResult(_result1, LAST, 1);
            TestSubscribe(ON_NEW_RESULT);
            TestClose(FINISHED, 2);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_NI_a_NL_C()
        {
            _internalConsumer.OnNewResult(null, INTERMEDIATE);
            Assert.IsTrue(((MockDataSubscriber<object>)_dataSubscriber1).OnNewResultCallCount == 1);
            Assert.AreSame(
                _dataSource,
                ((MockDataSubscriber<object>)_dataSubscriber1).DataSource);
            ((MockDataSubscriber<object>)_dataSubscriber1).OnNewResultCallCount = 0;
            VerifyWithResult(null, INTERMEDIATE);

            TestSubscribe(NO_INTERACTIONS);

            _internalImageRequest = null;
            _internalRequestId = null;
            _internalIsPrefetch = true;
            _internalConsumer.OnNewResult(null, LAST);
            Assert.IsTrue(_onRequestSuccessInvocation);
            Assert.AreSame(_internalImageRequest, _imageRequest);
            Assert.AreSame(_internalRequestId, _requestId);
            Assert.IsFalse(_internalIsPrefetch);
            _onRequestSuccessInvocation = false;
            Assert.IsTrue(((MockDataSubscriber<object>)_dataSubscriber1).OnNewResultCallCount == 1);
            Assert.AreSame(
                _dataSource,
                ((MockDataSubscriber<object>)_dataSubscriber1).DataSource);
            ((MockDataSubscriber<object>)_dataSubscriber1).OnNewResultCallCount = 0;
            Assert.IsTrue(((MockDataSubscriber<object>)_dataSubscriber2).OnNewResultCallCount == 1);
            Assert.AreSame(
                _dataSource,
                ((MockDataSubscriber<object>)_dataSubscriber2).DataSource);
            ((MockDataSubscriber<object>)_dataSubscriber2).OnNewResultCallCount = 0;
            VerifyWithResult(null, LAST);

            TestClose(FINISHED, 2);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_I_NL_a_C()
        {
            TestNewResult(_result1, INTERMEDIATE, 1);

            _internalImageRequest = null;
            _internalRequestId = null;
            _internalIsPrefetch = true;
            _internalConsumer.OnNewResult(null, LAST);
            Assert.IsTrue(_onRequestSuccessInvocation);
            Assert.AreSame(_internalImageRequest, _imageRequest);
            Assert.AreSame(_internalRequestId, _requestId);
            Assert.IsFalse(_internalIsPrefetch);
            _onRequestSuccessInvocation = false;
            Assert.IsTrue(((MockDataSubscriber<object>)_dataSubscriber1).OnNewResultCallCount == 1);
            Assert.AreSame(
                _dataSource,
                ((MockDataSubscriber<object>)_dataSubscriber1).DataSource);
            ((MockDataSubscriber<object>)_dataSubscriber1).OnNewResultCallCount = 0;
            VerifyWithResult(null, LAST);

            TestSubscribe(ON_NEW_RESULT);
            TestClose(FINISHED, 2);
        }
    }
}
