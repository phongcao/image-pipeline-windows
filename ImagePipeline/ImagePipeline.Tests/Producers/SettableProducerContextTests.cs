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
    public class SettableProducerContextTests
    {
        private readonly ImageRequest IMAGE_REQUEST = ImageRequest.FromUri("http://microsoft.com");
        private readonly string REQUEST_ID = "RequestId";

        private IProducerContextCallbacks _callbacks1;
        private IProducerContextCallbacks _callbacks2;
        private SettableProducerContext _settableProducerContext;
        private int _onCancellationRequestedCount1;
        private int _onIsPrefetchChangedCount1;
        private int _onIsIntermediateResultExpectedChangedCount1;
        private int _onPriorityChangedCount1;
        private int _onCancellationRequestedCount2;
        private int _onIsPrefetchChangedCount2;
        private int _onIsIntermediateResultExpectedChangedCount2;
        private int _onPriorityChangedCount2;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            // Initializes the mock producer listener
            ProducerListenerImpl producerListener = new ProducerListenerImpl(
                (_, __) => {},
                (_, __, ___) => {},
                (_, __, ___) => {},
                (_, __, ___, ____) => {},
                (_, __, ___) => {},
                (_) => 
                {
                    return false;
                });

            // Initializes the mock producer context callbacks
            _callbacks1 = new BaseProducerContextCallbacks(
                () => 
                {
                    ++_onCancellationRequestedCount1;
                },
                () => 
                {
                    ++_onIsPrefetchChangedCount1;
                },
                () => 
                {
                    ++_onIsIntermediateResultExpectedChangedCount1;
                },
                () => 
                {
                    ++_onPriorityChangedCount1;
                });
            _callbacks2 = new BaseProducerContextCallbacks(
                () => 
                {
                    ++_onCancellationRequestedCount2;
                },
                () => 
                {
                    ++_onIsPrefetchChangedCount2;
                },
                () => 
                {
                    ++_onIsIntermediateResultExpectedChangedCount2;
                },
                () => 
                {
                    ++_onPriorityChangedCount2;
                });

            _settableProducerContext = new SettableProducerContext(
                IMAGE_REQUEST,
                REQUEST_ID,
                producerListener,
                new object(),
                RequestLevel.FULL_FETCH,
                false,
                true,
                Priority.MEDIUM);

            _onCancellationRequestedCount1 = 0;
            _onIsPrefetchChangedCount1 = 0;
            _onIsIntermediateResultExpectedChangedCount1 = 0;
            _onPriorityChangedCount1 = 0;
            _onCancellationRequestedCount2 = 0;
            _onIsPrefetchChangedCount2 = 0;
            _onIsIntermediateResultExpectedChangedCount2 = 0;
            _onPriorityChangedCount2 = 0;
        }

        /// <summary>
        /// Tests out the getter
        /// </summary>
        [TestMethod]
        public void TestGetters()
        {
            Assert.AreEqual(IMAGE_REQUEST, _settableProducerContext.ImageRequest);
            Assert.AreEqual(REQUEST_ID, _settableProducerContext.Id);
        }

        /// <summary>
        /// Tests out the prefetch
        /// </summary>
        [TestMethod]
        public void TestIsPrefetch()
        {
            Assert.IsFalse(_settableProducerContext.IsPrefetch);
        }

        /// <summary>
        /// Tests out the cancellation
        /// </summary>
        [TestMethod]
        public void TestCancellation()
        {
            _settableProducerContext.AddCallbacks(_callbacks1);
            Assert.IsTrue(_onCancellationRequestedCount1 == 0);
            _settableProducerContext.Cancel();
            Assert.IsTrue(_onCancellationRequestedCount1 == 1);
            Assert.IsTrue(_onIsPrefetchChangedCount1 == 0);

            _settableProducerContext.AddCallbacks(_callbacks2);
            Assert.IsTrue(_onCancellationRequestedCount2 == 1);
            Assert.IsTrue(_onIsPrefetchChangedCount2 == 0);
        }

        /// <summary>
        /// Tests setting prefetch
        /// </summary>
        [TestMethod]
        public void TestSetPrefetch()
        {
            _settableProducerContext.AddCallbacks(_callbacks1);
            Assert.IsFalse(_settableProducerContext.IsPrefetch);
            _settableProducerContext.SetIsPrefetch(true);
            Assert.IsTrue(_settableProducerContext.IsPrefetch);
            Assert.IsTrue(_onIsPrefetchChangedCount1 == 1);
            _settableProducerContext.SetIsPrefetch(true);

            // only one callback is expected
            Assert.IsTrue(_onIsPrefetchChangedCount1 == 1);
        }

        /// <summary>
        /// Tests setting the intermediate result
        /// </summary>
        [TestMethod]
        public void TestSetIsIntermediateResultExpected()
        {
            _settableProducerContext.AddCallbacks(_callbacks1);
            Assert.IsTrue(_settableProducerContext.IsIntermediateResultExpected);
            _settableProducerContext.SetIsIntermediateResultExpected(false);
            Assert.IsFalse(_settableProducerContext.IsIntermediateResultExpected);
            Assert.IsTrue(_onIsIntermediateResultExpectedChangedCount1 == 1);
            _settableProducerContext.SetIsIntermediateResultExpected(false);

            // only one callback is expected
            Assert.IsTrue(_onIsIntermediateResultExpectedChangedCount1 == 1);
        }

        /// <summary>
        /// Tests out the cancellation
        /// </summary>
        [TestMethod]
        public void TestNoCallbackCalledWhenIsPrefetchDoesNotChange()
        {
            Assert.IsFalse(_settableProducerContext.IsPrefetch);
            _settableProducerContext.AddCallbacks(_callbacks1);
            _settableProducerContext.SetIsPrefetch(false);
            Assert.IsTrue(_onIsPrefetchChangedCount1 == 0);
        }

        /// <summary>
        /// Tests out the cancellation
        /// </summary>
        [TestMethod]
        public void TestCallbackCalledWhenIsPrefetchChanges()
        {
            Assert.IsFalse(_settableProducerContext.IsPrefetch);
            _settableProducerContext.AddCallbacks(_callbacks1);
            _settableProducerContext.AddCallbacks(_callbacks2);
            _settableProducerContext.SetIsPrefetch(true);
            Assert.IsTrue(_settableProducerContext.IsPrefetch);
            Assert.IsTrue(_onIsPrefetchChangedCount1 == 1);
            Assert.IsTrue(_onCancellationRequestedCount1 == 0);
            Assert.IsTrue(_onIsPrefetchChangedCount2 == 1);
            Assert.IsTrue(_onCancellationRequestedCount2 == 0);
        }
    }
}
