using FBCore.Common.Internal;
using FBCore.Common.References;
using FBCore.Concurrency;
using FBCore.DataSource;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImagePipeline.Datasource
{
    /// <summary>
    /// Data source that wraps number of other data sources and waits until
    /// all of them are finished. After that each call to GetResult() returns
    /// list of final results of wrapped data sources.
    /// Caller of GetResult() is responsible for closing all each of the
    /// results separately.
    ///
    /// <para />This data source does not propagate intermediate results.
    /// </summary>
    public class ListDataSource<T> : AbstractDataSource<IList<CloseableReference<T>>>
    {
        private readonly object _gate = new object();

        private readonly IDataSource<CloseableReference<T>>[] _dataSources;
        private int _finishedDataSources;

        /// <summary>
        /// Instantiates the <see cref="ListDataSource{T}"/>
        /// </summary>
        /// <param name="dataSources"></param>
        protected ListDataSource(IDataSource<CloseableReference<T>>[] dataSources)
        {
            _dataSources = dataSources;
            _finishedDataSources = 0;
        }

        /// <summary>
        /// ListDataSource factory method.
        /// </summary>
        public static ListDataSource<T> Create(
            params IDataSource<CloseableReference<T>>[] dataSources)
        {
            Preconditions.CheckNotNull(dataSources);
            Preconditions.CheckState(dataSources.Length > 0);
            ListDataSource<T> listDataSource = new ListDataSource<T>(dataSources);
            foreach (var dataSource in dataSources)
            {
                if (dataSource != null)
                {
                    dataSource.Subscribe(
                        new InternalDataSubscriber(listDataSource),
                        CallerThreadExecutor.Instance);
                }
            }

            return listDataSource;
        }

        /// <summary>
        /// The most recent result of the asynchronous computation.
        /// </summary>
        public override IList<CloseableReference<T>> GetResult()
        {
            lock (_gate)
            {
                if (!HasResult())
                {
                    return null;
                }

                IList<CloseableReference<T>> results = 
                    new List<CloseableReference<T>>(_dataSources.Length);

                foreach (var dataSource in _dataSources)
                {
                    results.Add(dataSource.GetResult());
                }

                return results;
            }
        }

        /// <summary>
        /// Cancels the ongoing request and releases all associated resources.
        /// </summary>
        public override bool Close()
        {
            if (!base.Close())
            {
                return false;
            }

            foreach (var dataSource in _dataSources)
            {
                dataSource.Close();
            }

            return true;
        }

        private void OnDataSourceFinished()
        {
            if (IncreaseAndCheckIfLast())
            {
                SetResult(null, /* isLast */ true);
            }
        }

        private bool IncreaseAndCheckIfLast()
        {
            lock (_gate)
            {
                return ++_finishedDataSources == _dataSources.Length;
            }
        }

        private void OnDataSourceFailed(IDataSource<CloseableReference<T>> dataSource)
        {
            SetFailure(dataSource.GetFailureCause());
        }

        private void OnDataSourceCancelled()
        {
            SetFailure(new Exception("CancellationException"));
        }

        private void OnDataSourceProgress()
        {
            float progress = 0;
            foreach (var dataSource in _dataSources)
            {
                progress += dataSource.GetProgress();
            }

            SetProgress(progress / _dataSources.Length);
        }

        /// <summary>
        /// Checks if any result (possibly of lower quality) is available right now.
        /// </summary>
        /// <returns>
        /// true if any result (possibly of lower quality) is available right now,
        /// false otherwise.
        /// </returns>
        public override bool HasResult()
        {
            lock (_gate)
            {
                return !IsClosed() && (_finishedDataSources == _dataSources.Length);
            }
        }

        private class InternalDataSubscriber : IDataSubscriber<CloseableReference<T>>
        {
            private readonly object _gate = new object();
            bool _finished = false;
            ListDataSource<T> _parent;

            public InternalDataSubscriber(ListDataSource<T> parent)
            {
                _parent = parent;
            }

            private bool TryFinish()
            {
                lock (_gate)
                {
                    if (_finished)
                    {
                        return false;
                    }

                    _finished = true;
                    return true;
                }
            }

            public void OnFailure(IDataSource<CloseableReference<T>> dataSource)
            {
                _parent.OnDataSourceFailed(dataSource);
            }

            public void OnCancellation(IDataSource<CloseableReference<T>> dataSource)
            {
                _parent.OnDataSourceCancelled();
            }

            public Task OnNewResult(IDataSource<CloseableReference<T>> dataSource)
            {
                if (dataSource.IsFinished() && TryFinish())
                {
                    _parent.OnDataSourceFinished();
                }

                return Task.CompletedTask;
            }

            public void OnProgressUpdate(IDataSource<CloseableReference<T>> dataSource)
            {
                _parent.OnDataSourceProgress();
            }
        }
    }
}
