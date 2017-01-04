using FBCore.Common.Internal;
using FBCore.Concurrency;
using FBCore.DataSource;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;

namespace FBCore.Tests.DataSource
{
    [TestClass]
    class AbstractDataSourceSupplier
    {
        protected IDataSource<object> _src1;
        protected IDataSource<object> _src2;
        protected IDataSource<object> _src3;
        protected ISupplier<IDataSource<object>> _dataSourceSupplier1;
        protected ISupplier<IDataSource<object>> _dataSourceSupplier2;
        protected ISupplier<IDataSource<object>> _dataSourceSupplier3;
        protected IDataSubscriber<object> _dataSubscriber;
        protected IExecutorService _executor;
        protected IList<ISupplier<IDataSource<object>>> _suppliers;
        protected ISupplier<IDataSource<object>> _dataSourceSupplier = default(ISupplier<IDataSource<object>>);

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _src1 = new MockDataSource<object>();
            _src2 = new MockDataSource<object>();
            _src3 = new MockDataSource<object>();
            _dataSourceSupplier1 = new SupplierImpl<IDataSource<object>>(() =>
            {
                return _src1;
            });
            _dataSourceSupplier2 = new SupplierImpl<IDataSource<object>>(() =>
            {
                return _src2;
            });
            _dataSourceSupplier3 = new SupplierImpl<IDataSource<object>>(() =>
            {
                return _src3;
            });
            _dataSubscriber = new BaseDataSubscriberImpl<object>(
                _ => {}, __ => {});
            _executor = CallerThreadExecutor.Instance;
            _suppliers = new List<ISupplier<IDataSource<object>>>(3);
            _suppliers.Add(_dataSourceSupplier1);
            _suppliers.Add(_dataSourceSupplier2);
            _suppliers.Add(_dataSourceSupplier3);
        }

        protected void VerifyNoMoreInteractionsAll()
        {
            VerifyOptionals((MockDataSource<object>)_src1);
            VerifyOptionals((MockDataSource<object>)_src2);
            VerifyOptionals((MockDataSource<object>)_src3);
            ((MockDataSource<object>)_src1).VerifyNoMoreInteraction();
            ((MockDataSource<object>)_src2).VerifyNoMoreInteraction();
            ((MockDataSource<object>)_src3).VerifyNoMoreInteraction();
        }

        protected void VerifyOptionals(MockDataSource<object> underlyingDataSource)
        {
            underlyingDataSource.VerifyMethodInvocationOrder("IsFinished", 0);
            underlyingDataSource.VerifyMethodInvocationOrder("HasResult", 1);
            underlyingDataSource.VerifyMethodInvocationOrder("HasFailed", 2);
            underlyingDataSource.VerifyMethodInvocation("IsFinished", 0);
            underlyingDataSource.VerifyMethodInvocation("HasResult", 0);
            underlyingDataSource.VerifyMethodInvocation("HasFailed", 0);
        }

        /// <summary>
        /// Verifies that our _dataSourceSupplier got underlying data source and subscribed to it.
        /// Subscriber is returned.
        /// </summary>
        protected IDataSubscriber<object> VerifyGetAndSubscribe(
            ISupplier<IDataSource<object>> dataSourceSupplier,
            IDataSource<object> underlyingDataSource,
            bool expectMoreInteractions)
        {
            Assert.IsTrue(((SupplierImpl<IDataSource<object>>)dataSourceSupplier).GetCallCount == 1);
            Assert.IsTrue(((MockDataSource<object>)underlyingDataSource).DataSubscriber != null);
            if (!expectMoreInteractions)
            {
                VerifyNoMoreInteractionsAll();
            }

            return ((MockDataSource<object>)underlyingDataSource).DataSubscriber;
        }

        protected IDataSubscriber<object> VerifyGetAndSubscribe(
            ISupplier<IDataSource<object>> dataSourceSupplier,
            IDataSource<object> underlyingDataSource)
        {
            return VerifyGetAndSubscribe(dataSourceSupplier, underlyingDataSource, false);
        }

        protected IDataSubscriber<object> VerifyGetAndSubscribeM(
            ISupplier<IDataSource<object>> dataSourceSupplier,
            IDataSource<object> underlyingDataSource)
        {
            return VerifyGetAndSubscribe(dataSourceSupplier, underlyingDataSource, true);
        }

        /// <summary>
        /// Verifies that data source provided by our mDataSourceSupplier notified mDataSubscriber.
        /// </summary>
        protected void VerifySubscriber(
            IDataSource<object> dataSource,
            IDataSource<object> underlyingDataSource,
            int expected)
        {
            switch (expected)
            {
                case DataSourceTestUtils.NO_INTERACTIONS:
                    VerifyNoMoreInteractionsAll();
                    break;

                case DataSourceTestUtils.ON_NEW_RESULT:
                    Assert.IsTrue(((BaseDataSubscriber<object>)_dataSubscriber).OnNewResultCallCount == 1);
                    Assert.AreSame(dataSource, ((BaseDataSubscriber<object>)_dataSubscriber).DataSource);
                    VerifyNoMoreInteractionsAll();
                    break;

                case DataSourceTestUtils.ON_FAILURE:
                    Assert.IsTrue(((BaseDataSubscriber<object>)_dataSubscriber).OnNewResultCallCount == 1);
                    Assert.AreSame(dataSource, ((BaseDataSubscriber<object>)_dataSubscriber).DataSource);
                    ((MockDataSource<object>)underlyingDataSource).VerifyMethodInvocationOrder(
                        "GetFailureCause", 0);
                    Assert.IsTrue(((BaseDataSubscriber<object>)_dataSubscriber).OnFailureCallCount == 1);
                    Assert.AreSame(dataSource, ((BaseDataSubscriber<object>)_dataSubscriber).DataSource);
                    VerifyNoMoreInteractionsAll();
                    break;

                case DataSourceTestUtils.ON_CANCELLATION:
                    Assert.IsTrue(((BaseDataSubscriber<object>)_dataSubscriber).OnCancellationCallCount == 1);
                    Assert.AreSame(dataSource, ((BaseDataSubscriber<object>)_dataSubscriber).DataSource);
                    VerifyNoMoreInteractionsAll();
                    break;
            }
        }

        /// <summary>
        /// Verifies the state of the data source provided by our mDataSourceSupplier.
        /// </summary>
        protected void VerifyState(
            IDataSource<object> dataSource,
            IDataSource<object> dataSourceWithResult,
            bool isClosed,
            bool isFinished,
            bool hasResult,
            object result,
            bool hasFailed,
            Exception failureCause)
        {
            DataSourceTestUtils.VerifyState(
                (MockDataSource<object>)dataSource, 
                isClosed, 
                isFinished, 
                hasResult, 
                result, 
                hasFailed, 
                failureCause);

            // DataSourceTestUtils.VerifyState will call dataSource.GetResult() which should forward to
            // underlyingDataSource.GetResult()
            if (dataSourceWithResult != null)
            {
                ((MockDataSource<object>)dataSourceWithResult).VerifyMethodInvocationOrder("GetResult", 0);
            }

            VerifyNoMoreInteractionsAll();
        }

        /// <summary>
        /// Verifies that the underlying data sources get closed when data source provided by
        /// our _dataSourceSupplier gets closed.
        /// </summary>
        protected void TestClose(
            IDataSource<object> dataSource,
            params IDataSource<object>[] underlyingDataSources)
        {
            dataSource.Close();
            if (underlyingDataSources != null)
            {
                foreach (var underlyingDataSource in underlyingDataSources)
                {
                    ((MockDataSource<object>)underlyingDataSource).VerifyMethodInvocationOrder("Close", 0);
                }
            }
        }

        /// <summary>
        /// Gets data source from our _dataSourceSupplier and subscribes _dataSubscriber to it.
        /// Obtained data source is returned.
        /// </summary>
        protected IDataSource<object> GetAndSubscribe()
        {
            IDataSource<object> dataSource = _dataSourceSupplier.Get();
            dataSource.Subscribe(_dataSubscriber, _executor);
            return dataSource;
        }

        /// <summary>
        /// Schedule response on subscribe.
        /// </summary>
        protected static void RespondOnSubscribe<T>(
            IDataSource<T> dataSource,
            int response)
        {
            ((MockDataSource<T>)dataSource).RespondOnSubscribe(response);
        }
    }
}
