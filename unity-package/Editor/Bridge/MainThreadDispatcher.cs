using System;
using System.Collections.Concurrent;
using UnityEditor;

namespace DAGP.MCP.Bridge
{
    /// <summary>Marshals work from background WebSocket threads to Unity's Editor main thread.</summary>
    [InitializeOnLoad]
    public static class MainThreadDispatcher
    {
        static readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        static MainThreadDispatcher()
        {
            EditorApplication.update += Drain;
        }

        /// <summary>Schedule an action to run on the next Editor update tick.</summary>
        public static void Enqueue(Action action)
        {
            if (action != null) _queue.Enqueue(action);
        }

        static void Drain()
        {
            while (_queue.TryDequeue(out var action))
            {
                try { action(); }
                catch (Exception ex) { UnityEngine.Debug.LogError($"[DAGP-MCP] Dispatcher error: {ex}"); }
            }
        }
    }
}
