using ImagePipeline.Common;
using ImagePipeline.Producers;
using ImagePipeline.Request;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace ImagePipeline.Tests.Producers
{
    /// <summary>
    /// Checks basic properties of NullProducer, that is that it always returns null.
    /// </summary>
    [TestClass]
    public class NullProducerTests
    {
        private readonly ImageRequest IMAGE_REQUEST = ImageRequest.FromUri("http://microsoft.com");
        private const string REQUEST_ID = "RequestId";

        private IConsumer<object> _consumer;
        private IProducerContext _producerContext;
        private IProducerListener _producerListener;
        private NullProducer<object> _nullProducer;
        private int _consumerOnNewResultCount;
        private object _consumerInternalResult;
        private bool _consumerInternalIsLast;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            // Initializes the mock data
            _producerListener = new ProducerListenerImpl(
                (_, __) => { },
                (_, __, ___) => { },
                (_, __, ___) => { },
                (_, __, ___, ____) => { },
                (_, __, ___) => { },
                (_) =>
                {
                    return false;
                });
            _consumer = new BaseConsumerImpl<object>(
                (result, isLast) =>
                {
                    ++_consumerOnNewResultCount;
                    _consumerInternalResult = result;
                    _consumerInternalIsLast = isLast;
                },
                (_) => { },
                () => { },
                (_) => { });
            _producerContext = new SettableProducerContext(
                IMAGE_REQUEST,
                REQUEST_ID,
                _producerListener,
                new object(),
                RequestLevel.FULL_FETCH,
                false,
                true,
                Priority.MEDIUM);

            _nullProducer = new NullProducer<object>();
        }

        /// <summary>
        /// Tests the result from OnNewResult
        /// </summary>
        [TestMethod]
        public void TestNullProducerReturnsNull()
        {
            _nullProducer.ProduceResults(_consumer, _producerContext);
            Assert.IsTrue(_consumerOnNewResultCount == 1);
            Assert.IsNull(_consumerInternalResult);
            Assert.IsTrue(_consumerInternalIsLast);
        }
    }
}
