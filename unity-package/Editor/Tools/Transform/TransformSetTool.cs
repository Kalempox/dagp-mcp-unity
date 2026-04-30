using System;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DAGP.MCP.Tools.TransformOps
{
    /// <summary>Sets absolute position, rotation (euler or quaternion), or localScale on a Transform.</summary>
    public class TransformSetTool : IDAGPTool
    {
        public string Name => "transform.set";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var go = GameObjectFinder.Resolve(parameters);
            if (go == null) throw new InvalidOperationException("gameobject not found");
            var t = go.transform;
            var space = ((string)parameters["space"] ?? "world").ToLowerInvariant();
            Undo.RecordObject(t, $"Set Transform {go.name}");

            if (parameters["position"] != null)
            {
                var p = parameters["position"].ToVector3();
                if (space == "local") t.localPosition = p; else t.position = p;
            }
            if (parameters["rotation"] != null)
            {
                var q = parameters["rotation"].ToQuaternion();
                if (space == "local") t.localRotation = q; else t.rotation = q;
            }
            if (parameters["scale"] != null)
            {
                t.localScale = parameters["scale"].ToVector3(Vector3.one);
            }

            EditorUtility.SetDirty(go);
            return Task.FromResult<JToken>(new JObject
            {
                ["set"] = true,
                ["name"] = go.name,
                ["position"] = t.position.ToJ(),
                ["rotationEuler"] = t.rotation.ToJEuler(),
                ["localScale"] = t.localScale.ToJ()
            });
        }
    }
}
