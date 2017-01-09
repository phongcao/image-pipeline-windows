using FBCore.DataSource;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;

namespace FBCore.Tests.DataSource
{
    /// <summary>
    /// Tests for <see cref="IncreasingQualityDataSourceSupplier{T}"/>
    /// </summary>
    [TestClass]
    public class IncreasingQualityDataSourceSupplierTests : AbstractDataSourceSupplier
    {
        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();
            _dataSourceSupplier = IncreasingQualityDataSourceSupplier<object>.Create(_suppliers);
        }

        /// <summary>
        /// All data sources failed, highest-quality failed last, no intermediate results.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_F2_F3_F1_C()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribeM(_dataSourceSupplier1, _src1);
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribeM(_dataSourceSupplier2, _src2);
            IDataSubscriber<object> subscriber3 = VerifyGetAndSubscribe(_dataSourceSupplier3, _src3);

            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                new Exception());
            subscriber2.OnFailure(_src2);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src2).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.NO_INTERACTIONS);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);

            ((MockAbstractDataSource<object>)_src3).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                new Exception());
            subscriber3.OnFailure(_src3);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src3).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src3, DataSourceTestUtils.NO_INTERACTIONS);
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
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                throwable);
            subscriber1.OnFailure(_src1);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src1).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.ON_FAILURE);
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
        /// Highest-quality data source failed second, result of the third data source is ignored.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_F2_F1_S3_C()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribeM(_dataSourceSupplier1, _src1);
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribeM(_dataSourceSupplier2, _src2);
            IDataSubscriber<object> subscriber3 = VerifyGetAndSubscribe(_dataSourceSupplier3, _src3);

            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                new Exception());
            subscriber2.OnFailure(_src2);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src2).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.NO_INTERACTIONS);
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
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                throwable);
            subscriber1.OnFailure(_src1);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src1).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.ON_FAILURE);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                throwable);

            // Gets ignored because DS1 failed
            ((MockAbstractDataSource<object>)_src3).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                new object(),
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber3.OnFailure(_src3);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src3).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src3, DataSourceTestUtils.NO_INTERACTIONS);
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
        /// Highest-quality data source failed, result of the third data source is ignored.
        /// Second data source produced intermediate result first, the result is preserved 
        /// until closed.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_I2_F2_F1_S3_C()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribeM(_dataSourceSupplier1, _src1);
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribeM(_dataSourceSupplier2, _src2);
            IDataSubscriber<object> subscriber3 = VerifyGetAndSubscribe(_dataSourceSupplier3, _src3);

            object val2 = new object();
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnNewResult(_src2);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src3).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src2,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2,
                DataSourceTestUtils.NOT_FAILED,
                null);

            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnFailure(_src2);
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.NO_INTERACTIONS);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src2,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2,
                DataSourceTestUtils.NOT_FAILED,
                null);

            Exception throwable = new Exception();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.FAILED,
                throwable);
            subscriber1.OnFailure(_src1);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src1).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.ON_FAILURE);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src2,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2,
                DataSourceTestUtils.FAILED,
                throwable);

            // gets ignored because DS1 failed
            // besides, this data source shouldn't have finished as it was supposed to be closed!
            ((MockAbstractDataSource<object>)_src3).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                new object(),
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber3.OnFailure(_src3);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src3).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src3, DataSourceTestUtils.NO_INTERACTIONS);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src2,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2,
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
        /// Second data source produced multiple intermediate results first, intermediate result of
        /// highest-quality data source gets ignored afterwards. Second data source fails and first data
        /// source produced another intermediate result, but it gets ignored again. Finally, first data
        /// source produced its final result which is set.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_I2_I2_I1_F2_I1_S1_C()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribeM(_dataSourceSupplier1, _src1);
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribeM(_dataSourceSupplier2, _src2);
            IDataSubscriber<object> subscriber3 = VerifyGetAndSubscribe(_dataSourceSupplier3, _src3);

            object val2a = new object();
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2a,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnNewResult(_src2);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src3).VerifyMethodInvocation("Close", 1));
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

            // gets ignored because DS2 was first to produce result
            object val1a = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1a,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.NO_INTERACTIONS);
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
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.NO_INTERACTIONS);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src2,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2b,
                DataSourceTestUtils.NOT_FAILED,
                null);

            // gets ignored because DS2 was first to produce result
            object val1b = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1b,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.NO_INTERACTIONS);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src2,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2b,
                DataSourceTestUtils.NOT_FAILED,
                null);

            object val1c = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1c,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src2).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1c,
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
        /// Interleaved results.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_I3_I2_I3_S2_I1_S1_C()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribeM(_dataSourceSupplier1, _src1);
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribeM(_dataSourceSupplier2, _src2);
            IDataSubscriber<object> subscriber3 = VerifyGetAndSubscribe(_dataSourceSupplier3, _src3);

            object val3a = new object();
            ((MockAbstractDataSource<object>)_src3).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val3a,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber3.OnNewResult(_src3);
            VerifySubscriber(dataSource, _src3, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src3,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val3a,
                DataSourceTestUtils.NOT_FAILED,
                null);

            // gets ignored because DS3 was first
            object val2a = new object();
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2a,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnNewResult(_src2);
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.NO_INTERACTIONS);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src3,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val3a,
                DataSourceTestUtils.NOT_FAILED,
                null);

            object val3b = new object();
            ((MockAbstractDataSource<object>)_src3).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val3b,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber3.OnNewResult(_src3);
            VerifySubscriber(dataSource, _src3, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src3,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val3b,
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
            Assert.IsTrue(((MockAbstractDataSource<object>)_src3).VerifyMethodInvocation("Close", 1));
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

            // gets ignored because DS2 was first
            object val1a = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1a,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.NO_INTERACTIONS);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src2,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2b,
                DataSourceTestUtils.NOT_FAILED,
                null);

            object val1b = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1b,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src2).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1b,
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
        /// Second data source produced its final result, followed by the first data source.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_S2_S1_C()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribeM(_dataSourceSupplier1, _src1);
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribeM(_dataSourceSupplier2, _src2);
            IDataSubscriber<object> subscriber3 = VerifyGetAndSubscribe(_dataSourceSupplier3, _src3);

            object val2 = new object();
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnNewResult(_src2);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src3).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src2,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2,
                DataSourceTestUtils.NOT_FAILED,
                null);

            object val1 = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src2).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1,
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
        /// Highest-quality data source was first to produce result, other data sources got closed.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_I1_S1_C()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribeM(_dataSourceSupplier1, _src1);
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribeM(_dataSourceSupplier2, _src2);
            IDataSubscriber<object> subscriber3 = VerifyGetAndSubscribe(_dataSourceSupplier3, _src3);

            object val1a = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1a,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src3).VerifyMethodInvocation("Close", 1));
            Assert.IsTrue(((MockAbstractDataSource<object>)_src2).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1a,
                DataSourceTestUtils.NOT_FAILED,
                null);

            object val1b = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1b,
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
                val1b,
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
        /// Highest-quality data source was first to produce result, other data sources got closed.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_S1_C()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribeM(_dataSourceSupplier1, _src1);
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribeM(_dataSourceSupplier2, _src2);
            IDataSubscriber<object> subscriber3 = VerifyGetAndSubscribe(_dataSourceSupplier3, _src3);

            object val1b = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1b,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber1.OnNewResult(_src1);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src3).VerifyMethodInvocation("Close", 1));
            Assert.IsTrue(((MockAbstractDataSource<object>)_src2).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src1, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src1,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val1b,
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
        /// Early close with intermediate result.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_I2_C()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribeM(_dataSourceSupplier1, _src1);
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribeM(_dataSourceSupplier2, _src2);
            IDataSubscriber<object> subscriber3 = VerifyGetAndSubscribe(_dataSourceSupplier3, _src3);

            object val2a = new object();
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2a,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnNewResult(_src2);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src3).VerifyMethodInvocation("Close", 1));
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

            TestClose(dataSource, _src1, _src2);
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
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribeM(_dataSourceSupplier1, _src1);
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribeM(_dataSourceSupplier2, _src2);
            IDataSubscriber<object> subscriber3 = VerifyGetAndSubscribe(_dataSourceSupplier3, _src3);

            TestClose(dataSource, _src1, _src2, _src3);
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
        public void TestLifecycle_I2_C_S1()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribeM(_dataSourceSupplier1, _src1);
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribeM(_dataSourceSupplier2, _src2);
            IDataSubscriber<object> subscriber3 = VerifyGetAndSubscribe(_dataSourceSupplier3, _src3);

            object val2a = new object();
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2a,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnNewResult(_src2);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src3).VerifyMethodInvocation("Close", 1));
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

            object val = new object();
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnNewResult(_src2);
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.NO_INTERACTIONS);
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
        public void TestLifecycle_WithoutResult_NI2_NS2_I3_S3_S1_C()
        {
            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribeM(_dataSourceSupplier1, _src1);
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribeM(_dataSourceSupplier2, _src2);
            IDataSubscriber<object> subscriber3 = VerifyGetAndSubscribe(_dataSourceSupplier3, _src3);

            // I2 gets ignored because there is no result
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnNewResult(_src2);
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.NO_INTERACTIONS);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);

            // S2 gets ignored because there is no result
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber2.OnNewResult(_src2);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src2).VerifyMethodInvocation("Close", 1));
            VerifySubscriber(dataSource, _src2, DataSourceTestUtils.NO_INTERACTIONS);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                null,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITHOUT_RESULT,
                null,
                DataSourceTestUtils.NOT_FAILED,
                null);

            object val3a = new object();
            ((MockAbstractDataSource<object>)_src3).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val3a,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber3.OnNewResult(_src3);
            VerifySubscriber(dataSource, _src3, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src3,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val3a,
                DataSourceTestUtils.NOT_FAILED,
                null);

            object val3b = new object();
            ((MockAbstractDataSource<object>)_src3).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val3b,
                DataSourceTestUtils.NOT_FAILED,
                null);
            subscriber3.OnNewResult(_src3);
            VerifySubscriber(dataSource, _src3, DataSourceTestUtils.ON_NEW_RESULT);
            VerifyState(
                ((AbstractDataSource<object>)dataSource),
                _src3,
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val3b,
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
            Assert.IsTrue(((MockAbstractDataSource<object>)_src3).VerifyMethodInvocation("Close", 1));
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
        /// Immediate result of low-res data source followed by delayed result of the 
        /// first data source.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_ImmediateLowRes()
        {
            object val2a = new object();
            ((MockAbstractDataSource<object>)_src2).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2a,
                DataSourceTestUtils.NOT_FAILED,
                null);
            ((MockAbstractDataSource<object>)_src2).RespondOnSubscribe(DataSourceTestUtils.ON_NEW_RESULT);

            IDataSource <object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribeM(_dataSourceSupplier1, _src1);
            IDataSubscriber<object> subscriber2 = VerifyGetAndSubscribeM(_dataSourceSupplier2, _src2);

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
                DataSourceTestUtils.NOT_FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val2b,
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
            Assert.IsTrue(((MockAbstractDataSource<object>)_src2).VerifyMethodInvocation("Close", 1));
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
        /// Immediate finish of the first data source.
        /// </summary>
        [TestMethod]
        public void TestLifecycle_ImmediateFinish()
        {
            object val = new object();
            ((MockAbstractDataSource<object>)_src1).SetState(
                DataSourceTestUtils.NOT_CLOSED,
                DataSourceTestUtils.FINISHED,
                DataSourceTestUtils.WITH_RESULT,
                val,
                DataSourceTestUtils.NOT_FAILED,
                null);
            ((MockAbstractDataSource<object>)_src1).RespondOnSubscribe(DataSourceTestUtils.ON_NEW_RESULT);

            IDataSource<object> dataSource = GetAndSubscribe();
            IDataSubscriber<object> subscriber1 = VerifyGetAndSubscribeM(_dataSourceSupplier1, _src1);

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
    }
}
