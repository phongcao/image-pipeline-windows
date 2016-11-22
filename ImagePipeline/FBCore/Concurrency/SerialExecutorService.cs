using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.System.Threading;

namespace FBCore.Concurrency
{
    /// <summary>
    /// Provides default implementations of IExecutorService execution methods
    /// using <see cref="LimitedConcurrencyTaskScheduler"/>
    /// </summary>
    public class SerialExecutorService : IExecutorService, IDisposable
    {
        private readonly object _gate = new object();
        private int _disposed;

        private readonly string _name;
        private readonly Action<Exception> _handler;
        private readonly TaskScheduler _taskScheduler;
        private readonly TaskFactory _taskFactory;

        /// <summary>
        /// Instantiates the <see cref="SerialExecutorService"/>.
        /// </summary>
        /// <param name="name">The name of the executor.</param>
        /// <param name="maxDegreeOfParallelism">The degrees of parallelism.</param>
        /// <param name="priority">The priority of the work item relative to work items 
        /// in the thread pool. The value of this parameter can be Low, Normal, or High.</param>
        /// <param name="handler">The exception handler.</param>
        internal SerialExecutorService(
            string name,
            int maxDegreeOfParallelism,
            WorkItemPriority priority,
            Action<Exception> handler)
        {
            if (maxDegreeOfParallelism < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _name = name;
            _taskScheduler = new LimitedConcurrencyTaskScheduler(maxDegreeOfParallelism, priority);
            _taskFactory = new TaskFactory(_taskScheduler);
            _handler = handler;
        }

        /// <summary>
        /// Flags if the <see cref="SerialExecutorService"/> is disposed.
        /// </summary>
        protected bool IsDisposed
        {
            get
            {
                return _disposed > 0;
            }
        }

        /// <summary>
        /// Queues an action to run.
        /// </summary>
        /// <param name="action">The action.</param>
        public Task Execute(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (IsDisposed)
            {
                Debug.WriteLine($"Dropping enqueued action on disposed '{_name}' thread.");
                return Task.CompletedTask;
            }

            return _taskFactory.StartNew(() =>
            {
                try
                {
                    lock (_gate)
                    {
                        if (!IsDisposed)
                        {
                            action();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _handler(ex);
                }
            });
        }

        /// <summary>
        /// Queues a function to run.
        /// </summary>
        /// <typeparam name="T">Type of response.</typeparam>
        /// <param name="func">The function.</param>
        public Task<T> Execute<T>(Func<T> func)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            if (IsDisposed)
            {
                Debug.WriteLine($"Dropping enqueued action on disposed '{_name}' thread.");
                return Task.FromResult(default(T));
            }

            return _taskFactory.StartNew(() =>
            {
                try
                {
                    lock (_gate)
                    {
                        if (!IsDisposed)
                        {
                            return func();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _handler(ex);
                }

                return default(T);
            });
        }

        /// <summary>
        /// Disposes the action queue.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Disposes the action queue.
        /// </summary>
        /// <param name="disposing">
        /// <b>false</b> if dispose was triggered by a finalizer, <b>true</b>
        /// otherwise.
        /// </param>
        private void Dispose(bool disposing)
        {
            // Warning: will deadlock if disposed from own queue thread.
            lock (_gate)
            {
                if (disposing)
                {
                    Interlocked.Increment(ref _disposed);
                }
            }
        }
    }
}
