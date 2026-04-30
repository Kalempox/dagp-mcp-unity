using System;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DAGP.MCP.Tools.TransformOps
{
    /// <summary>Scales a Transform. Mode 'set' (absolute localScale) or 'multiply' (componentwise).</summary>
    public class TransformScaleTool : IDAGPTool
    {
        public string Name => "transform.scale";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var go = GameObjectFinder.Resolve(parameters);
            if (go == null) throw new InvalidOperationException("gameobject not found");
            var t = go.transform;
            var scale = parameters["scale"].ToVector3(Vector3.one);
            var mode = ((string)parameters["mode"] ?? "set").ToLowerInvariant();
            Undo.RecordObject(t, $"Scale {go.name}");

            if (mode == "multiply")
            {
                var s = t.localScale;
                t.localScale = new Vector3(s.x * scale.x, s.y * scale.y, s.z * scale.z);
            }
            else
            {
                t.localScale = scale;
            }
            EditorUtility.SetDirty(go);

            return Task.FromResult<JToken>(new JObject
            {
                ["scaled"] = true,
                ["name"] = go.name,
                ["mode"] = mode,
                ["localScale"] = t.localScale.ToJ()
            });
        }
    }
}
