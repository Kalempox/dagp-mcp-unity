using System;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace DAGP.MCP.Tools.GameObjectOps
{
    /// <summary>Destroys a GameObject (with Undo).</summary>
    public class GameObjectDeleteTool : IDAGPTool
    {
        public string Name => "gameobject.delete";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var go = GameObjectFinder.Resolve(parameters);
            if (go == null) throw new InvalidOperationException("gameobject not found");
            var name = go.name;
            var id = go.GetInstanceID();
            Undo.DestroyObjectImmediate(go);

            return Task.FromResult<JToken>(new JObject
            {
                ["deleted"] = true,
                ["name"] = name,
                ["instanceId"] = id
            });
        }
    }
}
