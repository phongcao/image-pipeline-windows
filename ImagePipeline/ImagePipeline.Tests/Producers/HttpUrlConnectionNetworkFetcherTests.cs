using ImagePipeline.Common;
using ImagePipeline.Image;
using ImagePipeline.Producers;
using ImagePipeline.Request;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.IO;
using System.Threading;

namespace ImagePipeline.Tests.Producers
{
    /// <summary>
    /// Tests for <see cref="HttpUrlConnectionNetworkFetcher"/>
    /// </summary>
    [TestClass]
    public class HttpUrlConnectionNetworkFetcherTests
    {
        private const string SUCCESS_URL = "https://httpbin.org/image/png";
        private const string FAILURE_URL = "https://httpbin.org/image_not_found.png";
        private const string SUCCESS_REDIRECT_URL = "http://httpbin.org/redirect-to?url=https%3A%2F%2Fwww.microsoft.com%2F";
        private const string FAILURE_REDIRECT_URL = "http://httpbin.org/redirect-to?url=http%3A%2F%2Fhttpbin.org%2F";

        private static HttpUrlConnectionNetworkFetcher _fetcher;

        private FetchState _fetchState;
        private IConsumer<EncodedImage> _consumer;
        private IProducerListener _producerListener;
        private IProducerContext _producerContext;

        /// <summary>
        /// Global Initialize
        /// </summary>
        [ClassInitialize]
        public static void GlobalInitialize(TestContext testContext)
        {
            _fetcher = new HttpUrlConnectionNetworkFetcher();
        }

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
            _consumer = new BaseConsumerImpl<EncodedImage>(
                (_, __) => { },
                (_) => { },
                () => { },
                (_) => { });
        }

        /// <summary>
        /// Tests out the callback with success
        /// </summary>
        [TestMethod, Timeout(5000)]
        public void TestFetchSendsSuccessToCallback()
        {
            ManualResetEvent completion = new ManualResetEvent(false);
            bool failed = false;
            _producerContext = new SettableProducerContext(
                ImageRequest.FromUri(SUCCESS_URL),
                SUCCESS_URL,
                _producerListener,
                new object(),
                RequestLevel.FULL_FETCH,
                false,
                true,
                Priority.MEDIUM);
            _fetchState = new FetchState(_consumer, _producerContext);
            _fetcher.Fetch(
                _fetchState, 
                new NetworkFetcherCallbackImpl(
                    (response, responseLength) =>
                    {
                        Assert.IsTrue(response != null);
                        Assert.IsTrue(responseLength == -1);
                        completion.Set();
                    },
                    (throwable) =>
                    {
                        failed = true;
                        completion.Set();
                    },
                    () =>
                    {
                        failed = true;
                        completion.Set();
                    }));

            // Wait for callback
            completion.WaitOne();
            Assert.IsFalse(failed);
        }

        /// <summary>
        /// Tests out the callback with failure
        /// </summary>
        [TestMethod, Timeout(3000)]
        public void TestFetchSendsErrorToCallbackAfterHttpError()
        {
            ManualResetEvent completion = new ManualResetEvent(false);
            bool failed = false;
            _producerContext = new SettableProducerContext(
                ImageRequest.FromUri(FAILURE_URL),
                FAILURE_URL,
                _producerListener,
                new object(),
                RequestLevel.FULL_FETCH,
                false,
                true,
                Priority.MEDIUM);
            _fetchState = new FetchState(_consumer, _producerContext);
            _fetcher.Fetch(
                _fetchState,
                new NetworkFetcherCallbackImpl(
                    (response, responseLength) =>
                    {
                        failed = true;
                        completion.Set();
                    },
                    (throwable) =>
                    {
                        Assert.IsTrue(throwable.GetType() == typeof(IOException));
                        completion.Set();
                    },
                    () =>
                    {
                        failed = true;
                        completion.Set();
                    }));

            // Wait for callback
            completion.WaitOne();
            Assert.IsFalse(failed);
        }

        /// <summary>
        /// Tests out the callback with successful redirection
        /// </summary>
        [TestMethod, Timeout(3000)]
        public void TestFetchSendsSuccessToCallbackAfterRedirect()
        {
            ManualResetEvent completion = new ManualResetEvent(false);
            bool failed = false;
            _producerContext = new SettableProducerContext(
                ImageRequest.FromUri(SUCCESS_REDIRECT_URL),
                SUCCESS_REDIRECT_URL,
                _producerListener,
                new object(),
                RequestLevel.FULL_FETCH,
                false,
                true,
                Priority.MEDIUM);
            _fetchState = new FetchState(_consumer, _producerContext);
            _fetcher.Fetch(
                _fetchState,
                new NetworkFetcherCallbackImpl(
                    (response, responseLength) =>
                    {
                        Assert.IsTrue(response != null);
                        Assert.IsTrue(responseLength == -1);
                        completion.Set();
                    },
                    (throwable) =>
                    {
                        failed = true;
                        completion.Set();
                    },
                    () =>
                    {
                        failed = true;
                        completion.Set();
                    }));

            // Wait for callback
            completion.WaitOne();
            Assert.IsFalse(failed);
        }

        /// <summary>
        /// Tests out the callback with failed redirection
        /// </summary>
        [TestMethod, Timeout(3000)]
        public void TestFetchSendsErrorToCallbackAfterRedirectToSameLocation()
        {
            ManualResetEvent completion = new ManualResetEvent(false);
            bool failed = false;
            _producerContext = new SettableProducerContext(
                ImageRequest.FromUri(FAILURE_REDIRECT_URL),
                FAILURE_REDIRECT_URL,
                _producerListener,
                new object(),
                RequestLevel.FULL_FETCH,
                false,
                true,
                Priority.MEDIUM);
            _fetchState = new FetchState(_consumer, _producerContext);
            _fetcher.Fetch(
                _fetchState,
                new NetworkFetcherCallbackImpl(
                    (response, responseLength) =>
                    {
                        failed = true;
                        completion.Set();
                    },
                    (throwable) =>
                    {
                        Assert.IsTrue(throwable.GetType() == typeof(IOException));
                        completion.Set();
                    },
                    () =>
                    {
                        failed = true;
                        completion.Set();
                    }));

            // Wait for callback
            completion.WaitOne();
            Assert.IsFalse(failed);
        }
    }
}
