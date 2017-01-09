using FBCore.Common.Internal;
using FBCore.Concurrency;
using FBCore.DataSource;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;

namespace FBCore.Tests.DataSource
{
    /// <summary>
    /// Abstract data source supplier
    /// </summary>
    public class AbstractDataSourceSupplier
    {
        /// <summary>
        /// Mock data
        /// </summary>
        protected IDataSource<object> _src1;

        /// <summary>
        /// Mock data
        /// </summary>
        protected IDataSource<object> _src2;

        /// <summary>
        /// Mock data
        /// </summary>
        protected IDataSource<object> _src3;

        /// <summary>
        /// Mock data
        /// </summary>
        protected ISupplier<IDataSource<object>> _dataSourceSupplier1;

        /// <summary>
        /// Mock data
        /// </summary>
        protected ISupplier<IDataSource<object>> _dataSourceSupplier2;

        /// <summary>
        /// Mock data
        /// </summary>
        protected ISupplier<IDataSource<object>> _dataSourceSupplier3;

        /// <summary>
        /// Mock data
        /// </summary>
        protected IDataSubscriber<object> _dataSubscriber;

        /// <summary>
        /// Mock data
        /// </summary>
        protected IExecutorService _executor;

        /// <summary>
        /// Mock data
        /// </summary>
        protected IList<ISupplier<IDataSource<object>>> _suppliers;

        /// <summary>
        /// Mock data
        /// </summary>
        protected ISupplier<IDataSource<object>> _dataSourceSupplier = 
            default(ISupplier<IDataSource<object>>);

        /// <summary>
        /// Initialize
        /// </summary>
        public virtual void Initialize()
        {
            _src1 = new MockAbstractDataSource<object>();
            _src2 = new MockAbstractDataSource<object>();
            _src3 = new MockAbstractDataSource<object>();
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
            _dataSubscriber = new MockDataSubscriber<object>();
            _executor = CallerThreadExecutor.Instance;
            _suppliers = new List<ISupplier<IDataSource<object>>>(3);
            _suppliers.Add(_dataSourceSupplier1);
            _suppliers.Add(_dataSourceSupplier2);
            _suppliers.Add(_dataSourceSupplier3);
        }

        /// <summary>
        /// Verifies that there is no more method invocation
        /// </summary>
        protected void VerifyNoMoreInteractionsAll()
        {
            VerifyOptionals((MockAbstractDataSource<object>)_src1);
            VerifyOptionals((MockAbstractDataSource<object>)_src2);
            VerifyOptionals((MockAbstractDataSource<object>)_src3);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src1).HasNoMoreInteraction);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src2).HasNoMoreInteraction);
            Assert.IsTrue(((MockAbstractDataSource<object>)_src3).HasNoMoreInteraction);
        }

        /// <summary>
        /// Verifies method invocation order and number of invocations
        /// </summary>
        /// <param name="underlyingDataSource"></param>
        protected void VerifyOptionals(MockAbstractDataSource<object> underlyingDataSource)
        {
            underlyingDataSource.VerifyMethodInvocationOrder("IsClosed", 1);
            underlyingDataSource.VerifyMethodInvocationOrder("IsFinished", 2);
            underlyingDataSource.VerifyMethodInvocationOrder("HasResult", 3);
            underlyingDataSource.VerifyMethodInvocationOrder("HasFailed", 4);
            underlyingDataSource.VerifyMethodInvocation("IsClosed", 0);
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
            Assert.IsTrue(((MockAbstractDataSource<object>)underlyingDataSource).VerifyMethodInvocation(
                "Subscribe", 1));
            if (!expectMoreInteractions)
            {
                VerifyNoMoreInteractionsAll();
            }

            return ((MockAbstractDataSource<object>)underlyingDataSource).DataSubscriber;
        }

        /// <summary>
        /// Verifies that data source provided by our _dataSourceSupplier notified _dataSubscriber.
        /// </summary>
        protected IDataSubscriber<object> VerifyGetAndSubscribe(
            ISupplier<IDataSource<object>> dataSourceSupplier,
            IDataSource<object> underlyingDataSource)
        {
            return VerifyGetAndSubscribe(dataSourceSupplier, underlyingDataSource, false);
        }

        /// <summary>
        /// Verifies that data source provided by our _dataSourceSupplier notified _dataSubscriber.
        /// </summary>
        protected IDataSubscriber<object> VerifyGetAndSubscribeM(
            ISupplier<IDataSource<object>> dataSourceSupplier,
            IDataSource<object> underlyingDataSource)
        {
            return VerifyGetAndSubscribe(dataSourceSupplier, underlyingDataSource, true);
        }

        /// <summary>
        /// Verifies that data source provided by our _dataSourceSupplier notified _dataSubscriber.
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
                    Assert.IsTrue(((MockDataSubscriber<object>)_dataSubscriber).OnNewResultCallCount != 0);
                    Assert.AreSame(dataSource, ((MockDataSubscriber<object>)_dataSubscriber).DataSource);
                    VerifyNoMoreInteractionsAll();
                    break;

                case DataSourceTestUtils.ON_FAILURE:
                    ((MockAbstractDataSource<object>)underlyingDataSource).VerifyMethodInvocation(
                        "GetFailureCause", 1);
                    Assert.IsTrue(((MockDataSubscriber<object>)_dataSubscriber).OnFailureCallCount != 0);
                    Assert.AreSame(dataSource, ((MockDataSubscriber<object>)_dataSubscriber).DataSource);
                    VerifyNoMoreInteractionsAll();
                    break;

                case DataSourceTestUtils.ON_CANCELLATION:
                    Assert.IsTrue(((MockDataSubscriber<object>)_dataSubscriber).OnCancellationCallCount != 0);
                    Assert.AreSame(dataSource, ((MockDataSubscriber<object>)_dataSubscriber).DataSource);
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
                (AbstractDataSource<object>)dataSource, 
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
                ((MockAbstractDataSource<object>)dataSourceWithResult).VerifyMethodInvocation(
                    "GetResult", 1);
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
                    ((MockAbstractDataSource<object>)underlyingDataSource).VerifyMethodInvocation(
                        "Close", 1);
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
            ((MockAbstractDataSource<T>)dataSource).RespondOnSubscribe(response);
        }
    }
}
