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
    public class SerialExecutorService : IScheduledExecutorService, IDisposable
    {
        /// <summary>
        /// Lock
        /// </summary>
        protected readonly object _gate = new object();

        /// <summary>
        /// Dispose flag
        /// </summary>
        protected int _disposed;

        /// <summary>
        /// The name of the executor
        /// </summary>
        protected readonly string _name;

        /// <summary>
        /// The exception handler
        /// </summary>
        protected readonly Action<Exception> _handler;

        /// <summary>
        /// The task scheduler
        /// </summary>
        protected readonly TaskScheduler _taskScheduler;

        /// <summary>
        /// The task factory
        /// </summary>
        protected readonly TaskFactory _taskFactory;

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
                return Volatile.Read(ref _disposed) > 0;
            }
        }

        /// <summary>
        /// Queues an action to run.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="token">The cancellation token.</param>
        public virtual Task Execute(Action action, CancellationToken token)
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
                        if (token != CancellationToken.None)
                        {
                            token.ThrowIfCancellationRequested();
                        }

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
        /// Queues an action to run.
        /// </summary>
        /// <param name="action">The action.</param>
        public Task Execute(Action action)
        {
            return Execute(action, CancellationToken.None);
        }

        /// <summary>
        /// Creates and executes a one-shot action that becomes enabled after the given delay.
        /// </summary>
        /// <remarks>
        /// The action will be submitted to the end of the event queue
        /// even if it is being submitted from the same queue Thread.
        /// </remarks>
        /// <param name="action">The action.</param>
        /// <param name="delay">The delay in milliseconds.</param>
        public Task Schedule(Action action, long delay)
        {
            return Task.Delay((int)delay).ContinueWith(
                _ => Execute(action, CancellationToken.None));
        }

        /// <summary>
        /// Creates and executes a one-shot action that becomes enabled after the given delay.
        /// </summary>
        /// <remarks>
        /// The action will be submitted to the end of the event queue
        /// even if it is being submitted from the same queue Thread.
        /// </remarks>
        /// <param name="action">The action.</param>
        /// <param name="delay">The delay in milliseconds.</param>
        /// <param name="token">The cancellation token.</param>
        public Task Schedule(Action action, long delay, CancellationToken token)
        {
            return Task.Delay((int)delay).ContinueWith(
                _ => Execute(action, token));
        }

        /// <summary>
        /// Queues a function to run.
        /// </summary>
        /// <typeparam name="T">Type of response.</typeparam>
        /// <param name="func">The function.</param>
        /// <param name="token">The cancellation token.</param>
        public virtual Task<T> Execute<T>(Func<T> func, CancellationToken token)
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
                        if (token != CancellationToken.None)
                        {
                            token.ThrowIfCancellationRequested();
                        }

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
        /// Queues a function to run.
        /// </summary>
        /// <typeparam name="T">Type of response.</typeparam>
        /// <param name="func">The function.</param>
        public Task<T> Execute<T>(Func<T> func)
        {
            return Execute(func, CancellationToken.None);
        }

        /// <summary>
        /// Creates and executes a one-shot function that becomes enabled after the given delay.
        /// </summary>
        /// <remarks>
        /// The function will be submitted to the end of the event queue
        /// even if it is being submitted from the same queue Thread.
        /// </remarks>
        /// <typeparam name="T">Type of response.</typeparam>
        /// <param name="func">The action.</param>
        /// <param name="delay">The delay in milliseconds.</param>
        public Task<T> Schedule<T>(Func<T> func, long delay)
        {
            return Task.Delay((int)delay).ContinueWith(
                _ => Execute(func, CancellationToken.None)).Unwrap();
        }

        /// <summary>
        /// Creates and executes a one-shot function that becomes enabled after the given delay.
        /// </summary>
        /// <remarks>
        /// The function will be submitted to the end of the event queue
        /// even if it is being submitted from the same queue Thread.
        /// </remarks>
        /// <typeparam name="T">Type of response.</typeparam>
        /// <param name="func">The function.</param>
        /// <param name="delay">The delay in milliseconds.</param>
        /// <param name="token">The cancellation token.</param>
        public Task<T> Schedule<T>(Func<T> func, long delay, CancellationToken token)
        {
            return Task.Delay((int)delay).ContinueWith(
                _ => Execute(func, token)).Unwrap();
        }

        /// <summary>
        /// Disposes the action queue.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
