using System;
using Windows.System.Threading;

namespace FBCore.Concurrency
{
    /// <summary>
    /// Factory and utility methods for <see cref="IExecutorService"/>
    /// </summary>
    public static class Executors
    {
        /// <summary>
        /// Creates the action queue that uses the task scheduler to ensure a maximum concurrency 
        /// level while running on top of the thread pool.
        ///
        /// <param name="maxDegreeOfParallelism">The degrees of parallelism.</param>
        /// </summary>
        public static IExecutorService NewFixedThreadPool(int maxDegreeOfParallelism)
        {
            return new SerialExecutorService(
                "default",
                maxDegreeOfParallelism,
                WorkItemPriority.Normal,
                _ => {});
        }

        /// <summary>
        /// Creates the action queue that uses the task scheduler to ensure a maximum concurrency 
        /// level while running on top of the thread pool.
        ///
        /// <param name="maxDegreeOfParallelism">The degrees of parallelism.</param>
        /// <param name="priority">The priority of the work item relative to work items 
        /// in the thread pool. The value of this parameter can be Low, Normal, or High.</param>
        /// </summary>
        public static IExecutorService NewFixedThreadPool(int maxDegreeOfParallelism, WorkItemPriority priority)
        {
            return new SerialExecutorService(
                "default",
                maxDegreeOfParallelism,
                priority,
                _ => {});
        }

        /// <summary>
        /// Creates the action queue that uses the task scheduler to ensure a maximum concurrency 
        /// level while running on top of the thread pool.
        ///
        /// <param name="name">The name of the action queue.</param>
        /// <param name="maxDegreeOfParallelism">The degrees of parallelism.</param>
        /// <param name="priority">The priority of the work item relative to work items 
        /// in the thread pool. The value of this parameter can be Low, Normal, or High.</param>
        /// <param name="handler">The exception handler.</param>
        /// </summary>
        public static IExecutorService NewFixedThreadPool(
            string name, 
            int maxDegreeOfParallelism, 
            WorkItemPriority priority,
            Action<Exception> handler)
        {
            return new SerialExecutorService(
                name,
                maxDegreeOfParallelism,
                priority,
                handler);
        }
    }
}
