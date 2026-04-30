using System;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DAGP.MCP.Tools.TransformOps
{
    /// <summary>Translates a Transform by a delta vector. Space: 'world' (default) or 'self'.</summary>
    public class TransformMoveTool : IDAGPTool
    {
        public string Name => "transform.move";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var go = GameObjectFinder.Resolve(parameters);
            if (go == null) throw new InvalidOperationException("gameobject not found");
            var t = go.transform;
            var delta = parameters["delta"].ToVector3();
            var space = ((string)parameters["space"] ?? "world").ToLowerInvariant();
            Undo.RecordObject(t, $"Move {go.name}");

            t.Translate(delta, space == "self" ? Space.Self : Space.World);
            EditorUtility.SetDirty(go);

            return Task.FromResult<JToken>(new JObject
            {
                ["moved"] = true,
                ["name"] = go.name,
                ["delta"] = delta.ToJ(),
                ["position"] = t.position.ToJ()
            });
        }
    }
}
