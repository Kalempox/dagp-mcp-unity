using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace DAGP.MCP.Tools.Scene
{
    /// <summary>Deletes a scene asset from the project. Requires the scene to be unloaded first.</summary>
    public class SceneDeleteTool : IDAGPTool
    {
        public string Name => "scene.delete";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var path = (string)parameters["path"] ?? throw new ArgumentException("'path' required");
            var loaded = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            if (loaded.IsValid() && loaded.isLoaded)
                throw new InvalidOperationException($"scene is currently loaded; unload first: {path}");

            var ok = AssetDatabase.DeleteAsset(path);
            if (!ok) throw new InvalidOperationException($"failed to delete asset: {path}");
            AssetDatabase.Refresh();

            return Task.FromResult<JToken>(new JObject
            {
                ["deleted"] = true,
                ["path"] = path
            });
        }
    }
}
