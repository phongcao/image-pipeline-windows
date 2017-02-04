using System;
using System.Threading;
using System.Threading.Tasks;

namespace FBCore.Concurrency
{
    /// <summary>
    /// An <see cref="IExecutorService"/> that can schedule commands to run after a given 
    /// delay, or to execute periodically.
    /// </summary>
    public interface IScheduledExecutorService : IExecutorService
    {
        /// <summary>
        /// Creates and executes a one-shot action that becomes enabled after the given delay.
        /// </summary>
        /// <remarks>
        /// The action will be submitted to the end of the event queue
        /// even if it is being submitted from the same queue Thread.
        /// </remarks>
        /// <param name="action">The action.</param>
        /// <param name="delay">The delay in milliseconds.</param>
        Task Schedule(Action action, long delay);

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
        Task Schedule(Action action, long delay, CancellationToken token);

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
        Task<T> Schedule<T>(Func<T> func, long delay);

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
        Task<T> Schedule<T>(Func<T> func, long delay, CancellationToken token);
    }
}
