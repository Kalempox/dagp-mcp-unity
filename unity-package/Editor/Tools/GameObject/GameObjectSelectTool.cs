using System;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace DAGP.MCP.Tools.GameObjectOps
{
    /// <summary>Selects a GameObject in the Editor (Hierarchy + Inspector focus).</summary>
    public class GameObjectSelectTool : IDAGPTool
    {
        public string Name => "gameobject.select";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var go = GameObjectFinder.Resolve(parameters);
            if (go == null) throw new InvalidOperationException("gameobject not found");

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);

            return Task.FromResult<JToken>(new JObject
            {
                ["selected"] = true,
                ["name"] = go.name,
                ["instanceId"] = go.GetInstanceID()
            });
        }
    }
}
