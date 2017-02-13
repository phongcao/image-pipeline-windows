using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace ImagePipeline.Platform
{
    /// <summary>
    /// Dispatcher helpers.
    /// 
    /// @author Eric Rozell
    /// </summary>
    public static class DispatcherHelpers
    {
        /// <summary>
        /// Asserts on dispatcher.
        /// </summary>
        public static void AssertOnDispatcher()
        {
            if (!IsOnDispatcher())
            {
                throw new InvalidOperationException("Thread does not have dispatcher access.");
            }
        }

        /// <summary>
        /// Checks is on dispatcher.
        /// </summary>
        public static bool IsOnDispatcher()
        {
            return CoreWindow.GetForCurrentThread()?.Dispatcher != null;
        }

        /// <summary>
        /// Runs on dispatcher.
        /// </summary>
        public static async void RunOnDispatcher(DispatchedHandler action)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, action).AsTask().ConfigureAwait(false);
        }

        /// <summary>
        /// Runs on dispatcher.
        /// </summary>
        public static Task RunOnDispatcherAsync(Action action)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();

            RunOnDispatcher(() =>
            {
                try
                {
                    action();
                    taskCompletionSource.SetResult(string.Empty);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            });

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Calls on dispatcher.
        /// </summary>
        public static Task<T> CallOnDispatcherAsync<T>(Func<T> func)
        {
            var taskCompletionSource = new TaskCompletionSource<T>();

            RunOnDispatcher(() =>
            {
                var result = func();

                // TaskCompletionSource<T>.SetResult can call continuations
                // on the awaiter of the task completion source.
                Task.Run(() => taskCompletionSource.SetResult(result));
            });

            return taskCompletionSource.Task;
        }
    }
}
