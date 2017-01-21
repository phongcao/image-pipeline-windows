using System;
using System.Threading;
using System.Threading.Tasks;

namespace FBCore.Concurrency
{
    /// <summary>
    /// This interface provides a way of decoupling task submission from the
    /// mechanics of how each task will be run, including details of thread
    /// use, scheduling, etc.
    /// </summary>
    public interface IExecutorService
    {
        /// <summary>
        /// Runs the given action on this thread. 
        /// </summary>
        /// <remarks>
        /// The action will be submitted to the end of the event queue
        /// even if it is being submitted from the same queue Thread.
        /// </remarks>
        /// <param name="action">The action.</param>
        Task Execute(Action action);

        /// <summary>
        /// Runs the given action on this thread. 
        /// </summary>
        /// <remarks>
        /// The action will be submitted to the end of the event queue
        /// even if it is being submitted from the same queue Thread.
        /// </remarks>
        /// <param name="action">The action.</param>
        /// <param name="token">The cancellation token.</param>
        Task Execute(Action action, CancellationToken token);

        /// <summary>
        /// Runs the given function on this thread and returns a task to 
        /// await the response.
        /// </summary>
        /// <typeparam name="T">Type of response.</typeparam>
        /// <param name="func">The function.</param>
        /// <returns>A task to await the result.</returns>
        Task<T> Execute<T>(Func<T> func);

        /// <summary>
        /// Runs the given function on this thread and returns a task to 
        /// await the response.
        /// </summary>
        /// <typeparam name="T">Type of response.</typeparam>
        /// <param name="func">The function.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>A task to await the result.</returns>
        Task<T> Execute<T>(Func<T> func, CancellationToken token);
    }
}
