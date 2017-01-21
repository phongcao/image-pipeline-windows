using ImagePipeline.Producers;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;

namespace ImagePipeline.Tests.Producers
{
    /// <summary>
    /// Tests for BaseConsumer
    /// </summary>
    [TestClass]
    public class BaseConsumerTests
    {
        private MockBaseConsumer<object> _delegatedConsumer;
        private object _result;
        private object _result2;
        private Exception _error;
        private BaseConsumer<object> _baseConsumer;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _delegatedConsumer = new MockBaseConsumer<object>();
            _result = new object();
            _result2 = new object();
            _error = new Exception();
            _baseConsumer = new BaseConsumerImpl<object>(
                (newResult, isLast) =>
                {
                    _delegatedConsumer.OnNewResult(newResult, isLast);
                },
                (error) =>
                {
                    _delegatedConsumer.OnFailure(error);
                },
                () =>
                {
                    _delegatedConsumer.OnCancellation();
                },
                (_) => { });
        }

        /// <summary>
        /// Tests for OnNewResult
        /// </summary>
        [TestMethod]
        public void TestOnNewResultDoesNotThrow()
        {
            _delegatedConsumer.SetOnNewResultImplFunc((_, __) =>
            {
                throw new Exception();
            });

            _baseConsumer.OnNewResult(_result, false);
            Assert.IsTrue(1 == _delegatedConsumer.OnNewResultCount);
        }

        /// <summary>
        /// Tests for OnFailure
        /// </summary>
        [TestMethod]
        public void TestOnFailureDoesNotThrow()
        {
            _delegatedConsumer.SetOnFailureImplFunc(_ =>
            {
                throw new Exception();
            });

            _baseConsumer.OnFailure(_error);
            Assert.IsTrue(1 == _delegatedConsumer.OnFailureCount);
        }

        /// <summary>
        /// Tests for OnCancellation
        /// </summary>
        [TestMethod]
        public void TestOnCancellationDoesNotThrow()
        {
            _delegatedConsumer.SetOnCancellationImplFunc(() =>
            {
                throw new Exception();
            });

            _baseConsumer.OnCancellation();
            Assert.IsTrue(1 == _delegatedConsumer.OnCancellationCount);
        }

        /// <summary>
        /// Tests consumer stops after having the final result
        /// </summary>
        [TestMethod]
        public void TestDoesNotForwardAfterFinalResult()
        {
            _baseConsumer.OnNewResult(_result, true);
            _baseConsumer.OnFailure(_error);
            _baseConsumer.OnCancellation();
            Assert.IsTrue(1 == _delegatedConsumer.OnNewResultCount);
            Assert.IsTrue(0 == _delegatedConsumer.OnFailureCount);
            Assert.IsTrue(0 == _delegatedConsumer.OnCancellationCount);
        }

        /// <summary>
        /// Tests consumer stops after having failure
        /// </summary>
        [TestMethod]
        public void TestDoesNotForwardAfterOnFailure()
        {
            _baseConsumer.OnFailure(_error);
            _baseConsumer.OnNewResult(_result, true);
            _baseConsumer.OnCancellation();
            Assert.IsTrue(1 == _delegatedConsumer.OnFailureCount);
            Assert.IsTrue(0 == _delegatedConsumer.OnNewResultCount);
            Assert.IsTrue(0 == _delegatedConsumer.OnCancellationCount);
        }

        /// <summary>
        /// Tests consumer stops after being cancelled
        /// </summary>
        [TestMethod]
        public void TestDoesNotForwardAfterOnCancellation()
        {
            _baseConsumer.OnCancellation();
            _baseConsumer.OnNewResult(_result, true);
            _baseConsumer.OnFailure(_error);
            Assert.IsTrue(1 == _delegatedConsumer.OnCancellationCount);
            Assert.IsTrue(0 == _delegatedConsumer.OnNewResultCount);
            Assert.IsTrue(0 == _delegatedConsumer.OnFailureCount);
        }

        /// <summary>
        /// Tests consumer continues after having the intermediate result
        /// </summary>
        [TestMethod]
        public void TestDoesForwardAfterIntermediateResult()
        {
            _baseConsumer.OnNewResult(_result, false);
            _baseConsumer.OnNewResult(_result2, true);
            Assert.IsTrue(2 == _delegatedConsumer.OnNewResultCount);
            Assert.IsTrue(0 == _delegatedConsumer.OnFailureCount);
            Assert.IsTrue(0 == _delegatedConsumer.OnCancellationCount);
        }
    }
}
