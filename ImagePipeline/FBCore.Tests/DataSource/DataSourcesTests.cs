using FBCore.DataSource;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;

namespace FBCore.Tests.DataSource
{
    /// <summary>
    /// Tests for IDataSource
    /// </summary>
    [TestClass]
    public class DataSourcesTests
    {
        private Exception _exception;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _exception = new Exception();
        }

        /// <summary>
        /// Tests failed data source
        /// </summary>
        [TestMethod]
        public void TestImmediateFailedDataSource()
        {
            IDataSource<object> dataSource = DataSources.ImmediateFailedDataSource<object>(_exception);
            Assert.IsTrue(dataSource.IsFinished());
            Assert.IsTrue(dataSource.HasFailed());
            Assert.AreEqual(_exception, dataSource.GetFailureCause());
            Assert.IsFalse(dataSource.HasResult());
            Assert.IsFalse(dataSource.IsClosed());
        }
    }
}
