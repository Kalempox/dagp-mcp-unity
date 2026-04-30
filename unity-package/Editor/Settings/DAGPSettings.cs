using UnityEditor;

namespace DAGP.MCP.Settings
{
    /// <summary>Editor-pref backed settings for the DAGP MCP bridge.</summary>
    public static class DAGPSettings
    {
        const string PortKey = "DAGP.MCP.Port";
        const string AutoStartKey = "DAGP.MCP.AutoStart";

        public static int Port
        {
            get => EditorPrefs.GetInt(PortKey, 8090);
            set => EditorPrefs.SetInt(PortKey, value);
        }

        public static bool AutoStart
        {
            get => EditorPrefs.GetBool(AutoStartKey, true);
            set => EditorPrefs.SetBool(AutoStartKey, value);
        }
    }
}
