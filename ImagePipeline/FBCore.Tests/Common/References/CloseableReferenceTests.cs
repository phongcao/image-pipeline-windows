using FBCore.Common.References;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;

namespace FBCore.Tests.Common.References
{
    /// <summary>
    /// Basic tests for closeable references
    /// </summary>
    [TestClass]
    public class CloseableReferenceTest
    {
        private MockDisposable _mockCloseable;
        private CloseableReference<IDisposable> _closeableReference;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _mockCloseable = new MockDisposable();
            _closeableReference = CloseableReference<IDisposable>.of(_mockCloseable);
        }

        /// <summary>
        /// Tests the creation of the closable reference
        /// </summary>
        [TestMethod]
        public void TestCreation()
        {
            Assert.AreEqual(1, _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
        }

        /// <summary>
        /// Tests out the Clone method
        /// </summary>
        [TestMethod]
        public void TestClone()
        {
            CloseableReference<IDisposable> copy = _closeableReference.Clone();
            Assert.AreEqual(2, _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
            Assert.AreSame(_closeableReference.GetUnderlyingReferenceTestOnly(), copy.GetUnderlyingReferenceTestOnly());
        }

        /// <summary>
        /// Tests out the Dispose method
        /// </summary>
        [TestMethod]
        public void TestCloseReference()
        {
            CloseableReference<IDisposable> copy = _closeableReference.Clone();
            Assert.AreEqual(2, _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
            copy.Dispose();
            Assert.AreEqual(1, _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
        }

        /// <summary>
        /// Tests out the Dispose method of the mock disposable object
        /// </summary>
        [TestMethod]
        public void TestCloseWhenRefcount0()
        {
            _closeableReference.Dispose();
            Assert.AreEqual(0, _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
            Assert.IsTrue(_mockCloseable.Disposed);
        }
    }
}
