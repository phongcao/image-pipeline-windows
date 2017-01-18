using FBCore.Concurrency;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;

namespace FBCore.Tests.Concurrency
{
    /// <summary>
    /// Tests for <see cref="StatefulRunnable{T}"/>
    /// </summary>
    [TestClass]
    public class StatefulRunnableTests
    {
        private MockStatefulRunnable<object> _statefulRunnable;
        private object _result;
        private Exception _exception;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _result = new object();
            _exception = new Exception();
            _statefulRunnable = new MockStatefulRunnable<object>();
        }

        /// <summary>
        /// Tests running successfully
        /// </summary>
        [TestMethod]
        public void TestSuccess()
        {
            RunSuccess();
            Assert.IsTrue(_statefulRunnable.OnSuccessCallCount == 1);
            Assert.IsTrue(_statefulRunnable.DisposeResultCallCount == 1);
        }

        /// <summary>
        /// Tests closing after running successfully
        /// </summary>
        [TestMethod]
        public void TestClosesResultWhenOnSuccessThrows()
        {
            _statefulRunnable.ThrowExceptionOnSuccess(_exception);

            try
            {
                RunSuccess();
                Assert.Fail();
            }
            catch (Exception)
            {
                // expected
            }

            Assert.IsTrue(_statefulRunnable.DisposeResultCallCount == 1);
            Assert.AreSame(_statefulRunnable.CallbackResult, _result);
        }

        /// <summary>
        /// Tests running failed
        /// </summary>
        [TestMethod]
        public void TestFailure()
        {
            RunFailure();
            Assert.IsTrue(_statefulRunnable.OnFailureCallCount == 1);
        }

        /// <summary>
        /// Tests running again
        /// </summary>
        [TestMethod]
        public void TestDoesNotRunAgainAfterStarted()
        {
            _statefulRunnable.SetInternalState(StatefulRunnable<object>.STATE_STARTED);
            RunSuccess();
            Assert.IsTrue(_statefulRunnable.GetResultCallCount == 0);
        }

        /// <summary>
        /// Tests cancelling
        /// </summary>
        [TestMethod]
        public void TestCancellation()
        {
            _statefulRunnable.Cancel();
            Assert.IsTrue(_statefulRunnable.OnCancellationCallCount == 1);
        }

        /// <summary>
        /// Tests running after being cancelled
        /// </summary>
        [TestMethod]
        public void TestDoesNotRunAfterCancellation()
        {
            _statefulRunnable.Cancel();
            RunSuccess();
            Assert.IsTrue(_statefulRunnable.GetResultCallCount == 0);
        }

        /// <summary>
        /// Tests cancelling twice
        /// </summary>
        [TestMethod]
        public void TestDoesNotCancelTwice()
        {
            _statefulRunnable.Cancel();
            _statefulRunnable.Cancel();
            Assert.IsTrue(_statefulRunnable.OnCancellationCallCount == 1);
        }

        /// <summary>
        /// Tests cancelling after having started
        /// </summary>
        [TestMethod]
        public void TestDoesNotCancelAfterStarted()
        {
            _statefulRunnable.SetInternalState(StatefulRunnable<object>.STATE_STARTED);
            _statefulRunnable.Cancel();
            Assert.IsTrue(_statefulRunnable.OnCancellationCallCount == 0);
        }

        private void RunSuccess()
        {
            _statefulRunnable.SetResult(_result);
            _statefulRunnable.Runnable();
        }

        private void RunFailure()
        {
            _statefulRunnable.ThrowExceptionGetResult(_exception);
            _statefulRunnable.Runnable();
        }
    }
}
