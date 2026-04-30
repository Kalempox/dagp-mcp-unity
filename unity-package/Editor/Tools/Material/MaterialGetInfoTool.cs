using System;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DAGP.MCP.Tools.MaterialOps
{
    /// <summary>Returns shader name and key property values of a Material asset.</summary>
    public class MaterialGetInfoTool : IDAGPTool
    {
        public string Name => "material.get_info";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var path = (string)parameters["path"] ?? throw new ArgumentException("'path' required");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) throw new ArgumentException($"material not found: {path}");

            var props = new JObject();
            var shader = mat.shader;
            int count = shader.GetPropertyCount();
            for (int i = 0; i < count; i++)
            {
                var pname = shader.GetPropertyName(i);
                var ptype = shader.GetPropertyType(i);
                switch (ptype)
                {
                    case UnityEngine.Rendering.ShaderPropertyType.Color:
                        props[pname] = mat.GetColor(pname).ToJ(); break;
                    case UnityEngine.Rendering.ShaderPropertyType.Float:
                    case UnityEngine.Rendering.ShaderPropertyType.Range:
                        props[pname] = mat.GetFloat(pname); break;
                    case UnityEngine.Rendering.ShaderPropertyType.Vector:
                        var v = mat.GetVector(pname);
                        props[pname] = new JObject { ["x"] = v.x, ["y"] = v.y, ["z"] = v.z, ["w"] = v.w }; break;
                    case UnityEngine.Rendering.ShaderPropertyType.Texture:
                        var t = mat.GetTexture(pname);
                        props[pname] = t ? AssetDatabase.GetAssetPath(t) : null; break;
                }
            }

            return Task.FromResult<JToken>(new JObject
            {
                ["path"] = path,
                ["name"] = mat.name,
                ["shader"] = shader.name,
                ["renderQueue"] = mat.renderQueue,
                ["properties"] = props
            });
        }
    }
}
