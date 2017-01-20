using FBCore.Common.References;
using FBCore.Concurrency;
using FBCore.DataSource;
using ImagePipeline.Common;
using ImagePipeline.Datasource;
using ImagePipeline.Listener;
using ImagePipeline.Memory;
using ImagePipeline.Producers;
using ImagePipeline.Request;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;

namespace ImagePipeline.Tests.Datasource
{
    /// <summary>
    /// Tests for <see cref="CloseableProducerToDataSourceAdapter{T}"/>
    /// </summary>
    [TestClass]
    public class CloseableProducerToDataSourceAdapterTests
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

        private readonly Exception NPE = new NullReferenceException();
        private readonly string REQUEST_ID = "requestId";
        private readonly ImageRequest IMAGE_REQUEST = ImageRequest.FromUri("http://microsoft.com");
        private readonly object CALLER_CONTEXT = new object();
        private readonly bool IS_PREFETCH = false;
        private readonly Exception EXCEPTION = new Exception();

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

        private IResourceReleaser<object> _resourceReleaser;
        private CloseableReference<object> _resultRef1;
        private CloseableReference<object> _resultRef2;
        private CloseableReference<object> _resultRef3;

        private IDataSubscriber<CloseableReference<object>> _dataSubscriber1;
        private IDataSubscriber<CloseableReference<object>> _dataSubscriber2;

        private SettableProducerContext _settableProducerContext;
        private IProducer<CloseableReference<object>> _producer;
        private IConsumer<CloseableReference<object>> _internalConsumer;

        private IDataSource<CloseableReference<object>> _dataSource;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            // Initializes the mock RequestListener
            ProducerListenerImpl producerListener = new ProducerListenerImpl(
                (_, __) => {},
                (_, __, ___) => {},
                (_, __, ___) => {},
                (_, __, ___, ____) => {},
                (_, __, ___) => {},
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
            _resourceReleaser = new ResourceReleaserImpl<object>(_ => {});
            _resultRef1 = CloseableReference<object>.of(new object(), _resourceReleaser);
            _resultRef2 = CloseableReference<object>.of(new object(), _resourceReleaser);
            _resultRef3 = CloseableReference<object>.of(new object(), _resourceReleaser);

            _dataSubscriber1 = new MockDataSubscriber<CloseableReference<object>>();
            _dataSubscriber2 = new MockDataSubscriber<CloseableReference<object>>();

            _internalIsPrefetch = true;
            _settableProducerContext = new SettableProducerContext(
                IMAGE_REQUEST,
                REQUEST_ID,
                producerListener,
                CALLER_CONTEXT,
                RequestLevel.FULL_FETCH,
                IS_PREFETCH,
                true,
                Priority.HIGH);
            _producer = new ProducerImpl<CloseableReference<object>>(
                (consumer, _) =>
                {
                    _internalConsumer = consumer;
                });
            _dataSource = CloseableProducerToDataSourceAdapter<object>.Create(
                _producer,
                _settableProducerContext,
                _requestListener);

            Assert.IsTrue(_onRequestStartInvocation);
            Assert.AreSame(_internalImageRequest, IMAGE_REQUEST);
            Assert.AreSame(_internalCallerContext, CALLER_CONTEXT);
            Assert.AreSame(_internalRequestId, REQUEST_ID);
            Assert.IsFalse(_internalIsPrefetch);
            Assert.IsNotNull(_internalConsumer);
            _onRequestStartInvocation = false;

            _dataSource.Subscribe(_dataSubscriber1, CallerThreadExecutor.Instance);
        }

        /// <summary>
        /// Reference assertions
        /// </summary>
        private void AssertReferenceCount<T>(int expectedCount, CloseableReference<T> reference)
        {
            Assert.IsTrue(expectedCount == reference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
        }

        private void AssertReferencesSame<T>(
            string errorMessage,
            CloseableReference<T> expectedRef,
            CloseableReference<T> actualRef)
        {
            if (expectedRef == null)
            {
                Assert.IsNull(actualRef, errorMessage);
            }
            else
            {
                Assert.AreSame(expectedRef.Get(), actualRef.Get(), errorMessage);
            }
        }

        /// <summary>
        /// Verification helpers
        /// </summary>
        private void VerifyState(
            bool isFinished,
            bool hasResult,
            CloseableReference<object> resultRef,
            bool hasFailed,
            Exception failureCause)
        {
            IDataSource<CloseableReference<object>> dataSource = _dataSource;
            Assert.IsTrue(isFinished == dataSource.IsFinished(), "isFinished");
            Assert.IsTrue(hasResult == dataSource.HasResult(), "hasResult");
            CloseableReference<object> dataSourceRef = dataSource.GetResult();
            AssertReferencesSame("getResult", resultRef, dataSourceRef);
            CloseableReference<object>.CloseSafely(dataSourceRef);
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

        private void VerifyReferenceCount(CloseableReference<object> resultRef)
        {
            // this unit test class keeps references alive, so their ref count must be 1;
            // except for the result which have ref count of 2 because it's also kept by data source
            AssertReferenceCount((resultRef == _resultRef1) ? 2 : 1, _resultRef1);
            AssertReferenceCount((resultRef == _resultRef2) ? 2 : 1, _resultRef2);
            AssertReferenceCount((resultRef == _resultRef3) ? 2 : 1, _resultRef3);
        }

        private void VerifyNoMoreInteractionsAndReset()
        {
            // Verify
            Assert.IsFalse(_onRequestStartInvocation);
            Assert.IsFalse(_onRequestSuccessInvocation);
            Assert.IsFalse(_onRequestFailureInvocation);
            Assert.IsFalse(_onRequestCancellationInvocation);
            Assert.IsTrue(((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).HasZeroInteractions);
            Assert.IsTrue(((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).HasZeroInteractions);

            // Reset
            _internalImageRequest = null;
            _internalCallerContext = null;
            _internalRequestId = null;
            _internalIsPrefetch = true;
            _internalException = null;
            ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).Reset();
            ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).Reset();
        }

        /// <summary>
        /// State verification methods
        /// </summary>
        private void VerifyInitial()
        {
            VerifyState(NOT_FINISHED, WITHOUT_RESULT, null, NOT_FAILED, null);
            VerifyReferenceCount(null);
            VerifyNoMoreInteractionsAndReset();
        }

        private void VerifyWithResult(CloseableReference<object> resultRef, bool isLast)
        {
            VerifyState(isLast, resultRef != null, resultRef, NOT_FAILED, null);
            VerifyReferenceCount(resultRef);
            VerifyNoMoreInteractionsAndReset();
        }

        private void VerifyFailed(CloseableReference<object> resultRef, Exception throwable)
        {
            VerifyState(FINISHED, resultRef != null, resultRef, FAILED, throwable);
            VerifyReferenceCount(resultRef);
            VerifyNoMoreInteractionsAndReset();
        }

        private void VerifyClosed(bool isFinished, Exception throwable)
        {
            VerifyState(isFinished, WITHOUT_RESULT, null, throwable != null, throwable);
            VerifyReferenceCount(null);
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
                    Assert.IsTrue(((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).OnNewResultCallCount == 1);
                    Assert.AreSame(
                        _dataSource,
                        ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).DataSource);
                    ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).OnNewResultCallCount = 0;
                    break;

                case ON_FAILURE:
                    Assert.IsTrue(((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).OnFailureCallCount == 1);
                    Assert.AreSame(
                        _dataSource,
                        ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).DataSource);
                    ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).OnFailureCallCount = 0;
                    break;
            }

            VerifyNoMoreInteractionsAndReset();
        }

        private void TestNewResult(
            CloseableReference<object> resultRef,
            bool isLast,
            int numSubscribers)
        {
            _internalImageRequest = null;
            _internalRequestId = null;
            _internalIsPrefetch = true;
            _internalConsumer.OnNewResult(resultRef, isLast);
            if (isLast)
            {
                Assert.IsTrue(_onRequestSuccessInvocation);
                Assert.AreSame(_internalImageRequest, IMAGE_REQUEST);
                Assert.AreSame(_internalRequestId, REQUEST_ID);
                Assert.IsFalse(_internalIsPrefetch);
                _onRequestSuccessInvocation = false;
            }

            if (numSubscribers >= 1)
            {
                Assert.IsTrue(((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).OnNewResultCallCount == 1);
                Assert.AreSame(
                    _dataSource,
                    ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).DataSource);
                ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).OnNewResultCallCount = 0;
            }

            if (numSubscribers >= 2)
            {
                Assert.IsTrue(((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).OnNewResultCallCount == 1);
                Assert.AreSame(
                    _dataSource,
                    ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).DataSource);
                ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).OnNewResultCallCount = 0;
            }

            VerifyWithResult(resultRef, isLast);
        }

        private void TestFailure(
            CloseableReference<object> resultRef,
            int numSubscribers)
        {
            _internalImageRequest = null;
            _internalRequestId = null;
            _internalException = null;
            _internalIsPrefetch = true;
            _internalConsumer.OnFailure(EXCEPTION);
            Assert.IsTrue(_onRequestFailureInvocation);
            Assert.AreSame(_internalImageRequest, IMAGE_REQUEST);
            Assert.AreSame(_internalRequestId, REQUEST_ID);
            Assert.AreSame(_internalException, EXCEPTION);
            Assert.IsFalse(_internalIsPrefetch);
            _onRequestFailureInvocation = false;

            if (numSubscribers >= 1)
            {
                Assert.IsTrue(((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).OnFailureCallCount == 1);
                Assert.AreSame(
                    _dataSource,
                    ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).DataSource);
                ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).OnFailureCallCount = 0;
            }

            if (numSubscribers >= 2)
            {
                Assert.IsTrue(((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).OnFailureCallCount == 1);
                Assert.AreSame(
                    _dataSource,
                    ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).DataSource);
                ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).OnFailureCallCount = 0;
            }

            VerifyFailed(resultRef, EXCEPTION);
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
                Assert.AreSame(_internalRequestId, REQUEST_ID);
                _onRequestCancellationInvocation = false;

                if (numSubscribers >= 1)
                {
                    Assert.IsTrue(((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).OnCancellationCallCount == 1);
                    Assert.AreSame(
                        _dataSource,
                        ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).DataSource);
                    ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).OnCancellationCallCount = 0;
                }

                if (numSubscribers >= 2)
                {
                    Assert.IsTrue(((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).OnCancellationCallCount == 1);
                    Assert.AreSame(
                        _dataSource,
                        ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).DataSource);
                    ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).OnCancellationCallCount = 0;
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
            _internalConsumer.OnNewResult(_resultRef2, INTERMEDIATE);
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
            _internalConsumer.OnNewResult(_resultRef2, LAST);
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
            _internalConsumer.OnFailure(EXCEPTION);
            VerifyClosed(NOT_FINISHED, null);
            TestSubscribe(NO_INTERACTIONS);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_I_a_C()
        {
            TestNewResult(_resultRef1, INTERMEDIATE, 1);
            TestSubscribe(ON_NEW_RESULT);
            TestClose(NOT_FINISHED, 2);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_I_I_a_C()
        {
            TestNewResult(_resultRef1, INTERMEDIATE, 1);
            TestNewResult(_resultRef2, INTERMEDIATE, 1);
            TestSubscribe(ON_NEW_RESULT);
            TestClose(NOT_FINISHED, 2);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_I_I_L_a_C()
        {
            TestNewResult(_resultRef1, INTERMEDIATE, 1);
            TestNewResult(_resultRef2, INTERMEDIATE, 1);
            TestNewResult(_resultRef3, LAST, 1);
            TestSubscribe(ON_NEW_RESULT);
            TestClose(FINISHED, 2);
        }

        /// <summary>
        /// Tests NewResult, Failure, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_I_I_F_a_C()
        {
            TestNewResult(_resultRef1, INTERMEDIATE, 1);
            TestNewResult(_resultRef2, INTERMEDIATE, 1);
            TestFailure(_resultRef2, 1);
            TestSubscribe(ON_FAILURE);
            TestClose(EXCEPTION);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_I_L_a_C()
        {
            TestNewResult(_resultRef1, INTERMEDIATE, 1);
            TestNewResult(_resultRef2, LAST, 1);
            TestSubscribe(ON_NEW_RESULT);
            TestClose(FINISHED, 2);
        }

        /// <summary>
        /// Tests NewResult, Failure, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_I_F_a_C()
        {
            TestNewResult(_resultRef1, INTERMEDIATE, 1);
            TestFailure(_resultRef1, 1);
            TestSubscribe(ON_FAILURE);
            TestClose(EXCEPTION);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_L_a_C()
        {
            TestNewResult(_resultRef1, LAST, 1);
            TestSubscribe(ON_NEW_RESULT);
            TestClose(FINISHED, 2);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_L_I_a_C()
        {
            TestNewResult(_resultRef1, LAST, 1);
            _internalConsumer.OnNewResult(_resultRef2, INTERMEDIATE);
            VerifyWithResult(_resultRef1, LAST);
            TestSubscribe(ON_NEW_RESULT);
            TestClose(FINISHED, 2);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_L_L_a_C()
        {
            TestNewResult(_resultRef1, LAST, 1);
            _internalConsumer.OnNewResult(_resultRef2, LAST);
            VerifyWithResult(_resultRef1, LAST);
            TestSubscribe(ON_NEW_RESULT);
            TestClose(FINISHED, 2);
        }

        /// <summary>
        /// Tests NewResult, Failure, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_L_F_a_C()
        {
            TestNewResult(_resultRef1, LAST, 1);
            _internalConsumer.OnFailure(EXCEPTION);
            VerifyWithResult(_resultRef1, LAST);
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
            TestClose(EXCEPTION);
        }

        /// <summary>
        /// Tests Failure, NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_F_I_a_C()
        {
            TestFailure(null, 1);
            _internalConsumer.OnNewResult(_resultRef1, INTERMEDIATE);
            VerifyFailed(null, EXCEPTION);
            TestSubscribe(ON_FAILURE);
            TestClose(EXCEPTION);
        }

        /// <summary>
        /// Tests Failure, NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_F_L_a_C()
        {
            TestFailure(null, 1);
            _internalConsumer.OnNewResult(_resultRef1, LAST);
            VerifyFailed(null, EXCEPTION);
            TestSubscribe(ON_FAILURE);
            TestClose(EXCEPTION);
        }

        /// <summary>
        /// Tests Failure, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_F_F_a_C()
        {
            TestFailure(null, 1);
            _internalConsumer.OnFailure(new Exception());
            VerifyFailed(null, EXCEPTION);
            TestSubscribe(ON_FAILURE);
            TestClose(EXCEPTION);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_NI_S_a_C()
        {
            _internalConsumer.OnNewResult(null, INTERMEDIATE);
            Assert.IsTrue(((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).OnNewResultCallCount == 1);
            Assert.AreSame(
                _dataSource,
                ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).DataSource);
            ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).OnNewResultCallCount = 0;
            VerifyWithResult(null, INTERMEDIATE);

            TestNewResult(_resultRef1, LAST, 1);
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
            Assert.IsTrue(((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).OnNewResultCallCount == 1);
            Assert.AreSame(
                _dataSource,
                ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).DataSource);
            ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).OnNewResultCallCount = 0;
            VerifyWithResult(null, INTERMEDIATE);

            TestSubscribe(NO_INTERACTIONS);

            _internalImageRequest = null;
            _internalRequestId = null;
            _internalIsPrefetch = true;
            _internalConsumer.OnNewResult(null, LAST);
            Assert.IsTrue(_onRequestSuccessInvocation);
            Assert.AreSame(_internalImageRequest, IMAGE_REQUEST);
            Assert.AreSame(_internalRequestId, REQUEST_ID);
            Assert.IsFalse(_internalIsPrefetch);
            _onRequestSuccessInvocation = false;
            Assert.IsTrue(((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).OnNewResultCallCount == 1);
            Assert.AreSame(
                _dataSource,
                ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).DataSource);
            ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).OnNewResultCallCount = 0;
            Assert.IsTrue(((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).OnNewResultCallCount == 1);
            Assert.AreSame(
                _dataSource,
                ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).DataSource);
            ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber2).OnNewResultCallCount = 0;
            VerifyWithResult(null, LAST);

            TestClose(FINISHED, 2);
        }

        /// <summary>
        /// Tests NewResult, Subscribe and Close
        /// </summary>
        [TestMethod]
        public void Test_I_NL_a_C()
        {
            TestNewResult(_resultRef1, INTERMEDIATE, 1);

            _internalImageRequest = null;
            _internalRequestId = null;
            _internalIsPrefetch = true;
            _internalConsumer.OnNewResult(null, LAST);
            Assert.IsTrue(_onRequestSuccessInvocation);
            Assert.AreSame(_internalImageRequest, IMAGE_REQUEST);
            Assert.AreSame(_internalRequestId, REQUEST_ID);
            Assert.IsFalse(_internalIsPrefetch);
            _onRequestSuccessInvocation = false;
            Assert.IsTrue(((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).OnNewResultCallCount == 1);
            Assert.AreSame(
                _dataSource,
                ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).DataSource);
            ((MockDataSubscriber<CloseableReference<object>>)_dataSubscriber1).OnNewResultCallCount = 0;
            VerifyWithResult(null, LAST);

            TestSubscribe(ON_NEW_RESULT);
            TestClose(FINISHED, 2);
        }
    }
}
