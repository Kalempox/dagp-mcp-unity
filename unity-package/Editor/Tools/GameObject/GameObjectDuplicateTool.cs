using System;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DAGP.MCP.Tools.GameObjectOps
{
    /// <summary>Clones a GameObject (and its hierarchy). Optionally renames the clone.</summary>
    public class GameObjectDuplicateTool : IDAGPTool
    {
        public string Name => "gameobject.duplicate";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var src = GameObjectFinder.Resolve(parameters);
            if (src == null) throw new InvalidOperationException("source gameobject not found");

            var clone = UnityEngine.Object.Instantiate(src, src.transform.parent);
            clone.name = (string)parameters["newName"] ?? src.name + " (Clone)";
            Undo.RegisterCreatedObjectUndo(clone, $"Duplicate {src.name}");

            return Task.FromResult<JToken>(new JObject
            {
                ["duplicated"] = true,
                ["sourceName"] = src.name,
                ["newName"] = clone.name,
                ["instanceId"] = clone.GetInstanceID()
            });
        }
    }
}
