using System;
using System.Threading.Tasks;

namespace FBCore.Concurrency
{
    /// <summary>
    /// An executor service that runs each task in the thread that 
    /// invokes <code> execute/submit</code>.
    /// </summary>
    public class CallerThreadExecutor : IExecutorService
    {
        private static readonly object _instanceGate = new object();
        private static CallerThreadExecutor _instance = null;

        private CallerThreadExecutor()
        {
        }

        /// <summary>
        /// Singleton
        /// </summary>
        /// <returns></returns>
        public static CallerThreadExecutor Instance
        {
            get
            {
                lock (_instanceGate)
                {
                    if (_instance == null)
                    {
                        _instance = new CallerThreadExecutor();
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Runs the given action on this thread. 
        /// </summary>
        /// <remarks>
        /// The action will be submitted to the end of the event queue
        /// even if it is being submitted from the same queue Thread.
        /// </remarks>
        /// <param name="action">The action.</param>
        public Task Execute(Action action)
        {
            action();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Runs the given function on this thread and returns a task to 
        /// await the response.
        /// </summary>
        /// <typeparam name="T">Type of response.</typeparam>
        /// <param name="func">The function.</param>
        /// <returns>A task to await the result.</returns>
        public Task<T> Execute<T>(Func<T> func)
        {
            return Task.FromResult(func());
        }
    }
}
