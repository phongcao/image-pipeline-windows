using FBCore.Concurrency;
using ImagePipeline.Common;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using ImagePipeline.Producers;
using ImagePipeline.Request;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ImagePipeline.Tests.Producers
{
    /// <summary>
    /// Tests for <see cref="NetworkFetchProducer"/>
    /// </summary>
    [TestClass]
    public sealed class NetworkFetchProducerTests : IDisposable
    {
        private readonly Uri IMAGE_URL = new Uri("http://i.imgur.com/9rkjHkK.jpg");
        private readonly Uri FAILURE_URL = new Uri("https://httpbin.org/image_not_found.png");
        private const int MAX_DEGREE_OF_PARALLELISM = 10;

        private static INetworkFetcher<FetchState> _networkFetcher;

        private IExecutorService _testExecutor;
        private PoolFactory _poolFactory;
        private IPooledByteBufferFactory _pooledByteBufferFactory;
        private IByteArrayPool _byteArrayPool;
        private ImageRequest _imageRequest;
        private IProducerListener _producerListener;
        private IConsumer<EncodedImage> _consumer;
        private NetworkFetchProducer _networkFetchProducer;
        private SettableProducerContext _producerContext;
        private ManualResetEvent _completion;
        private int _onProducerFinishWithSuccessFuncCalls;
        private int _onProducerFinishWithFailureFuncCalls;
        private int _onProducerFinishWithCancellationFuncCalls;
        private int _intermediateResultProducerEventCalls;
        private int _onNewResultImplCalls;
        private string _internalRequestId;
        private string _internalProducerName;
        private Exception _internalError;
        private IDictionary<string, string> _internalExtraMap;

        /// <summary>
        /// Global Initialize
        /// </summary>
        [ClassInitialize]
        public static void GlobalInitialize(TestContext testContext)
        {
            _networkFetcher = new HttpUrlConnectionNetworkFetcher();
        }

        /// <summary>
        /// Test cleanup.
        /// </summary>
        public void Dispose()
        {
            if (_completion != null)
            {
                _completion.Dispose();
            }
        }

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _testExecutor = Executors.NewFixedThreadPool(MAX_DEGREE_OF_PARALLELISM);
            _poolFactory = new PoolFactory(PoolConfig.NewBuilder().Build());
            _pooledByteBufferFactory = _poolFactory.PooledByteBufferFactory;
            _byteArrayPool = _poolFactory.SmallByteArrayPool;
            _producerListener = new ProducerListenerImpl(
                (_, __) => { },
                (requestId, producerName, eventName) => 
                {
                    if (eventName.Equals(NetworkFetchProducer.INTERMEDIATE_RESULT_PRODUCER_EVENT))
                    {
                        ++_intermediateResultProducerEventCalls;
                    }
                },
                (_, __, ___) =>
                {
                    ++_onProducerFinishWithSuccessFuncCalls;
                },
                (requestId, producerName, error, extraMap) =>
                {
                    _internalRequestId = requestId;
                    _internalProducerName = producerName;
                    _internalError = error;
                    _internalExtraMap = extraMap;
                    ++_onProducerFinishWithFailureFuncCalls;
                },
                (_, __, ___) =>
                {
                    ++_onProducerFinishWithCancellationFuncCalls;
                },
                (_) =>
                {
                    return false;
                });

            _consumer = new BaseConsumerImpl<EncodedImage>(
                (_, isLast) =>
                {
                    ++_onNewResultImplCalls;

                    if (isLast)
                    {
                        _completion.Set();
                    }
                },
                (_) =>
                {
                    _completion.Set();
                },
                () =>
                {
                    _completion.Set();
                },
                (_) => { });

            _networkFetchProducer = new NetworkFetchProducer(
                _pooledByteBufferFactory,
                _byteArrayPool,
                _networkFetcher);

            _completion = new ManualResetEvent(false);
        }

        /// <summary>
        /// Tests out exception in fetching image
        /// </summary>
        [TestMethod, Timeout(3000)]
        public void TestExceptionInFetchImage()
        {
            _imageRequest = ImageRequestBuilder
                .NewBuilderWithSource(FAILURE_URL)
                .SetProgressiveRenderingEnabled(true)
                .Build();
            _producerContext = new SettableProducerContext(
                _imageRequest,
                _imageRequest.SourceUri.ToString(),
                _producerListener,
                new object(),
                RequestLevel.FULL_FETCH,
                false,
                true,
                Priority.MEDIUM);
            _networkFetchProducer.ProduceResults(_consumer, _producerContext);

            // Wait for callback
            _completion.WaitOne();

            Assert.IsTrue(_onProducerFinishWithFailureFuncCalls == 1);
            Assert.AreEqual(_internalRequestId, _imageRequest.SourceUri.ToString());
            Assert.AreEqual(_internalProducerName, NetworkFetchProducer.PRODUCER_NAME);
            Assert.IsNotNull(_internalError);
            Assert.IsNull(_internalExtraMap);
        }

        /// <summary>
        /// Tests out no intermediate results
        /// </summary>
        [TestMethod, Timeout(5000)]
        public void TestNoIntermediateResults()
        {
            _imageRequest = ImageRequestBuilder
                .NewBuilderWithSource(IMAGE_URL)
                .SetProgressiveRenderingEnabled(false)
                .Build();
            _producerContext = new SettableProducerContext(
                _imageRequest,
                $"{ _imageRequest.SourceUri.ToString() }1",
                _producerListener,
                new object(),
                RequestLevel.FULL_FETCH,
                false,
                true,
                Priority.MEDIUM);
            _networkFetchProducer.ProduceResults(_consumer, _producerContext);

            // Wait for callback
            _completion.WaitOne();

            Assert.IsTrue(_onNewResultImplCalls == 1);
            Assert.IsTrue(_intermediateResultProducerEventCalls == 0);
        }

        /// <summary>
        /// Tests out the download handler
        /// </summary>
        [TestMethod, Timeout(5000)]
        public void TestDownloadHandler()
        {
            _imageRequest = ImageRequestBuilder
                .NewBuilderWithSource(IMAGE_URL)
                .SetProgressiveRenderingEnabled(true)
                .Build();
            _producerContext = new SettableProducerContext(
                _imageRequest,
                $"{ _imageRequest.SourceUri.ToString() }2",
                _producerListener,
                new object(),
                RequestLevel.FULL_FETCH,
                false,
                true,
                Priority.MEDIUM);
            _networkFetchProducer.ProduceResults(_consumer, _producerContext);

            // Wait for callback
            _completion.WaitOne();

            Assert.IsTrue(_onNewResultImplCalls > 1);
            Assert.IsTrue(_intermediateResultProducerEventCalls > 1);
        }
    }
}
