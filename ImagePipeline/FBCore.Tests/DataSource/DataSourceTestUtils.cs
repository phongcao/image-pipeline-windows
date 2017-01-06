using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;

namespace FBCore.Tests.DataSource
{
    /// <summary>
    /// Utils for data source unit tests
    /// </summary>
    class DataSourceTestUtils
    {
        internal const bool CLOSED = true;
        internal const bool NOT_CLOSED = false;
        internal const bool FINISHED = true;
        internal const bool NOT_FINISHED = false;
        internal const bool WITH_RESULT = true;
        internal const bool WITHOUT_RESULT = false;
        internal const bool FAILED = true;
        internal const bool NOT_FAILED = false;
        internal const bool LAST = true;
        internal const bool INTERMEDIATE = false;
        internal const int NO_INTERACTIONS = 0;
        internal const int ON_NEW_RESULT = 1;
        internal const int ON_FAILURE = 2;
        internal const int ON_CANCELLATION = 3;

        internal static void SetState(
            MockAbstractDataSource<object> dataSource,
            bool isClosed,
            bool isFinished,
            bool hasResult,
            object value,
            bool hasFailed,
            Exception failureCause)
        {
            dataSource.SetState(isClosed, isFinished, hasResult, value, hasFailed, failureCause);
        }

        internal static void VerifyState<T>(
            MockAbstractDataSource<T> dataSource,
            bool isClosed,
            bool isFinished,
            bool hasResult,
            T result,
            bool hasFailed,
            Exception failureCause)
        {
            Assert.IsTrue(isClosed == dataSource.IsClosed);
            Assert.IsTrue(isFinished == dataSource.IsFinished);
            Assert.IsTrue(hasResult == dataSource.HasResult);
            Assert.AreSame(result, dataSource.GetResult());
            Assert.IsTrue(hasFailed == dataSource.HasFailed);
            Assert.AreSame(failureCause, dataSource.GetFailureCause());
        }
    }
}
