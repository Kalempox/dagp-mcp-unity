using System;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DAGP.MCP.Tools.TransformOps
{
    /// <summary>Rotates a Transform by euler delta degrees. Space: 'world' (default) or 'self'.</summary>
    public class TransformRotateTool : IDAGPTool
    {
        public string Name => "transform.rotate";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var go = GameObjectFinder.Resolve(parameters);
            if (go == null) throw new InvalidOperationException("gameobject not found");
            var t = go.transform;
            var euler = parameters["euler"].ToVector3();
            var space = ((string)parameters["space"] ?? "world").ToLowerInvariant();
            Undo.RecordObject(t, $"Rotate {go.name}");

            t.Rotate(euler, space == "self" ? Space.Self : Space.World);
            EditorUtility.SetDirty(go);

            return Task.FromResult<JToken>(new JObject
            {
                ["rotated"] = true,
                ["name"] = go.name,
                ["delta"] = euler.ToJ(),
                ["rotationEuler"] = t.rotation.ToJEuler()
            });
        }
    }
}
