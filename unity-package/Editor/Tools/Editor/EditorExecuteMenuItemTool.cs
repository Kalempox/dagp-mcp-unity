using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace DAGP.MCP.Tools.EditorOps
{
    /// <summary>
    /// Executes a Unity menu item by path (e.g. 'File/Save', 'Window/Package Manager', 'GameObject/3D Object/Cube').
    /// </summary>
    public class EditorExecuteMenuItemTool : IDAGPTool
    {
        public string Name => "editor.execute_menu_item";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var menuPath = (string)parameters["menuPath"] ?? throw new ArgumentException("'menuPath' required");
            var ok = EditorApplication.ExecuteMenuItem(menuPath);
            return Task.FromResult<JToken>(new JObject
            {
                ["executed"] = ok,
                ["menuPath"] = menuPath,
                ["note"] = ok ? null : "menu item not found or rejected"
            });
        }
    }
}
