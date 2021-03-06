﻿using System;
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
            return CoreApplication.MainView.Dispatcher.HasThreadAccess;
        }

        /// <summary>
        /// Runs on dispatcher.
        /// </summary>
        public static async void RunOnDispatcher(DispatchedHandler action, CoreDispatcher dispatcher = null)
        {
            if (dispatcher != null)
            {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, action).AsTask().ConfigureAwait(false);
            }
            else
            {
                await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, action).AsTask().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Runs on dispatcher.
        /// </summary>
        public static Task RunOnDispatcherAsync(Action action, CoreDispatcher dispatcher = null)
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
            },
            dispatcher);

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Calls on dispatcher.
        /// </summary>
        public static Task CallOnDispatcherAsync(Func<Task> asyncFunc, CoreDispatcher dispatcher = null)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();

            RunOnDispatcher(async () =>
            {
                try
                {
                    await asyncFunc().ConfigureAwait(false);
                    taskCompletionSource.SetResult(string.Empty);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            },
            dispatcher);

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Calls on dispatcher.
        /// </summary>
        public static Task<T> CallOnDispatcherAsync<T>(Func<T> func, CoreDispatcher dispatcher = null)
        {
            var taskCompletionSource = new TaskCompletionSource<T>();

            RunOnDispatcher(() =>
            {
                var result = func();

                // TaskCompletionSource<T>.SetResult can call continuations
                // on the awaiter of the task completion source.
                Task.Run(() => taskCompletionSource.SetResult(result));
            },
            dispatcher);

            return taskCompletionSource.Task;
        }
    }
}
