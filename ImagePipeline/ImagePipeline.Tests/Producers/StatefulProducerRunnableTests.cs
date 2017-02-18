using FBCore.Common.Internal;
using ImagePipeline.Producers;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace ImagePipeline.Tests.Producers
{
    /// <summary>
    /// Tests for <see cref="StatefulProducerRunnable{T}"/>
    /// </summary>
    [TestClass]
    public sealed class StatefulProducerRunnableTests : IDisposable
    {
        private const string REQUEST_ID = "awesomeRequestId";
        private const string PRODUCER_NAME = "aBitLessAwesomeButStillAwesomeProducerName";
        private readonly SoftwareBitmap RESULT = new SoftwareBitmap(BitmapPixelFormat.Rgba8, 50, 50);
        private readonly Exception EXCEPTION = new Exception("ConcurrentModificationException");

        private IConsumer<IDisposable> _consumer;
        private IProducerListener _producerListener;
        private ISupplier<IDisposable> _resultSupplier;
        private int _consumerOnNewResultCount;
        private int _consumerOnFailureCount;
        private int _consumerOnCancellationCount;
        private IDisposable _consumerInternalResult;
        private bool _consumerInternalIsLast;
        private Exception _consumerInternalException;
        private int _onProducerStartCount;
        private int _onProducerFinishWithSuccessCount;
        private int _onProducerFinishWithFailureCount;
        private int _onProducerFinishWithCancellationCount;
        private string _internalRequestId;
        private string _internalProducerName;
        private Exception _internalException;
        private IDictionary<string, string> _internalExtraMap;
        private bool _throwExceptionInResultSupplierGet;
        private bool _requiresExtraMap;

        private IDictionary<string, string> _successMap;
        private IDictionary<string, string> _failureMap;
        private IDictionary<string, string> _cancellationMap;

        private StatefulProducerRunnable<IDisposable> _statefulProducerRunnable;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            // Initializes mock data
            _consumer = new BaseConsumerImpl<IDisposable>(
                (result, isLast) =>
                {
                    ++_consumerOnNewResultCount;
                    _consumerInternalResult = result;
                    _consumerInternalIsLast = isLast;
                },
                (error) =>
                {
                    ++_consumerOnFailureCount;
                    _consumerInternalException = error;
                },
                () =>
                {
                    ++_consumerOnCancellationCount;
                },
                (_) => { });

            _producerListener = new ProducerListenerImpl(
                (requestId, producerName) =>
                {
                    ++_onProducerStartCount;
                    _internalRequestId = requestId;
                    _internalProducerName = producerName;
                },
                (_, __, ___) => { },
                (requestId, producerName, extraMap) =>
                {
                    ++_onProducerFinishWithSuccessCount;
                    _internalRequestId = requestId;
                    _internalProducerName = producerName;
                    _internalExtraMap = extraMap;
                },
                (requestId, producerName, error, extraMap) =>
                {
                    ++_onProducerFinishWithFailureCount;
                    _internalRequestId = requestId;
                    _internalProducerName = producerName;
                    _internalException = error;
                    _internalExtraMap = extraMap;
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
                    return _requiresExtraMap;
                });

            _resultSupplier = new SupplierImpl<IDisposable>(() =>
            {
                if (_throwExceptionInResultSupplierGet)
                {
                    throw EXCEPTION;
                }

                return RESULT;
            });

            _successMap = new Dictionary<string, string>();
            _successMap.Add("state", "success");
            _failureMap = new Dictionary<string, string>();
            _failureMap.Add("state", "failure");
            _cancellationMap = new Dictionary<string, string>();
            _cancellationMap.Add("state", "cancelled");

            _statefulProducerRunnable = new StatefulProducerRunnableImpl<IDisposable>(
                _consumer,
                _producerListener,
                PRODUCER_NAME,
                REQUEST_ID,
                null,
                null,
                null,
                (_) =>
                {
                    return _successMap;
                },
                (_) =>
                {
                    return _failureMap;
                },
                () =>
                {
                    return _cancellationMap;
                },
                (IDisposable result) =>
                {
                    try
                    {
                        result.Dispose();
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Unexpected IOException", e);
                    }
                },
                () =>
                {
                    return Task.FromResult(_resultSupplier.Get());
                });

            _throwExceptionInResultSupplierGet = false;
            _requiresExtraMap = false;
        }

        /// <summary>
        /// Test cleanup.
        /// </summary>
        public void Dispose()
        {
            RESULT.Dispose();
            if (_consumerInternalResult != null)
            {
                ((SoftwareBitmap)_consumerInternalResult).Dispose();
            }
        }

        /// <summary>
        /// Tests out success with extra map
        /// </summary>
        [TestMethod]
        public void TestOnSuccess_ExtraMap()
        {
            _requiresExtraMap = true;
            _statefulProducerRunnable.Runnable();
            Assert.IsTrue(_consumerOnNewResultCount == 1);
            Assert.AreEqual(_consumerInternalResult, RESULT);
            Assert.IsTrue(_consumerInternalIsLast);
            Assert.IsTrue(_onProducerStartCount == 1);
            Assert.IsTrue(_onProducerFinishWithSuccessCount == 1);
            Assert.AreEqual(_internalRequestId, REQUEST_ID);
            Assert.AreEqual(_internalProducerName, PRODUCER_NAME);
            Assert.AreEqual(_internalExtraMap, _successMap);

            // _consumerInternalResult.Dispose() has been invoked
            Assert.IsTrue(((SoftwareBitmap)_consumerInternalResult).PixelWidth == 0);
        }

        /// <summary>
        /// Tests out success without extra map
        /// </summary>
        [TestMethod]
        public void TestOnSuccess_NoExtraMap()
        {
            _statefulProducerRunnable.Runnable();
            Assert.IsTrue(_consumerOnNewResultCount == 1);
            Assert.AreEqual(_consumerInternalResult, RESULT);
            Assert.IsTrue(_consumerInternalIsLast);
            Assert.IsTrue(_onProducerStartCount == 1);
            Assert.IsTrue(_onProducerFinishWithSuccessCount == 1);
            Assert.AreEqual(_internalRequestId, REQUEST_ID);
            Assert.AreEqual(_internalProducerName, PRODUCER_NAME);
            Assert.IsNull(_internalExtraMap);

            // _consumerInternalResult.Dispose() has been invoked
            Assert.IsTrue(((SoftwareBitmap)_consumerInternalResult).PixelWidth == 0);
        }

        /// <summary>
        /// Tests out cancellation with extra map
        /// </summary>
        [TestMethod]
        public void TestOnCancellation_ExtraMap()
        {
            _requiresExtraMap = true;
            _statefulProducerRunnable.Cancel();
            Assert.IsTrue(_consumerOnCancellationCount == 1);
            Assert.IsTrue(_onProducerStartCount == 1);
            Assert.IsTrue(_onProducerFinishWithCancellationCount == 1);
            Assert.AreEqual(_internalRequestId, REQUEST_ID);
            Assert.AreEqual(_internalProducerName, PRODUCER_NAME);
            Assert.AreEqual(_internalExtraMap, _cancellationMap);
        }

        /// <summary>
        /// Tests out cancellation without extra map
        /// </summary>
        [TestMethod]
        public void TestOnCancellation_NoExtraMap()
        {
            _statefulProducerRunnable.Cancel();
            Assert.IsTrue(_consumerOnCancellationCount == 1);
            Assert.IsTrue(_onProducerStartCount == 1);
            Assert.IsTrue(_onProducerFinishWithCancellationCount == 1);
            Assert.AreEqual(_internalRequestId, REQUEST_ID);
            Assert.AreEqual(_internalProducerName, PRODUCER_NAME);
            Assert.IsNull(_internalExtraMap);
        }

        /// <summary>
        /// Tests out failure with extra map
        /// </summary>
        [TestMethod]
        public void TestOnFailure_ExtraMap()
        {
            _requiresExtraMap = true;
            _throwExceptionInResultSupplierGet = true;
            _statefulProducerRunnable.Runnable();
            Assert.IsTrue(_consumerOnFailureCount == 1);
            Assert.AreEqual(_consumerInternalException, EXCEPTION);
            Assert.IsTrue(_onProducerStartCount == 1);
            Assert.IsTrue(_onProducerFinishWithFailureCount == 1);
            Assert.AreEqual(_internalRequestId, REQUEST_ID);
            Assert.AreEqual(_internalProducerName, PRODUCER_NAME);
            Assert.AreEqual(_internalException, EXCEPTION);
            Assert.AreEqual(_internalExtraMap, _failureMap);
        }

        /// <summary>
        /// Tests out failure without extra map
        /// </summary>
        [TestMethod]
        public void TestOnFailure_NoExtraMap()
        {
            _throwExceptionInResultSupplierGet = true;
            _statefulProducerRunnable.Runnable();
            Assert.IsTrue(_consumerOnFailureCount == 1);
            Assert.AreEqual(_consumerInternalException, EXCEPTION);
            Assert.IsTrue(_onProducerStartCount == 1);
            Assert.IsTrue(_onProducerFinishWithFailureCount == 1);
            Assert.AreEqual(_internalRequestId, REQUEST_ID);
            Assert.AreEqual(_internalProducerName, PRODUCER_NAME);
            Assert.AreEqual(_internalException, EXCEPTION);
            Assert.IsNull(_internalExtraMap);
        }
    }
}
