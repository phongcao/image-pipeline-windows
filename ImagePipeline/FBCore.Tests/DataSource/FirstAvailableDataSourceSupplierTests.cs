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
            Assert.IsTrue(((MockAbstractDataSource<object>)_src1).VerifyMethodInvocation("Close", 1));             
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribe(_dataSourceSupplier2, _src2);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
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
            Assert.IsTrue(((MockAbstractDataSource<object>)_src2).VerifyMethodInvocation("Close", 1));
            IDataSubscriber<object> subscriber3 = VerifyGetAndSubscribe(_dataSourceSupplier3, _src3);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
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
            Assert.IsTrue(((MockAbstractDataSource<object>)_src3).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src3, DataSourceTestUtils.ON_FAILURE);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                throwable);

            TestClose(dataSource);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                throwable);
        }

        /// <summary>
        /// All data sources failed, second data source produced multiple intermediate results.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_F1_I2_I2_F2_F3_C()
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
            Assert.IsTrue(((MockAbstractDataSource<object>)_src1).VerifyMethodInvocation("Close", 1));
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribe(_dataSourceSupplier2, _src2);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);

            object val2a = new object();
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2a,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnNewResult(_src2);
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src2,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2a,
                DataSourceTestUtils.NOT_FAILED,
                null);

            object val2b = new object();
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2b,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnNewResult(_src2);
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src2,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2b,
                DataSourceTestUtils.NOT_FAILED,
                null);

            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2b,
                DataSourceTestUtils.FAILED,
                new Exception());
            subscriber2.OnFailure(_src2);
            IDataSubscriber<object> subscriber3 = VerifyGetAndSubscribe(_dataSourceSupplier3, _src3);
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src2,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2b,
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
            Assert.IsTrue(((MockAbstractDataSource<object>)_src3).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src3, DataSourceTestUtils.ON_FAILURE);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src2,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2b,
                DataSourceTestUtils.FAILED,
                throwable);

            TestClose(dataSource, _src2);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                throwable);
        }

        /// <summary>
        /// All data sources failed, first two data sources produced intermediate results. 
        /// Only first kept.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_I1_F1_I2_F2_F3_C()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribe(_dataSourceSupplier1, _src1);

            object val1 = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.NOT_FAILED,
                null);

            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnFailure(_src1);
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribe(_dataSourceSupplier2, _src2);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.NOT_FAILED,
                null);

            // I2 gets ignored because we already have I1
            object val2 = new object();
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnNewResult(_src2);
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.NO_INTERACTIONS);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.NOT_FAILED,
                null);

            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2,
                DataSourceTestUtils.FAILED,
                new Exception());
            subscriber2.OnFailure(_src2);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src2).VerifyMethodInvocation("Close", 1));
            IDataSubscriber<object> subscriber3 = VerifyGetAndSubscribe(_dataSourceSupplier3, _src3);
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
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
            Assert.IsTrue(((MockAbstractDataSource<object>)_src3).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src3, DataSourceTestUtils.ON_FAILURE);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.FAILED,
                throwable);

            TestClose(dataSource, _src1);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                throwable);
        }

        /// <summary>
        /// First data source failed, second succeeded, no intermediate results.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_F1_S2_C()
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
            Assert.IsTrue(((MockAbstractDataSource<object>)_src1).VerifyMethodInvocation("Close", 1));
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribe(_dataSourceSupplier2, _src2);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);

            object val = new object();
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnNewResult(_src2);
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src2,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val,
                DataSourceTestUtils.NOT_FAILED,
                null);

            TestClose(dataSource, _src2);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);
        }

        /// <summary>
        /// First data source succeeded, no intermediate results.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_S1_C()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribe(_dataSourceSupplier1, _src1);

            object val = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val,
                DataSourceTestUtils.NOT_FAILED,
                null);

            TestClose(dataSource, _src1);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);
        }

        /// <summary>
        /// First data source succeeded, with multiple intermediate results.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_I1_I1_S1_C()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribe(_dataSourceSupplier1, _src1);

            object val1 = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.NOT_FAILED,
                null);

            object val2 = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2,
                DataSourceTestUtils.NOT_FAILED,
                null);

            object val = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val,
                DataSourceTestUtils.NOT_FAILED,
                null);

            TestClose(dataSource, _src1);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);
        }

        /// <summary>
        /// First data source failed with intermediate results, second succeeded with intermediate results.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_I1_F1_I2_S2_C()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribe(_dataSourceSupplier1, _src1);

            object val1 = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.NOT_FAILED,
                null);

            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.FAILED,
                new Exception());
            subscriber1.OnFailure(_src1);
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribe(_dataSourceSupplier2, _src2);
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.NOT_FAILED,
                null);

            // I2 gets ignored because we already have I1
            object val2 = new object();
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnNewResult(_src2);
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.NO_INTERACTIONS);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.NOT_FAILED,
                null);

            object val = new object();
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnNewResult(_src2);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src1).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src2,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val,
                DataSourceTestUtils.NOT_FAILED,
                null);

            TestClose(dataSource, _src2);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);
        }

        /// <summary>
        /// First data source failed with intermediate results, second had intermediate results but closed.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_I1_F1_I2_C()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribe(_dataSourceSupplier1, _src1);

            object val1 = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.NOT_FAILED,
                null);

            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.FAILED,
                new Exception());
            subscriber1.OnFailure(_src1);
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribe(_dataSourceSupplier2, _src2);
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.NOT_FAILED,
                null);

            // I2 gets ignored because we already have I1
            object val2 = new object();
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnNewResult(_src2);
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.NO_INTERACTIONS);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.NOT_FAILED,
                null);

            TestClose(dataSource, _src1, _src2);
            VerifySubscriber(dataSource, null, DataSourceTestUtils.ON_CANCELLATION);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);
        }

        /// <summary>
        /// Early close with no results.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_C()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribe(_dataSourceSupplier1, _src1);

            TestClose(dataSource, _src1);
            VerifySubscriber(dataSource, null, DataSourceTestUtils.ON_CANCELLATION);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);
        }

        /// <summary>
        /// Ignore callbacks after closed.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_I1_C_S1()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribe(_dataSourceSupplier1, _src1);

            object val1 = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.NOT_FAILED,
                null);

            TestClose(dataSource, _src1);
            VerifySubscriber(dataSource, null, DataSourceTestUtils.ON_CANCELLATION);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);

            object val = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.NO_INTERACTIONS);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);
        }

        /// <summary>
        /// Test data source without result
        /// </summary>
        [TestMethod]
        public void TestLifecycle_WithoutResult_NI1_NS1_I2_S2_C()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribe(_dataSourceSupplier1, _src1);

            // I1 gets ignored because there is no result
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.NO_INTERACTIONS);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);

            // S1 gets ignored because there is no result
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src1).VerifyMethodInvocation("Close", 1));
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribe(_dataSourceSupplier2, _src2);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);

            object val2a = new object();
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2a,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnNewResult(_src2);
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src2,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2a,
                DataSourceTestUtils.NOT_FAILED,
                null);

            object val2b = new object();
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2b,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnNewResult(_src2);
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src2,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2b,
                DataSourceTestUtils.NOT_FAILED,
                null);

            TestClose(dataSource, _src2);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);
        }
    }
}
