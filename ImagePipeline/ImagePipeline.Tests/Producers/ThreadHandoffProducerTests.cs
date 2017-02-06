using FBCore.Concurrency;
using ImagePipeline.Common;
using ImagePipeline.Producers;
using ImagePipeline.Request;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;

namespace ImagePipeline.Tests.Producers
{
    /// <summary>
    /// Tests for <see cref="ThreadHandoffProducer{T}"/>
    /// </summary>
    [TestClass]
    public sealed class ThreadHandoffProducerTests : IDisposable
    {
        private readonly ImageRequest IMAGE_REQUEST = ImageRequest.FromUri("http://microsoft.com");
        private const string REQUEST_ID = "RequestId";

        private IProducer<object> _inputProducer;
        private IConsumer<object> _consumer;
        private IProducerListener _producerListener;
        private SettableProducerContext _producerContext;
        private ThreadHandoffProducer<object> _threadHandoffProducer;
        private IExecutorService _testExecutorService;
        private IConsumer<object> _internalConsumer;
        private SettableProducerContext _internalProducerContext;
        private int _onProducerStartCount;
        private int _onProducerEventCount;
        private int _onProducerFinishWithSuccessCount;
        private int _onProducerFinishWithFailureCount;
        private int _onProducerFinishWithCancellationCount;
        private int _requiresExtraMapCount;
        private string _internalRequestId;
        private string _internalProducerName;
        private IDictionary<string, string> _internalExtraMap;
        private int _consumerOnCancellationCount;
        private bool _finishRunning;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            // Initializes mock data
            _inputProducer = new ProducerImpl<object>((consumer, producerContext) => 
            {
                _internalConsumer = consumer;
                _internalProducerContext = (SettableProducerContext)producerContext;
                _finishRunning = true;
            });
            _consumer = new BaseConsumerImpl<object>(
                (_, __) => { },
                (_) => { },
                () =>
                {
                    ++_consumerOnCancellationCount;
                    _finishRunning = true;
                },
                (_) => { });
            _producerListener = new ProducerListenerImpl(
                (requestId, producerName) =>
                {
                    ++_onProducerStartCount;
                    _internalRequestId = requestId;
                    _internalProducerName = producerName;
                },
                (_, __, ___) => 
                {
                    ++_onProducerEventCount;
                },
                (requestId, producerName, extraMap) =>
                {
                    ++_onProducerFinishWithSuccessCount;
                    _internalRequestId = requestId;
                    _internalProducerName = producerName;
                    _internalExtraMap = extraMap;
                },
                (_, __, ___, ____) => 
                {
                    ++_onProducerFinishWithFailureCount;
                    _finishRunning = true;
                },
                (requestId, producerName, extraMap) =>
                {
                    ++_onProducerFinishWithCancellationCount;
                    _internalRequestId = requestId;
                    _internalProducerName = producerName;
                    _internalExtraMap = extraMap;
                },
                (_) =>
                {
                    ++_requiresExtraMapCount;
                    return false;
                });

            _producerContext = new SettableProducerContext(
                IMAGE_REQUEST,
                REQUEST_ID,
                _producerListener,
                new object(),
                RequestLevel.FULL_FETCH,
                false,
                true,
                Priority.MEDIUM);

            _testExecutorService = new MockSerialExecutorService();
            _threadHandoffProducer = new ThreadHandoffProducer<object>(
                _inputProducer,
                new ThreadHandoffProducerQueue(_testExecutorService));
            _finishRunning = false;
        }

        /// <summary>
        /// Test cleanup.
        /// </summary>
        public void Dispose()
        {
            ((MockSerialExecutorService)_testExecutorService).Dispose();
        }

        /// <summary>
        /// Tests out the successful case
        /// </summary>
        [TestMethod]
        public void TestSuccess()
        {
            _threadHandoffProducer.ProduceResults(_consumer, _producerContext);

            // Wait until finish
            while (!_finishRunning);

            Assert.AreSame(_internalConsumer, _consumer);
            Assert.AreSame(_internalProducerContext, _producerContext);
            Assert.IsTrue(_onProducerStartCount == 1);
            Assert.IsTrue(_onProducerFinishWithSuccessCount == 1);
            Assert.AreEqual(_internalRequestId, REQUEST_ID);
            Assert.AreEqual(_internalProducerName, ThreadHandoffProducer<object>.PRODUCER_NAME);
            Assert.IsNull(_internalExtraMap);
            Assert.IsTrue(_onProducerEventCount == 0);
            Assert.IsTrue(_onProducerFinishWithFailureCount == 0);
            Assert.IsTrue(_onProducerFinishWithCancellationCount == 0);
            Assert.IsTrue(_requiresExtraMapCount == 0);
        }

        /// <summary>
        /// Tests out the cancelled case
        /// </summary>
        [TestMethod]
        public void TestCancellation()
        {
            _threadHandoffProducer.ProduceResults(_consumer, _producerContext);
            _producerContext.Cancel();

            // Wait until finish
            while (!_finishRunning);

            Assert.IsNull(_internalConsumer);
            Assert.IsNull(_internalProducerContext);
            Assert.IsTrue(_consumerOnCancellationCount == 1);
            Assert.IsTrue(_onProducerStartCount == 1);
            Assert.IsTrue(_requiresExtraMapCount == 1);
            Assert.IsTrue(_onProducerFinishWithCancellationCount == 1);
            Assert.AreEqual(_internalRequestId, REQUEST_ID);
            Assert.AreEqual(_internalProducerName, ThreadHandoffProducer<object>.PRODUCER_NAME);
            Assert.IsNull(_internalExtraMap);
            Assert.IsTrue(_onProducerEventCount == 0);
            Assert.IsTrue(_onProducerFinishWithSuccessCount == 0);
            Assert.IsTrue(_onProducerFinishWithFailureCount == 0);
        }
    }
}
