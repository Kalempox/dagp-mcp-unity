using System;
using System.IO;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DAGP.MCP.Tools.MaterialOps
{
    /// <summary>
    /// Creates a new Material asset. Default shader auto-detects URP/Standard.
    /// Optionally sets initial color.
    /// </summary>
    public class MaterialCreateTool : IDAGPTool
    {
        public string Name => "material.create";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var name = (string)parameters["name"] ?? throw new ArgumentException("'name' required");
            var shaderName = (string)parameters["shader"] ?? DetectDefaultShader();
            var savePath = (string)parameters["savePath"] ?? "Assets/Materials";

            var shader = Shader.Find(shaderName);
            if (shader == null) throw new ArgumentException($"shader not found: {shaderName}");

            if (!savePath.StartsWith("Assets/")) throw new ArgumentException("'savePath' must start with 'Assets/'");
            if (!AssetDatabase.IsValidFolder(savePath)) Directory.CreateDirectory(savePath);

            var mat = new Material(shader) { name = name };
            if (parameters["color"] != null) SetColor(mat, parameters["color"].ToColor(Color.white));

            var assetPath = $"{savePath}/{name}.mat";
            AssetDatabase.CreateAsset(mat, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return Task.FromResult<JToken>(new JObject
            {
                ["created"] = true,
                ["path"] = assetPath,
                ["shader"] = shader.name,
                ["color"] = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor").ToJ()
                          : mat.HasProperty("_Color") ? mat.GetColor("_Color").ToJ() : null
            });
        }

        static string DetectDefaultShader()
        {
            if (Shader.Find("Universal Render Pipeline/Lit") != null) return "Universal Render Pipeline/Lit";
            return "Standard";
        }

        static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            else if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }
    }
}
