using FBCore.Common.References;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;

namespace FBCore.Tests.Common.References
{
    /**
     * Basic tests for closeable references
     */
    [TestClass]
    public class CloseableReferenceTest
    {
        private MockDisposable _mockCloseable;
        private CloseableReference<IDisposable> _closeableReference;

        [TestInitialize]
        public void Initialize()
        {
            _mockCloseable = new MockDisposable();
            _closeableReference = CloseableReference<IDisposable>.of(_mockCloseable);
        }

        [TestMethod]
        public void TestCreation()
        {
            Assert.AreEqual(1, _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
        }

        [TestMethod]
        public void TestClone()
        {
            CloseableReference<IDisposable> copy = _closeableReference.Clone();
            Assert.AreEqual(2, _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
            Assert.AreSame(_closeableReference.GetUnderlyingReferenceTestOnly(), copy.GetUnderlyingReferenceTestOnly());
        }

        [TestMethod]
        public void TestCloseReference()
        {
            CloseableReference<IDisposable> copy = _closeableReference.Clone();
            Assert.AreEqual(2, _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
            copy.Dispose();
            Assert.AreEqual(1, _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
        }

        [TestMethod]
        public void TestCloseWhenRefcount0()
        {
            _closeableReference.Dispose();
            Assert.AreEqual(0, _closeableReference.GetUnderlyingReferenceTestOnly().GetRefCountTestOnly());
            Assert.IsTrue(_mockCloseable.IsDisposed);
        }
    }
}
