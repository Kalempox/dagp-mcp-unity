using System;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DAGP.MCP.Tools.MaterialOps
{
    /// <summary>Assigns a Material to a Renderer's slot (default slot 0).</summary>
    public class MaterialAssignTool : IDAGPTool
    {
        public string Name => "material.assign";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var go = GameObjectFinder.Resolve(parameters);
            if (go == null) throw new InvalidOperationException("gameobject not found");
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) throw new InvalidOperationException($"no Renderer on {go.name}");

            var matPath = (string)parameters["materialPath"] ?? throw new ArgumentException("'materialPath' required");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) throw new ArgumentException($"material not found: {matPath}");

            var slot = parameters.Value<int?>("slot") ?? 0;
            Undo.RecordObject(renderer, $"Assign Material {mat.name}");

            var mats = renderer.sharedMaterials;
            if (slot < 0 || slot >= mats.Length) throw new ArgumentException($"slot {slot} out of range (0..{mats.Length - 1})");
            mats[slot] = mat;
            renderer.sharedMaterials = mats;
            EditorUtility.SetDirty(renderer);

            return Task.FromResult<JToken>(new JObject
            {
                ["assigned"] = true,
                ["target"] = go.name,
                ["slot"] = slot,
                ["materialPath"] = matPath,
                ["materialName"] = mat.name
            });
        }
    }
}
