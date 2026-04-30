using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;

namespace DAGP.MCP.Tools.Scene
{
    /// <summary>Unloads a loaded scene by path. Cannot unload the only loaded scene.</summary>
    public class SceneUnloadTool : IDAGPTool
    {
        public string Name => "scene.unload";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var path = (string)parameters["path"] ?? throw new ArgumentException("'path' required");
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            if (!scene.IsValid()) throw new InvalidOperationException($"scene not loaded: {path}");

            var ok = EditorSceneManager.CloseScene(scene, removeScene: true);
            return Task.FromResult<JToken>(new JObject
            {
                ["unloaded"] = ok,
                ["path"] = path
            });
        }
    }
}
