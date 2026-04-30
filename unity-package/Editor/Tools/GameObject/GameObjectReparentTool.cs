using System;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace DAGP.MCP.Tools.GameObjectOps
{
    /// <summary>Re-parents a GameObject. Pass parent=null to detach to scene root.</summary>
    public class GameObjectReparentTool : IDAGPTool
    {
        public string Name => "gameobject.reparent";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var go = GameObjectFinder.Resolve(parameters);
            if (go == null) throw new InvalidOperationException("gameobject not found");

            var parentName = (string)parameters["parent"];
            var worldPositionStays = parameters.Value<bool?>("worldPositionStays") ?? true;

            UnityEngine.Transform newParent = null;
            if (!string.IsNullOrEmpty(parentName))
            {
                var p = GameObjectFinder.Find(parentName);
                if (p == null) throw new InvalidOperationException($"parent not found: {parentName}");
                newParent = p.transform;
            }

            Undo.SetTransformParent(go.transform, newParent, $"Reparent {go.name}");
            if (newParent == null) go.transform.SetParent(null, worldPositionStays);
            else go.transform.SetParent(newParent, worldPositionStays);

            return Task.FromResult<JToken>(new JObject
            {
                ["reparented"] = true,
                ["name"] = go.name,
                ["newParent"] = newParent ? newParent.name : null
            });
        }
    }
}
