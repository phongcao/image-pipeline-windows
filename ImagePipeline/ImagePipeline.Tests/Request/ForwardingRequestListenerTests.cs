using ImagePipeline.Listener;
using ImagePipeline.Producers;
using ImagePipeline.Request;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;

namespace ImagePipeline.Tests.Request
{
    /// <summary>
    /// Tests for <see cref="ForwardingRequestListener"/>
    /// </summary>
    [TestClass]
    public class ForwardingRequestListenerTests
    {
        private ImageRequest _request;
        private object _callerContext;
        private Exception _error;
        private IDictionary<string, string> _immutableMap;

        private IRequestListener _requestListener1;
        private IRequestListener _requestListener2;
        private IRequestListener _requestListener3;
        private ForwardingRequestListener _listenerManager;
        private string _requestId = "DummyRequestId";
        private string _producerName = "DummyProducerName";
        private string _producerEventName = "DummyProducerEventName";
        private bool _isPrefetch = true;

        private int _onRequestStartFuncCalls1;
        private int _onRequestSuccessFuncCall1;
        private int _onRequestFailureFuncCalls1;
        private int _onRequestCancellationFuncCalls1;
        private int _onProducerStartFuncCalls1;
        private int _onProducerEventFuncCalls1;
        private int _onProducerFinishWithSuccessFuncCalls1;
        private int _onProducerFinishWithFailureFuncCalls1;
        private int _onProducerFinishWithCancellationFuncCalls1;
        private int _requiresExtraMapFuncCalls1;

        private int _onRequestStartFuncCalls2;
        private int _onRequestSuccessFuncCall2;
        private int _onRequestFailureFuncCalls2;
        private int _onRequestCancellationFuncCalls2;
        private int _onProducerStartFuncCalls2;
        private int _onProducerEventFuncCalls2;
        private int _onProducerFinishWithSuccessFuncCalls2;
        private int _onProducerFinishWithFailureFuncCalls2;
        private int _onProducerFinishWithCancellationFuncCalls2;
        private int _requiresExtraMapFuncCalls2;

        private int _onRequestStartFuncCalls3;
        private int _onRequestSuccessFuncCall3;
        private int _onRequestFailureFuncCalls3;
        private int _onRequestCancellationFuncCalls3;
        private int _onProducerStartFuncCalls3;
        private int _onProducerEventFuncCalls3;
        private int _onProducerFinishWithSuccessFuncCalls3;
        private int _onProducerFinishWithFailureFuncCalls3;
        private int _onProducerFinishWithCancellationFuncCalls3;
        private int _requiresExtraMapFuncCalls3;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _request = ImageRequestBuilder.NewBuilderWithSource(new Uri("http://request")).Build();
            _callerContext = new object();
            _error = new Exception();
            _immutableMap = new Dictionary<string, string>();

            _onRequestStartFuncCalls1 = 0;
            _onRequestSuccessFuncCall1 = 0;
            _onRequestFailureFuncCalls1 = 0;
            _onRequestCancellationFuncCalls1 = 0;
            _onProducerStartFuncCalls1 = 0;
            _onProducerEventFuncCalls1 = 0;
            _onProducerFinishWithSuccessFuncCalls1 = 0;
            _onProducerFinishWithFailureFuncCalls1 = 0;
            _onProducerFinishWithCancellationFuncCalls1 = 0;
            _requiresExtraMapFuncCalls1 = 0;

            _onRequestStartFuncCalls2 = 0;
            _onRequestSuccessFuncCall2 = 0;
            _onRequestFailureFuncCalls2 = 0;
            _onRequestCancellationFuncCalls2 = 0;
            _onProducerStartFuncCalls2 = 0;
            _onProducerEventFuncCalls2 = 0;
            _onProducerFinishWithSuccessFuncCalls2 = 0;
            _onProducerFinishWithFailureFuncCalls2 = 0;
            _onProducerFinishWithCancellationFuncCalls2 = 0;
            _requiresExtraMapFuncCalls2 = 0;

            _onRequestStartFuncCalls3 = 0;
            _onRequestSuccessFuncCall3 = 0;
            _onRequestFailureFuncCalls3 = 0;
            _onRequestCancellationFuncCalls3 = 0;
            _onProducerStartFuncCalls3 = 0;
            _onProducerEventFuncCalls3 = 0;
            _onProducerFinishWithSuccessFuncCalls3 = 0;
            _onProducerFinishWithFailureFuncCalls3 = 0;
            _onProducerFinishWithCancellationFuncCalls3 = 0;
            _requiresExtraMapFuncCalls3 = 0;

            ProducerListenerHelper producerListener1 = new ProducerListenerHelper(
                (_, __) => { ++_onProducerStartFuncCalls1; },
                (_, __, ___) => { ++_onProducerEventFuncCalls1; },
                (_, __, ___) => { ++_onProducerFinishWithSuccessFuncCalls1; },
                (_, __, ___, ____) => { ++_onProducerFinishWithFailureFuncCalls1; },
                (_, __, ___) => { ++_onProducerFinishWithCancellationFuncCalls1; },
                (_) => { ++_requiresExtraMapFuncCalls1; return false; });

            ProducerListenerHelper producerListener2 = new ProducerListenerHelper(
                (_, __) => { ++_onProducerStartFuncCalls2; },
                (_, __, ___) => { ++_onProducerEventFuncCalls2; },
                (_, __, ___) => { ++_onProducerFinishWithSuccessFuncCalls2; },
                (_, __, ___, ____) => { ++_onProducerFinishWithFailureFuncCalls2; },
                (_, __, ___) => { ++_onProducerFinishWithCancellationFuncCalls2; },
                (_) => { ++_requiresExtraMapFuncCalls2; return false; });

            ProducerListenerHelper producerListener3 = new ProducerListenerHelper(
                (_, __) => { ++_onProducerStartFuncCalls3; },
                (_, __, ___) => { ++_onProducerEventFuncCalls3; },
                (_, __, ___) => { ++_onProducerFinishWithSuccessFuncCalls3; },
                (_, __, ___, ____) => { ++_onProducerFinishWithFailureFuncCalls3; },
                (_, __, ___) => { ++_onProducerFinishWithCancellationFuncCalls3; },
                (_) => { ++_requiresExtraMapFuncCalls3; return false; });

            _requestListener1 = new RequestListenerHelper(
                producerListener1,
                (_, __, ___, ____) => { ++_onRequestStartFuncCalls1; },
                (_, __, ___) => { ++_onRequestSuccessFuncCall1; },
                (_, __, ___, ____) => { ++_onRequestFailureFuncCalls1; },
                (_) => { ++_onRequestCancellationFuncCalls1; });

            _requestListener2 = new RequestListenerHelper(
                producerListener2,
                (_, __, ___, ____) => { ++_onRequestStartFuncCalls2; },
                (_, __, ___) => { ++_onRequestSuccessFuncCall2; },
                (_, __, ___, ____) => { ++_onRequestFailureFuncCalls2; },
                (_) => { ++_onRequestCancellationFuncCalls2; });

            _requestListener3 = new RequestListenerHelper(
                producerListener3,
                (_, __, ___, ____) => { ++_onRequestStartFuncCalls3; },
                (_, __, ___) => { ++_onRequestSuccessFuncCall3; },
                (_, __, ___, ____) => { ++_onRequestFailureFuncCalls3; },
                (_) => { ++_onRequestCancellationFuncCalls3; });

            _listenerManager = new ForwardingRequestListener(new HashSet<IRequestListener>
            {
                _requestListener1,
                _requestListener2,
                _requestListener3
            });
        }

        /// <summary>
        /// Tests out the OnRequestStart method
        /// </summary>
        [TestMethod]
        public void TestOnRequestStart()
        {
            _listenerManager.OnRequestStart(_request, _callerContext, _requestId, _isPrefetch);
            Assert.IsTrue(1 == _onRequestStartFuncCalls1);
            Assert.IsTrue(1 == _onRequestStartFuncCalls2);
            Assert.IsTrue(1 == _onRequestStartFuncCalls3);
        }

        /// <summary>
        /// Tests out the OnRequestSuccess method
        /// </summary>
        [TestMethod]
        public void TestOnRequestSuccess()
        {
            _listenerManager.OnRequestSuccess(_request, _requestId, _isPrefetch);
            Assert.IsTrue(1 == _onRequestSuccessFuncCall1);
            Assert.IsTrue(1 == _onRequestSuccessFuncCall2);
            Assert.IsTrue(1 == _onRequestSuccessFuncCall3);
        }

        /// <summary>
        /// Tests out the OnRequestFailure method
        /// </summary>
        [TestMethod]
        public void TestOnRequestFailure()
        {
            _listenerManager.OnRequestFailure(_request, _requestId, _error, _isPrefetch);
            Assert.IsTrue(1 == _onRequestFailureFuncCalls1);
            Assert.IsTrue(1 == _onRequestFailureFuncCalls2);
            Assert.IsTrue(1 == _onRequestFailureFuncCalls3);
        }

        /// <summary>
        /// Tests out the OnProducerStart method
        /// </summary>
        [TestMethod]
        public void TestOnProducerStart()
        {
            _listenerManager.OnProducerStart(_requestId, _producerName);
            Assert.IsTrue(1 == _onProducerStartFuncCalls1);
            Assert.IsTrue(1 == _onProducerStartFuncCalls2);
            Assert.IsTrue(1 == _onProducerStartFuncCalls3);
        }

        /// <summary>
        /// Tests out the OnProducerFinishWithSuccess method
        /// </summary>
        [TestMethod]
        public void TestOnProducerFinishWithSuccess()
        {
            _listenerManager.OnProducerFinishWithSuccess(_requestId, _producerName, _immutableMap);
            Assert.IsTrue(1 == _onProducerFinishWithSuccessFuncCalls1);
            Assert.IsTrue(1 == _onProducerFinishWithSuccessFuncCalls2);
            Assert.IsTrue(1 == _onProducerFinishWithSuccessFuncCalls3);
        }

        /// <summary>
        /// Tests out the OnProducerFinishWithFailure method
        /// </summary>
        [TestMethod]
        public void TestOnProducerFinishWithFailure()
        {
            _listenerManager.OnProducerFinishWithFailure(_requestId, _producerName, _error, _immutableMap);
            Assert.IsTrue(1 == _onProducerFinishWithFailureFuncCalls1);
            Assert.IsTrue(1 == _onProducerFinishWithFailureFuncCalls2);
            Assert.IsTrue(1 == _onProducerFinishWithFailureFuncCalls3);
        }

        /// <summary>
        /// Tests out the OnProducerFinishWithCancellation method
        /// </summary>
        [TestMethod]
        public void TestOnProducerFinishWithCancellation()
        {
            _listenerManager.OnProducerFinishWithCancellation(_requestId, _producerName, _immutableMap);
            Assert.IsTrue(1 == _onProducerFinishWithCancellationFuncCalls1);
            Assert.IsTrue(1 == _onProducerFinishWithCancellationFuncCalls2);
            Assert.IsTrue(1 == _onProducerFinishWithCancellationFuncCalls3);
        }

        /// <summary>
        /// Tests out the OnProducerEvent method
        /// </summary>
        [TestMethod]
        public void TestOnProducerEvent()
        {
            _listenerManager.OnProducerEvent(_requestId, _producerName, _producerEventName);
            Assert.IsTrue(1 == _onProducerEventFuncCalls1);
            Assert.IsTrue(1 == _onProducerEventFuncCalls2);
            Assert.IsTrue(1 == _onProducerEventFuncCalls3);
        }

        /// <summary>
        /// Tests out the RequiresExtraMap method
        /// </summary>
        [TestMethod]
        public void TestRequiresExtraMap()
        {
            _listenerManager.RequiresExtraMap(_requestId);
            Assert.IsTrue(1 == _requiresExtraMapFuncCalls1);
            Assert.IsTrue(1 == _requiresExtraMapFuncCalls2);
            Assert.IsTrue(1 == _requiresExtraMapFuncCalls3);
        }
    }
}
