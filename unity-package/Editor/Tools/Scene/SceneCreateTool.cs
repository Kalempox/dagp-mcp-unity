using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace DAGP.MCP.Tools.Scene
{
    /// <summary>Creates a new scene at the given asset path. Optionally adds default GameObjects (camera+light).</summary>
    public class SceneCreateTool : IDAGPTool
    {
        public string Name => "scene.create";
        public bool RequiresMainThread => true;

        public Task<JToken> Execute(JObject parameters)
        {
            var path = (string)parameters["path"] ?? throw new ArgumentException("'path' required (e.g. 'Assets/Scenes/New.unity')");
            var addDefaults = parameters.Value<bool?>("addDefaults") ?? true;
            var loadAfter = parameters.Value<bool?>("load") ?? true;

            if (!path.StartsWith("Assets/")) throw new ArgumentException("'path' must start with 'Assets/'");
            if (!path.EndsWith(".unity")) path += ".unity";

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir)) Directory.CreateDirectory(dir);

            var setup = addDefaults ? NewSceneSetup.DefaultGameObjects : NewSceneSetup.EmptyScene;
            var mode = loadAfter ? NewSceneMode.Single : NewSceneMode.Additive;
            var scene = EditorSceneManager.NewScene(setup, mode);

            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();

            return Task.FromResult<JToken>(new JObject
            {
                ["created"] = true,
                ["path"] = path,
                ["name"] = scene.name,
                ["loaded"] = loadAfter
            });
        }
    }
}
