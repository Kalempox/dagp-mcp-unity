using System;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DAGP.MCP.Tools.MaterialOps
{
    /// <summary>
    /// Modifies a Material asset's shader properties. Supports color, float, vector, texture (asset path).
    /// 'color' is shorthand for the main color (_BaseColor / _Color, auto-detected).
    /// </summary>
    public class MaterialModifyTool : IDAGPTool
    {
        public string Name => "material.modify";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var path = (string)parameters["path"] ?? throw new ArgumentException("'path' required");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) throw new ArgumentException($"material not found: {path}");

            Undo.RecordObject(mat, $"Modify Material {mat.name}");
            var changed = new JObject();

            if (parameters["color"] != null)
            {
                var c = parameters["color"].ToColor(Color.white);
                var prop = mat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                mat.SetColor(prop, c);
                changed[prop] = c.ToJ();
            }

            if (parameters["floats"] is JObject floats)
                foreach (var kv in floats) { mat.SetFloat(kv.Key, (float)kv.Value); changed[kv.Key] = (float)kv.Value; }

            if (parameters["vectors"] is JObject vectors)
                foreach (var kv in vectors) { mat.SetVector(kv.Key, new Vector4((float)kv.Value["x"], (float)kv.Value["y"], (float)kv.Value["z"], kv.Value.Value<float?>("w") ?? 0f)); changed[kv.Key] = kv.Value; }

            if (parameters["colors"] is JObject colors)
                foreach (var kv in colors) { var col = kv.Value.ToColor(Color.white); mat.SetColor(kv.Key, col); changed[kv.Key] = col.ToJ(); }

            if (parameters["textures"] is JObject textures)
                foreach (var kv in textures)
                {
                    var texPath = (string)kv.Value;
                    var tex = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                    if (tex == null) throw new ArgumentException($"texture not found: {texPath}");
                    mat.SetTexture(kv.Key, tex);
                    changed[kv.Key] = texPath;
                }

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            return Task.FromResult<JToken>(new JObject
            {
                ["modified"] = true,
                ["path"] = path,
                ["changes"] = changed
            });
        }
    }
}
