using FBCore.Common.References;
using FBCore.Concurrency;
using FBCore.DataSource;
using ImagePipeline.Datasource;
using ImagePipeline.Memory;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;

namespace ImagePipeline.Tests.Datasource
{
    /// <summary>
    /// Tests for <see cref="ListDataSource{T}"/>
    /// </summary>
    [TestClass]
    public class ListDataSourceTests
    {
        private SettableDataSource<int> _settableDataSource1;
        private SettableDataSource<int> _settableDataSource2;
        private ListDataSource<int> _listDataSource;
        private CloseableReference<int> _ref1;
        private CloseableReference<int> _ref2;
        private Exception _runtimeException;

        private IResourceReleaser<int> _resourceReleaser;
        private IDataSubscriber<IList<CloseableReference<int>>> _dataSubscriber;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _resourceReleaser = new ResourceReleaserImpl<int>(_ => { });
            _dataSubscriber = new MockDataSubscriber<IList<CloseableReference<int>>>();
            _settableDataSource1 = SettableDataSource<int>.Create<int>();
            _settableDataSource2 = SettableDataSource<int>.Create<int>();
            _listDataSource = ListDataSource<int>.Create(_settableDataSource1, _settableDataSource2);
            _ref1 = CloseableReference<int>.of(1, _resourceReleaser);
            _ref2 = CloseableReference<int>.of(2, _resourceReleaser);
            _runtimeException = new Exception();
            _listDataSource.Subscribe(_dataSubscriber, CallerThreadExecutor.Instance);
        }

        /// <summary>
        /// Tests out setting the first data source only
        /// </summary>
        [TestMethod]
        public void TestFirstResolvedSecondNot()
        {
            ResolveFirstDataSource();
            AssertDataSourceNotResolved();
        }

        /// <summary>
        /// Tests out setting the second data source only
        /// </summary>
        [TestMethod]
        public void TestSecondResolvedFirstNot()
        {
            ResolveSecondDataSource();
            AssertDataSourceNotResolved();
        }

        /// <summary>
        /// Tests out cancelling the first data source only
        /// </summary>
        [TestMethod]
        public void TestFirstCancelledSecondNot()
        {
            CancelFirstDataSource();
            AssertDataSourceCancelled();
        }

        /// <summary>
        /// Tests out cancelling the second data source only
        /// </summary>
        [TestMethod]
        public void TestSecondCancelledFirstNot()
        {
            CancelSecondDataSource();
            AssertDataSourceCancelled();
        }

        /// <summary>
        /// Tests out failing the first data source only
        /// </summary>
        [TestMethod]
        public void TestFirstFailedSecondNot()
        {
            FailFirstDataSource();
            AssertDataSourceFailed();
        }

        /// <summary>
        /// Tests out cancelling the second data source only
        /// </summary>
        [TestMethod]
        public void TestSecondFailedFirstNot()
        {
            FailSecondDataSource();
            AssertDataSourceFailed();
        }

        /// <summary>
        /// Tests out the resolved first and failed second data source
        /// </summary>
        [TestMethod]
        public void TestFirstResolvedSecondFailed()
        {
            ResolveFirstDataSource();
            FailSecondDataSource();
            AssertDataSourceFailed();
        }

        /// <summary>
        /// Tests out the resolved second and failed first data source
        /// </summary>
        [TestMethod]
        public void TestSecondResolvedFirstFailed()
        {
            FailFirstDataSource();
            ResolveSecondDataSource();
            AssertDataSourceFailed();
        }

        /// <summary>
        /// Tests out the resolved first and cancelled second data source
        /// </summary>
        [TestMethod]
        public void TestFirstResolvedSecondCancelled()
        {
            ResolveFirstDataSource();
            CancelSecondDataSource();
            AssertDataSourceCancelled();
        }

        /// <summary>
        /// Tests out the resolved second and cancelled first data source
        /// </summary>
        [TestMethod]
        public void TestSecondResolvedFirstCancelled()
        {
            ResolveSecondDataSource();
            CancelFirstDataSource();
            AssertDataSourceCancelled();
        }

        /// <summary>
        /// Tests out resolving both data sources
        /// </summary>
        [TestMethod]
        public void TestFirstAndSecondResolved()
        {
            ResolveFirstDataSource();
            ResolveSecondDataSource();
            AssertDataSourceResolved();
        }

        /// <summary>
        /// Tests out closing all data sources
        /// </summary>
        [TestMethod]
        public void TestCloseClosesAllDataSources()
        {
            _listDataSource.Close();
            Assert.IsTrue(_settableDataSource1.IsClosed());
            Assert.IsTrue(_settableDataSource2.IsClosed());
        }

        private void FailFirstDataSource()
        {
            _settableDataSource1.SetException(_runtimeException);
        }

        private void FailSecondDataSource()
        {
            _settableDataSource2.SetException(_runtimeException);
        }

        private void CancelFirstDataSource()
        {
            _settableDataSource1.Close();
        }

        private void CancelSecondDataSource()
        {
            _settableDataSource2.Close();
        }

        private void ResolveFirstDataSource()
        {
            _settableDataSource1.Set(_ref1);
        }

        private void ResolveSecondDataSource()
        {
            _settableDataSource2.Set(_ref2);
        }

        private void VerifyNoMoreInteractions(
            IDataSubscriber<IList<CloseableReference<int>>> dataSubscriber)
        {
            Assert.IsTrue(((MockDataSubscriber<IList<CloseableReference<int>>>)_dataSubscriber).HasZeroInteractions);
        }

        private void AssertDataSourceNotResolved()
        {
            VerifyNoMoreInteractions(_dataSubscriber);
            Assert.IsFalse(_listDataSource.HasResult());
            Assert.IsFalse(_listDataSource.HasFailed());
            Assert.IsFalse(_listDataSource.IsFinished());
            Assert.IsNull(_listDataSource.GetFailureCause());
            Assert.IsNull(_listDataSource.GetResult());
        }

        private void AssertDataSourceFailed()
        {
            Assert.IsTrue(
                ((MockDataSubscriber<IList<CloseableReference<int>>>)_dataSubscriber).OnFailureCallCount == 1);
            Assert.AreSame(
                _listDataSource,
                ((MockDataSubscriber<IList<CloseableReference<int>>>)_dataSubscriber).DataSource);
            ((MockDataSubscriber<IList<CloseableReference<int>>>)_dataSubscriber).OnFailureCallCount = 0;
            Assert.IsTrue(
                ((MockDataSubscriber<IList<CloseableReference<int>>>)_dataSubscriber).HasZeroInteractions);
            Assert.IsFalse(_listDataSource.HasResult());
            Assert.IsTrue(_listDataSource.HasFailed());
            Assert.IsTrue(_listDataSource.IsFinished());
            Assert.AreSame(_runtimeException, _listDataSource.GetFailureCause());
            Assert.IsNull(_listDataSource.GetResult());
        }

        private void AssertDataSourceCancelled()
        {
            Assert.IsTrue(
                ((MockDataSubscriber<IList<CloseableReference<int>>>)_dataSubscriber).OnFailureCallCount == 1);
            Assert.AreSame(
                _listDataSource,
                ((MockDataSubscriber<IList<CloseableReference<int>>>)_dataSubscriber).DataSource);
            ((MockDataSubscriber<IList<CloseableReference<int>>>)_dataSubscriber).OnFailureCallCount = 0;
            Assert.IsTrue(
                ((MockDataSubscriber<IList<CloseableReference<int>>>)_dataSubscriber).HasZeroInteractions);
            Assert.IsFalse(_listDataSource.HasResult());
            Assert.IsTrue(_listDataSource.HasFailed());
            Assert.IsTrue(_listDataSource.IsFinished());
            Assert.IsNotNull(_listDataSource.GetFailureCause());
            Assert.IsNull(_listDataSource.GetResult());
        }

        private void AssertDataSourceResolved()
        {
            Assert.IsTrue(
                ((MockDataSubscriber<IList<CloseableReference<int>>>)_dataSubscriber).OnNewResultCallCount == 1);
            Assert.AreSame(
                _listDataSource,
                ((MockDataSubscriber<IList<CloseableReference<int>>>)_dataSubscriber).DataSource);
            ((MockDataSubscriber<IList<CloseableReference<int>>>)_dataSubscriber).OnNewResultCallCount = 0;
            Assert.IsTrue(
                ((MockDataSubscriber<IList<CloseableReference<int>>>)_dataSubscriber).HasZeroInteractions);
            Assert.IsTrue(_listDataSource.HasResult());
            Assert.IsFalse(_listDataSource.HasFailed());
            Assert.IsTrue(_listDataSource.IsFinished());
            Assert.IsNull(_listDataSource.GetFailureCause());
            Assert.AreEqual(2, _listDataSource.GetResult().Count);
            Assert.AreEqual(1, (int)_listDataSource.GetResult()[0].Get());
            Assert.AreEqual(2, (int)_listDataSource.GetResult()[1].Get());
        }
    }
}
