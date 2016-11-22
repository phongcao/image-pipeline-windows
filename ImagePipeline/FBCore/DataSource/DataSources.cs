using FBCore.Common.Internal;
using System;

namespace FBCore.DataSource
{
    /// <summary>
    /// Static utility methods pertaining to the <see cref="IDataSource{T}"/> interface.
    /// </summary>
    public class DataSources
    {
        private DataSources()
        {
        }

        /// <summary>
        /// Instantiates an immediate failed data source
        /// </summary>
        public static IDataSource<T> ImmediateFailedDataSource<T>(Exception failure)
        {
            SimpleDataSource<T> simpleDataSource = SimpleDataSource<T>.Create();
            simpleDataSource.SetFailure(failure);
            return simpleDataSource;
        }

        /// <summary>
        /// Instantiates an immediate data source
        /// </summary>
        public static IDataSource<T> ImmediateDataSource<T>(T result)
        {
            SimpleDataSource<T> simpleDataSource = SimpleDataSource<T>.Create();
            simpleDataSource.SetResult(result);
            return simpleDataSource;
        }

        /// <summary>
        /// Gets the failed data source supplier of the given failure
        /// </summary>
        public static ISupplier<IDataSource<T>> GetFailedDataSourceSupplier<T>(Exception failure)
        {
            return new SupplierImpl<IDataSource<T>>(() =>
            {
                return ImmediateFailedDataSource<T>(failure);
            });
        }
    }
}
