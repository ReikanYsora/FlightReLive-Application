using System;
using System.Collections.Generic;

namespace FlightReLive.Core
{
    public static class UnityMainThreadDispatcher
    {
        #region ATTRIBUTES
        private static readonly Queue<Action> _actions = new Queue<Action>();
        #endregion

        #region METHODS
        public static void AddActionInMainThread(Action action)
        {
            lock (_actions)
            {
                _actions.Enqueue(action);
            }
        }

        public static void ManageThreads()
        {
            lock (_actions)
            {
                while (_actions.Count > 0)
                {
                    _actions.Dequeue()?.Invoke();
                }
            }
        }
        #endregion
    }
}
