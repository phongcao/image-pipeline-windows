using FBCore.Concurrency;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.System.Threading;

namespace ImagePipeline.Tests.Producers
{
    /// <summary>
    /// Mock <see cref="MockSerialExecutorService"/>
    /// </summary>
    class MockSerialExecutorService : SerialExecutorService
    {
        /// <summary>
        /// Delay time before start running
        /// </summary>
        private const int TIME_DELAY = 100;

        internal MockSerialExecutorService() : base(
                "MockSerialExecutorService",
                1,
                WorkItemPriority.Low,
                (_) => {})
        {
        }

        /// <summary>
        /// Queues an action to run.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="token">The cancellation token.</param>
        public override Task Execute(Action action, CancellationToken token)
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
                        Task.Delay(TIME_DELAY).Wait();

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
        /// Queues a function to run.
        /// </summary>
        /// <typeparam name="T">Type of response.</typeparam>
        /// <param name="func">The function.</param>
        /// <param name="token">The cancellation token.</param>
        public override Task<T> Execute<T>(Func<T> func, CancellationToken token)
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
                        Task.Delay(TIME_DELAY).Wait();

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
    }
}
