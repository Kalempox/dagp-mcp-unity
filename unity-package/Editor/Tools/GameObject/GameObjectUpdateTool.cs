using System;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DAGP.MCP.Tools.GameObjectOps
{
    /// <summary>Updates GameObject properties: name, active, tag, layer, isStatic.</summary>
    public class GameObjectUpdateTool : IDAGPTool
    {
        public string Name => "gameobject.update";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var go = GameObjectFinder.Resolve(parameters);
            if (go == null) throw new InvalidOperationException("gameobject not found");
            Undo.RecordObject(go, $"Update {go.name}");

            var changed = new JObject();
            if (parameters["newName"] != null) { go.name = (string)parameters["newName"]; changed["name"] = go.name; }
            if (parameters["active"] != null) { go.SetActive(parameters.Value<bool>("active")); changed["active"] = go.activeSelf; }
            if (parameters["tag"] != null) { go.tag = (string)parameters["tag"]; changed["tag"] = go.tag; }
            if (parameters["layer"] != null)
            {
                var layerToken = parameters["layer"];
                int layer = layerToken.Type == JTokenType.String
                    ? LayerMask.NameToLayer((string)layerToken)
                    : (int)layerToken;
                if (layer < 0) throw new ArgumentException($"unknown layer: {layerToken}");
                go.layer = layer;
                changed["layer"] = layer;
            }
            if (parameters["isStatic"] != null) { go.isStatic = parameters.Value<bool>("isStatic"); changed["isStatic"] = go.isStatic; }

            EditorUtility.SetDirty(go);
            return Task.FromResult<JToken>(new JObject
            {
                ["updated"] = true,
                ["instanceId"] = go.GetInstanceID(),
                ["changes"] = changed
            });
        }
    }
}
