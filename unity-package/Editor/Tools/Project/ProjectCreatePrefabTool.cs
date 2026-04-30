using System;
using System.IO;
using System.Threading.Tasks;
using DAGP.MCP.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DAGP.MCP.Tools.ProjectOps
{
    /// <summary>Creates a prefab asset from a scene GameObject. Optionally connects the source as a prefab instance.</summary>
    public class ProjectCreatePrefabTool : IDAGPTool
    {
        public string Name => "project.create_prefab";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var go = GameObjectFinder.Resolve(parameters);
            if (go == null) throw new InvalidOperationException("source gameobject not found");

            var savePath = (string)parameters["savePath"] ?? "Assets/Prefabs";
            var name = (string)parameters["name"] ?? go.name;
            var connect = parameters.Value<bool?>("connect") ?? true;

            if (!savePath.StartsWith("Assets/")) throw new ArgumentException("'savePath' must start with 'Assets/'");
            if (!AssetDatabase.IsValidFolder(savePath)) Directory.CreateDirectory(savePath);

            var assetPath = $"{savePath}/{name}.prefab";
            GameObject prefab;
            if (connect)
                prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(go, assetPath, InteractionMode.UserAction);
            else
                prefab = PrefabUtility.SaveAsPrefabAsset(go, assetPath);

            if (prefab == null) throw new InvalidOperationException($"failed to save prefab at {assetPath}");
            AssetDatabase.SaveAssets();

            return Task.FromResult<JToken>(new JObject
            {
                ["created"] = true,
                ["path"] = assetPath,
                ["sourceGameObject"] = go.name,
                ["connected"] = connect
            });
        }
    }
}
