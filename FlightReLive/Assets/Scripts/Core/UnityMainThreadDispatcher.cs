using System;
using System.Collections.Generic;
using System.Threading;

namespace FlightReLive.Core
{
    /// <summary>
    /// High-performance dispatcher ensuring that actions from any thread
    /// are executed safely in Unity's main thread.
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
        /// Execute all queued actions in the Unity main thread.
        /// Call this method once per frame (e.g. in Update).
        /// </summary>
        public static void ManageThreads()
        {
            // Quick check: skip lock if nothing to execute
            if (Volatile.Read(ref _pendingCount) == 0)
            {
                return;
            }

            // Swap queues under lock
            lock (_lock)
            {
                if (_actionsWrite.Count > 0)
                {
                    Queue<Action> tmp = _actionsRead;
                    _actionsRead = _actionsWrite;
                    _actionsWrite = tmp;
                }
            }

            // Execute without holding the lock
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
                    // Log exceptions to avoid breaking the dispatcher loop
                    UnityEngine.Debug.LogError($"UnityMainThreadDispatcher: Exception in dispatched action: {ex}");
                }
            }
        }

        #endregion
    }
}
