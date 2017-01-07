using FBCore.DataSource;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;

namespace FBCore.Tests.DataSource
{
    /// <summary>
    /// Tests for <see cref="FirstAvailableDataSourceSupplier{T}"/>
    /// </summary>
    [TestClass]
    public class FirstAvailableDataSourceSupplierTests : AbstractDataSourceSupplier
    {
        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();
            _dataSourceSupplier = FirstAvailableDataSourceSupplier<object>.Create(_suppliers);
        }

        /// <summary>
        /// All data sources failed, no intermediate results.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_F1_F2_F3_C()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribe(_dataSourceSupplier1, _src1);

            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                new Exception());
            subscriber1.OnFailure(_src1);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src1).VerifyMethodInvocation("Close", 0));             
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribe(_dataSourceSupplier2, _src2);
            DataSourceTestUtils.VerifyState(
                ((AbstractDataSource<object>)dataSource),
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);

            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                new Exception());
            subscriber2.OnFailure(_src2);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src2).VerifyMethodInvocation("Close", 0));
            IDataSubscriber<object> subscriber3 = VerifyGetAndSubscribe(_dataSourceSupplier3, _src3);
            DataSourceTestUtils.VerifyState(
                ((AbstractDataSource<object>)dataSource),
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);

            Exception throwable = new Exception();
            ((MockAbstractDataSource<object>)_src3).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                throwable);
            subscriber3.OnFailure(_src3);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src3).VerifyMethodInvocation("Close", 0));
            VerifySubscriber(dataSource, _src3, DataSourceTestUtils.ON_FAILURE);
            DataSourceTestUtils.VerifyState(
                ((AbstractDataSource<object>)dataSource),
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                throwable);

            TestClose(dataSource);
            DataSourceTestUtils.VerifyState(
                ((AbstractDataSource<object>)dataSource),
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                throwable);
        }
    }
}
