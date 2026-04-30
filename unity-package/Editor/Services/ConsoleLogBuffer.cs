using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DAGP.MCP.Services
{
    /// <summary>
    /// Editor-only ring buffer of Unity Console log messages. Subscribes to Application.logMessageReceivedThreaded.
    /// Capped at 2000 entries to bound memory.
    /// </summary>
    [InitializeOnLoad]
    public static class ConsoleLogBuffer
    {
        public class Entry
        {
            public DateTime Time;
            public string Type;       // Log / Warning / Error / Assert / Exception
            public string Message;
            public string StackTrace;
        }

        const int Capacity = 2000;
        static readonly LinkedList<Entry> _entries = new LinkedList<Entry>();
        static readonly object _lock = new object();

        static ConsoleLogBuffer()
        {
            Application.logMessageReceivedThreaded -= OnLog;
            Application.logMessageReceivedThreaded += OnLog;
        }

        static void OnLog(string message, string stackTrace, LogType type)
        {
            lock (_lock)
            {
                _entries.AddLast(new Entry
                {
                    Time = DateTime.Now,
                    Type = type.ToString(),
                    Message = message,
                    StackTrace = stackTrace
                });
                while (_entries.Count > Capacity) _entries.RemoveFirst();
            }
        }

        public static List<Entry> Snapshot(int count, string typeFilter = null, string messageContains = null)
        {
            var result = new List<Entry>();
            lock (_lock)
            {
                foreach (var e in _entries)
                {
                    if (!string.IsNullOrEmpty(typeFilter) && !string.Equals(e.Type, typeFilter, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.IsNullOrEmpty(messageContains) && (e.Message == null || e.Message.IndexOf(messageContains, StringComparison.OrdinalIgnoreCase) < 0)) continue;
                    result.Add(e);
                }
            }
            if (count > 0 && result.Count > count) result = result.GetRange(result.Count - count, count);
            return result;
        }

        public static void Clear() { lock (_lock) _entries.Clear(); }
        public static int Count { get { lock (_lock) return _entries.Count; } }
    }
}
