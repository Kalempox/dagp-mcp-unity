using System;
using DAGP.MCP.Settings;
using UnityEditor;

namespace DAGP.MCP.Bridge
{
    /// <summary>Editor menu items for inspecting and controlling the bridge.</summary>
    public static class DAGPMenu
    {
        [MenuItem("DAGP/MCP Status")]
        public static void ShowStatus()
        {
            var lastMsg = DAGPBridge.LastMessageAt == default ? "never" : DAGPBridge.LastMessageAt.ToString("HH:mm:ss");
            var msg =
                $"Running: {DAGPBridge.IsRunning}\n" +
                $"Port: {DAGPBridge.Port}\n" +
                $"Auto-start: {DAGPSettings.AutoStart}\n" +
                $"Last message: {lastMsg}\n" +
                $"Last error: {DAGPBridge.LastError ?? "none"}";
            EditorUtility.DisplayDialog("DAGP MCP", msg, "OK");
        }

        [MenuItem("DAGP/Restart Bridge")]
        public static void Restart()
        {
            DAGPBridge.Stop();
            DAGPBridge.Start();
        }

        [MenuItem("DAGP/Toggle Auto-Start")]
        public static void ToggleAutoStart()
        {
            DAGPSettings.AutoStart = !DAGPSettings.AutoStart;
            EditorUtility.DisplayDialog("DAGP MCP", $"Auto-start: {DAGPSettings.AutoStart}", "OK");
        }
    }
}
