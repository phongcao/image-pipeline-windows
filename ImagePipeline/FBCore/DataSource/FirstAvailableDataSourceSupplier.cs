using FBCore.Common.Internal;
using FBCore.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FBCore.DataSource
{
    /// <summary>
    /// <see cref="IDataSource{T}" /> supplier that provides a data source 
    /// which will forward results of the first available data source.
    ///
    /// <para />Data sources are obtained in order. Only if the current data 
    /// source fails, or if it finishes without result, the next one will be tried.
    /// </summary>
    public class FirstAvailableDataSourceSupplier<T> : ISupplier<IDataSource<T>>
    {
        private readonly IList<ISupplier<IDataSource<T>>> _dataSourceSuppliers;

        private FirstAvailableDataSourceSupplier(IList<ISupplier<IDataSource<T>>> dataSourceSuppliers)
        {
            Preconditions.CheckArgument(dataSourceSuppliers.Count != 0, "List of suppliers is empty!");
            _dataSourceSuppliers = dataSourceSuppliers;
        }

        /// <summary>
        /// Creates the <see cref="FirstAvailableDataSourceSupplier{T}"/>
        /// </summary>
        /// <param name="dataSourceSuppliers"></param>
        /// <returns></returns>
        public static FirstAvailableDataSourceSupplier<T> Create(
            IList<ISupplier<IDataSource<T>>> dataSourceSuppliers)
        {
            return new FirstAvailableDataSourceSupplier<T>(dataSourceSuppliers);
        }

        /// <summary>
        /// Gets the first available data source
        /// </summary>
        /// <returns></returns>
        public IDataSource<T> Get()
        {
            return new FirstAvailableDataSource(_dataSourceSuppliers);
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
        /// Custom Equals method
        /// </summary>
        /// <param name="other"></param>
        public override bool Equals(object other)
        {
            if (other == this)
            {
                return true;
            }

            if (other.GetType() != typeof(FirstAvailableDataSourceSupplier<T>))
            {
                return false;
            }
            
            FirstAvailableDataSourceSupplier<T> that = (FirstAvailableDataSourceSupplier<T>)other;
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

        private class FirstAvailableDataSource : AbstractDataSource<T>
        {
            private readonly object _gate = new object();

            private readonly IList<ISupplier<IDataSource<T>>> _dataSourceSuppliers;
            private int _index = 0;
            private IDataSource<T> _currentDataSource = default(IDataSource<T>);
            private IDataSource<T> _dataSourceWithResult = default(IDataSource<T>);

            public FirstAvailableDataSource(IList<ISupplier<IDataSource<T>>> dataSourceSuppliers)
            {
                _dataSourceSuppliers = dataSourceSuppliers;

                if (!StartNextDataSource())
                {
                    base.SetFailure(new Exception("No data source supplier or supplier returned null."));
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
                IDataSource<T> currentDataSource;
                IDataSource<T> dataSourceWithResult;
                lock (_gate)
                {
                    // it's fine to call <code> base.Close()</code> within a 
                    // synchronized block because we don't implement <see cref="CloseResult()"/>, 
                    // but perform result closing ourselves.
                    if (!base.Close())
                    {
                        return false;
                    }

                    currentDataSource = _currentDataSource;
                    _currentDataSource = default(IDataSource<T>);
                    dataSourceWithResult = _dataSourceWithResult;
                    _dataSourceWithResult = default(IDataSource<T>);
                }

                CloseSafely(dataSourceWithResult);
                CloseSafely(currentDataSource);
                return true;
            }

            private bool StartNextDataSource()
            {
                ISupplier<IDataSource<T>> dataSourceSupplier = GetNextSupplier();
                IDataSource<T> dataSource = (dataSourceSupplier != default(ISupplier<IDataSource<T>>)) ? 
                    dataSourceSupplier.Get() : 
                    default(IDataSource<T>);

                if (SetCurrentDataSource(dataSource) && dataSource != default(IDataSource<T>))
                {
                    dataSource.Subscribe(
                        new InternalDataSubscriber(this), 
                        CallerThreadExecutor.Instance);

                    return true;
                }
                else
                {
                    CloseSafely(dataSource);
                    return false;
                }
            }

            private ISupplier<IDataSource<T>> GetNextSupplier()
            {
                lock (_gate)
                {
                    if (!base.IsClosed() && _index < _dataSourceSuppliers.Count)
                    {
                        return _dataSourceSuppliers[_index++];
                    }

                    return default(ISupplier<IDataSource<T>>);
                }
            }

            private bool SetCurrentDataSource(IDataSource<T> dataSource)
            {
                lock (_gate)
                {
                    if (base.IsClosed())
                    {
                        return false;
                    }

                    _currentDataSource = dataSource;
                    return true;
                }
            }

            private bool ClearCurrentDataSource(IDataSource<T> dataSource)
            {
                lock (_gate)
                {
                    if (base.IsClosed() || dataSource != _currentDataSource)
                    {
                        return false;
                    }

                    _currentDataSource = default(IDataSource<T>);
                    return true;
                }
            }

            private IDataSource<T> GetDataSourceWithResult()
            {
                lock (_gate)
                {
                    return _dataSourceWithResult;
                }
            }

            private void MaybeSetDataSourceWithResult(
                IDataSource<T> dataSource,
                bool isFinished)
            {
                IDataSource<T> oldDataSource = default(IDataSource<T>);
                lock (_gate)
                {
                    if (dataSource != _currentDataSource || dataSource == _dataSourceWithResult)
                    {
                        return;
                    }

                    // If we didn't have any result so far, we got one now, so we'll set
                    // <code> _dataSourceWithResult</code> to point to the current data source.
                    // If we did have a result which came from another data source,
                    // we'll only set <code> _dataSourceWithResult</code> to point to the current
                    // data source if it has finished (i.e. the new result is final).
                    if (_dataSourceWithResult == default(IDataSource<T>) || isFinished)
                    {
                        oldDataSource = _dataSourceWithResult;
                        _dataSourceWithResult = dataSource;
                    }
                }

                CloseSafely(oldDataSource);
            }

            private void OnDataSourceFailed(IDataSource<T> dataSource)
            {
                if (!ClearCurrentDataSource(dataSource))
                {
                    return;
                }

                if (dataSource != GetDataSourceWithResult())
                {
                    CloseSafely(dataSource);
                }

                if (!StartNextDataSource())
                {
                    SetFailure(dataSource.GetFailureCause());
                }
            }

            private void OnDataSourceNewResult(IDataSource<T> dataSource)
            {
                MaybeSetDataSourceWithResult(dataSource, dataSource.IsFinished());

                // If the data source with the new result is our <code> _dataSourceWithResult</code>,
                // we have to notify our subscribers about the new result.
                if (dataSource == GetDataSourceWithResult())
                {
                    SetResult(default(T), dataSource.IsFinished());
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
                FirstAvailableDataSource _parent;

                public InternalDataSubscriber(FirstAvailableDataSource parent)
                {
                    _parent = parent;
                }

                public void OnFailure(IDataSource<T> dataSource)
                {
                    _parent.OnDataSourceFailed(dataSource);
                }

                public void OnCancellation(IDataSource<T> dataSource)
                {
                }

                public void OnNewResult(IDataSource<T> dataSource)
                {
                    if (dataSource.HasResult())
                    {
                        _parent.OnDataSourceNewResult(dataSource);
                    }
                    else if (dataSource.IsFinished())
                    {
                        _parent.OnDataSourceFailed(dataSource);
                    }
                }

                public void OnProgressUpdate(IDataSource<T> dataSource)
                {
                    float oldProgress = _parent.GetProgress();
                    _parent.SetProgress(Math.Max(oldProgress, dataSource.GetProgress()));
                }
            }
        }
    }
}
