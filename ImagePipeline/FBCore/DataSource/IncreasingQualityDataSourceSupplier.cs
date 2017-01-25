using FBCore.Common.Internal;
using FBCore.Concurrency;
using System.Collections.Generic;
using System.Linq;

namespace FBCore.DataSource
{
    /// <summary>
    /// <see cref="IDataSource{T}" /> supplier that provides a data source 
    /// which forwards results of the underlying data sources with the 
    /// increasing quality.
    ///
    /// <para />Data sources are obtained in order. The first data source 
    /// in array is considered to be of the highest quality. The first data 
    /// source to provide an result gets forwarded until one of the higher 
    /// quality data sources provides its final image at which point that data 
    /// source gets forwarded (and so on). That being said, only the first data 
    /// source to provide an result is streamed.
    ///
    /// <para />Outcome (success/failure) of the data source provided by this 
    /// supplier is determined by the outcome of the highest quality data 
    /// source (the first data source in the array).
    /// </summary>
    public class IncreasingQualityDataSourceSupplier<T> : ISupplier<IDataSource<T>>
    {
        private readonly IList<ISupplier<IDataSource<T>>> _dataSourceSuppliers;

        private IncreasingQualityDataSourceSupplier(IList<ISupplier<IDataSource<T>>> dataSourceSuppliers)
        {
            Preconditions.CheckArgument(dataSourceSuppliers.Count != 0, "List of suppliers is empty!");
            _dataSourceSuppliers = dataSourceSuppliers;
        }

        /// <summary>
        /// Creates a new data source supplier with increasing-quality strategy.
        /// <para />Note: for performance reasons the list doesn't get cloned, 
        /// so the caller of this method should not modify the list once passed in here.
        /// <param name="dataSourceSuppliers">list of underlying suppliers</param>
        /// </summary>
        public static IncreasingQualityDataSourceSupplier<T> Create(
            IList<ISupplier<IDataSource<T>>> dataSourceSuppliers)
        {
            return new IncreasingQualityDataSourceSupplier<T>(dataSourceSuppliers);
        }

        /// <summary>
        /// Gets the increasing quality data source
        /// </summary>
        /// <returns></returns>
        public IDataSource<T> Get()
        {
            return new IncreasingQualityDataSource(_dataSourceSuppliers);
        }

        /// <summary>
        /// Custom GetHashCode method
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _dataSourceSuppliers.GetHashCode();
        }

        /// <summary>
        /// Compares with other IncreasingQualityDataSourceSupplier objects
        /// </summary>
        /// <param name="other"></param>
        public override bool Equals(object other)
        {
            if (other == this)
            {
                return true;
            }

            if (other.GetType() != typeof(IncreasingQualityDataSourceSupplier<T>))
            {
                return false;
            }

            IncreasingQualityDataSourceSupplier<T> that = (IncreasingQualityDataSourceSupplier<T>)other;
            return _dataSourceSuppliers.SequenceEqual(that._dataSourceSuppliers);
        }

        /// <summary>
        /// Custom ToString method
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{ base.ToString() }{{list={ _dataSourceSuppliers.ToString() }}}";
        }

        private class IncreasingQualityDataSource : AbstractDataSource<T>
        {
            private readonly object _gate = new object();

            private readonly IList<ISupplier<IDataSource<T>>> _dataSourceSuppliers;
            private IList<IDataSource<T>> _dataSources;
            private int _indexOfDataSourceWithResult;

            public IncreasingQualityDataSource(IList<ISupplier<IDataSource<T>>> dataSourceSuppliers)
            {
                _dataSourceSuppliers = dataSourceSuppliers;
                int n = _dataSourceSuppliers.Count;
                _indexOfDataSourceWithResult = n;
                _dataSources = new List<IDataSource<T>>(n);
                for (int i = 0; i < n; i++)
                {
                    IDataSource<T> dataSource = _dataSourceSuppliers[i].Get();
                    _dataSources.Add(dataSource);
                    dataSource.Subscribe(
                        new InternalDataSubscriber(this, i), 
                        CallerThreadExecutor.Instance);
                    // there's no point in creating data sources of lower quality
                    // if the data source of a higher quality has some result already
                    if (dataSource.HasResult())
                    {
                        break;
                    }
                }
            }

            private IDataSource<T> GetDataSource(int i)
            {
                lock (_gate)
                {
                    return (_dataSources != default(IList<IDataSource<T>>) && i < _dataSources.Count) ? 
                        _dataSources[i] : 
                        default(IDataSource<T>);
                }
            }

            private IDataSource<T> GetAndClearDataSource(int i)
            {
                lock (_gate)
                {
                    IDataSource<T> dataSource = default(IDataSource<T>);

                    if (_dataSources != default(IList<IDataSource<T>>) && i < _dataSources.Count)
                    {
                        dataSource = _dataSources[i];
                        _dataSources[i] = default(IDataSource<T>);
                    }

                    return dataSource;
                }
            }

            private IDataSource<T> GetDataSourceWithResult()
            {
                lock (_gate)
                {
                    return GetDataSource(_indexOfDataSourceWithResult);
                }
            }

            public override T GetResult()
            {
                lock (_gate)
                {
                    IDataSource<T> dataSourceWithResult = GetDataSourceWithResult();
                    return (dataSourceWithResult != default(IDataSource<T>)) ?
                        dataSourceWithResult.GetResult() :
                        default(T);
                }
            }

            public override bool HasResult()
            {
                lock (_gate)
                {
                    IDataSource<T> dataSourceWithResult = GetDataSourceWithResult();
                    return (dataSourceWithResult != default(IDataSource<T>)) &&
                        dataSourceWithResult.HasResult();
                }
            }

            public override bool Close()
            {
                IList<IDataSource<T>> dataSources;
                lock (_gate)
                {
                    // it's fine to call <code> base.Close()</code> within a 
                    // synchronized block because we don't implement <see cref="CloseResult()"/>, 
                    // but perform result closing ourselves.
                    if (!base.Close())
                    {
                        return false;
                    }

                    dataSources = _dataSources;
                    _dataSources = default(IList<IDataSource<T>>);
                }

                if (dataSources != default(IList<IDataSource<T>>))
                {
                    foreach (var dataSource in dataSources)
                    {
                        CloseSafely(dataSource);
                    }
                }

                return true;
            }

            private void OnDataSourceNewResult(int index, IDataSource<T> dataSource)
            {
                MaybeSetIndexOfDataSourceWithResult(index, dataSource, dataSource.IsFinished());

                // If the data source with the new result is our <code> mIndexOfDataSourceWithResult</code>,
                // we have to notify our subscribers about the new result.
                if (dataSource == GetDataSourceWithResult())
                {
                    SetResult(default(T), (index == 0) && dataSource.IsFinished());
                }
            }

            private void OnDataSourceFailed(int index, IDataSource<T> dataSource)
            {
                CloseSafely(TryGetAndClearDataSource(index, dataSource));
                if (index == 0)
                {
                    SetFailure(dataSource.GetFailureCause());
                }
            }

            private void MaybeSetIndexOfDataSourceWithResult(
                int index,
                IDataSource<T> dataSource,
                bool isFinished)
            {
                int oldIndexOfDataSourceWithResult;
                int newIndexOfDataSourceWithResult;
                lock (_gate)
                {
                    oldIndexOfDataSourceWithResult = _indexOfDataSourceWithResult;
                    newIndexOfDataSourceWithResult = _indexOfDataSourceWithResult;
                    if (dataSource != GetDataSource(index) || index == _indexOfDataSourceWithResult)
                    {
                        return;
                    }

                    // If we didn't have any result so far, we got one now, so we'll set
                    // <code> _indexOfDataSourceWithResult</code> to point to the data source with result.
                    // If we did have a result which came from another data source,
                    // we'll only set <code> _indexOfDataSourceWithResult</code> to point to the current data source
                    // if it has finished (i.e. the new result is final), and is of higher quality.
                    if (GetDataSourceWithResult() == default(IDataSource<T>) ||
                        (isFinished && index < _indexOfDataSourceWithResult))
                    {
                        newIndexOfDataSourceWithResult = index;
                        _indexOfDataSourceWithResult = index;
                    }
                }

                // Close data sources of lower quality than the one with the result
                for (int i = oldIndexOfDataSourceWithResult; i > newIndexOfDataSourceWithResult; i--)
                {
                    CloseSafely(GetAndClearDataSource(i));
                }
            }

            private IDataSource<T> TryGetAndClearDataSource(int i, IDataSource<T> dataSource)
            {
                lock (_gate)
                {
                    if (dataSource == GetDataSourceWithResult())
                    {
                        return default(IDataSource<T>);
                    }

                    if (dataSource == GetDataSource(i))
                    {
                        return GetAndClearDataSource(i);
                    }

                    return dataSource;
                }
            }

            private void CloseSafely(IDataSource<T> dataSource)
            {
                if (dataSource != default(IDataSource<T>))
                {
                    dataSource.Close();
                }
            }

            private class InternalDataSubscriber : IDataSubscriber<T>
            {
                IncreasingQualityDataSource _parent;
                private int _index;

                public InternalDataSubscriber(IncreasingQualityDataSource parent, int index)
                {
                    _parent = parent;
                    _index = index;
                }

                public void OnFailure(IDataSource<T> dataSource)
                {
                    _parent.OnDataSourceFailed(_index, dataSource);
                }

                public void OnCancellation(IDataSource<T> dataSource)
                {
                }

                public void OnNewResult(IDataSource<T> dataSource)
                {
                    if (dataSource.HasResult())
                    {
                        _parent.OnDataSourceNewResult(_index, dataSource);
                    }
                    else if (dataSource.IsFinished())
                    {
                        _parent.OnDataSourceFailed(_index, dataSource);
                    }
                }

                public void OnProgressUpdate(IDataSource<T> dataSource)
                {
                    if (_index == 0)
                    {
                        _parent.SetProgress(dataSource.GetProgress());
                    }
                }
            }
        }
    }
}
