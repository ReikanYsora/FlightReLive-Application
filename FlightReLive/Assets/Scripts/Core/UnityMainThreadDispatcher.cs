using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlightReLive.Core
{
    /// <summary>
    /// High-performance dispatcher ensuring that actions from any thread are executed safely in Unity's main thread.
    /// - Double-buffered queues for minimal locking
    /// - Atomic counter to avoid unnecessary locks
    /// - Zero allocations during runtime (except if queues grow beyond initial capacity)
    /// - Exception safety to prevent dispatcher crash
    /// </summary>
    public static class UnityMainThreadDispatcher
    {
        #region ATTRIBUTES
        /// <summary>
        /// Queue used by background threads to enqueue actions.
        /// </summary>
        private static Queue<Action> _actionsWrite = new Queue<Action>(256);

        /// <summary>
        /// Queue consumed on the Unity main thread.
        /// </summary>
        private static Queue<Action> _actionsRead = new Queue<Action>(256);

        /// <summary>
        /// Synchronization lock for swapping queues.
        /// </summary>
        private static readonly object _lock = new object();

        /// <summary>
        /// Atomic counter of pending actions (faster check than locking).
        /// </summary>
        private static int _pendingCount = 0;
        #endregion

        #region METHODS
        /// <summary>
        /// Add an action that will be executed in the Unity main thread.
        /// This method is thread-safe and optimized for minimal contention.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public static void AddActionInMainThread(Action action)
        {
            if (action == null)
            {
                return;
            }

            lock (_lock)
            {
                _actionsWrite.Enqueue(action);
                Interlocked.Increment(ref _pendingCount);
            }
        }

        /// <summary>
        /// Schedules a function to be executed on Unity's main thread and returns a Task that completes with the function's return value once execution is finished.
        /// </summary>
        /// <typeparam name="T">Return type of the function</typeparam>
        /// <param name="func">The function to execute on the main thread</param>
        /// <returns>A Task that completes with the function's result</returns>
        public static Task<T> AwaitOnMainThread<T>(Func<T> func)
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

            AddActionInMainThread(() =>
            {
                try
                {
                    T result = func();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// Schedules an action to be executed on Unity's main thread and returns a Task that completes once the action has finished execution.
        /// </summary>
        /// <param name="action">The action to execute on the main thread</param>
        /// <returns>A Task that completes when the action has executed</returns>
        public static Task AwaitOnMainThread(Action action)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            AddActionInMainThread(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// Schedules an asynchronous function to be executed on Unity's main thread and returns its awaited result.
        /// </summary>
        /// <typeparam name="T">Return type of the Task result</typeparam>
        /// <param name="asyncFunc">The asynchronous function to execute on the main thread</param>
        /// <returns>A Task that completes with the function's result</returns>
        public static Task<T> AwaitOnMainThread<T>(Func<Task<T>> asyncFunc)
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

            AddActionInMainThread(async () =>
            {
                try
                {
                    T result = await asyncFunc();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// Execute all queued actions in the Unity main thread.
        /// Call this method once per frame (e.g. in Update).
        /// </summary>
        public static void ManageThreads()
        {
            //Quick check: skip lock if nothing to execute
            if (Volatile.Read(ref _pendingCount) == 0)
            {
                return;
            }

            //Swap queues under lock
            lock (_lock)
            {
                if (_actionsWrite.Count > 0)
                {
                    Queue<Action> tmp = _actionsRead;
                    _actionsRead = _actionsWrite;
                    _actionsWrite = tmp;
                }
            }

            //Execute without holding the lock
            while (_actionsRead.Count > 0)
            {
                Action action = _actionsRead.Dequeue();
                Interlocked.Decrement(ref _pendingCount);

                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    //Log exceptions to avoid breaking the dispatcher loop
                    UnityEngine.Debug.LogError($"UnityMainThreadDispatcher: Exception in dispatched action: {ex}");
                }
            }
        }

        #endregion
    }
}
